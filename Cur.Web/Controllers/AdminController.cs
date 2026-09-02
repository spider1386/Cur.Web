using Cur.Web.Models;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using Cur.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cur.Web.Controllers;

/// <summary>
/// Panel de administracion. Todo el controlador exige el rol Administrador; el rol
/// nunca se otorga desde la aplicacion, solo directamente en la base de datos.
/// </summary>
[Authorize(Roles = Roles.Administrador)]
public class AdminController : Controller
{
    private readonly IAdminService _admin;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AdminController> _log;

    public AdminController(
        IAdminService admin,
        UserManager<IdentityUser> userManager,
        ILogger<AdminController> log)
    {
        _admin = admin;
        _userManager = userManager;
        _log = log;
    }

    // ---------- Tablero ----------

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await _admin.ObtenerDashboardAsync(ct));

    // ---------- Usuarios ----------

    [HttpGet]
    public async Task<IActionResult> Usuarios(string? busqueda, CancellationToken ct) =>
        View(new UsuariosAdminViewModel
        {
            Busqueda = busqueda,
            Usuarios = await _admin.ListarUsuariosAsync(busqueda, ct)
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bloquear(string id, string? busqueda)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null) return NotFound();

        var permiso = await PuedeIntervenirAsync(usuario);
        if (!permiso.Permitido)
        {
            TempData["Error"] = permiso.Motivo;
            return RedirectToAction(nameof(Usuarios), new { busqueda });
        }

        await _userManager.SetLockoutEnabledAsync(usuario, true);
        await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);

        _log.LogInformation("Usuario {Email} bloqueado por {Admin}", usuario.Email, User.Identity?.Name);
        TempData["Exito"] = $"Bloqueamos el acceso de {usuario.Email}.";
        return RedirectToAction(nameof(Usuarios), new { busqueda });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desbloquear(string id, string? busqueda)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(usuario, null);
        await _userManager.ResetAccessFailedCountAsync(usuario);

        _log.LogInformation("Usuario {Email} desbloqueado por {Admin}", usuario.Email, User.Identity?.Name);
        TempData["Exito"] = $"Restablecimos el acceso de {usuario.Email}.";
        return RedirectToAction(nameof(Usuarios), new { busqueda });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(string id, string? busqueda, CancellationToken ct)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario is null) return NotFound();

        var permiso = await PuedeIntervenirAsync(usuario);
        if (!permiso.Permitido)
        {
            TempData["Error"] = permiso.Motivo;
            return RedirectToAction(nameof(Usuarios), new { busqueda });
        }

        // Primero la hoja de vida: las FK de la base no borran en cascada.
        await _admin.EliminarHojaVidaAsync(usuario.Id, ct);

        var resultado = await _userManager.DeleteAsync(usuario);
        if (!resultado.Succeeded)
        {
            TempData["Error"] = "No se pudo eliminar la cuenta: " +
                string.Join(" ", resultado.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Usuarios), new { busqueda });
        }

        _log.LogWarning("Usuario {Email} eliminado por {Admin}", usuario.Email, User.Identity?.Name);
        TempData["Exito"] = $"Eliminamos la cuenta {usuario.Email} y su hoja de vida.";
        return RedirectToAction(nameof(Usuarios), new { busqueda });
    }

    // ---------- Parametros ----------

    [HttpGet]
    public async Task<IActionResult> Parametros(int? grupo, CancellationToken ct)
    {
        var vm = await _admin.ListarParametrosAsync(ct);
        vm.GrupoAbierto = grupo ?? vm.Grupos.FirstOrDefault()?.Codigo;
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Parametro(int? id, int? grupo, CancellationToken ct)
    {
        var vm = new ParametroFormViewModel { Codigo = grupo ?? 0 };

        if (id is > 0)
        {
            var entidad = await _admin.ObtenerParametroAsync(id.Value, ct);
            if (entidad is null) return NotFound();

            vm = new ParametroFormViewModel
            {
                ParametroId = entidad.ParametroId,
                Tipo = entidad.Tipo,
                Descripcion = entidad.Descripcion,
                Codigo = entidad.Codigo
            };
        }

        vm.Grupos = ListaGrupos(vm.Codigo);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Parametro(ParametroFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Grupos = ListaGrupos(vm.Codigo);
            return View(vm);
        }

        try
        {
            await _admin.GuardarParametroAsync(vm, ct);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        TempData["Exito"] = vm.EsNuevo ? "Parámetro creado." : "Parámetro actualizado.";
        return RedirectToAction(nameof(Parametros), new { grupo = vm.Codigo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarParametro(int id, int grupo, CancellationToken ct)
    {
        var usos = await _admin.EliminarParametroAsync(id, ct);

        TempData[usos == 0 ? "Exito" : "Error"] = usos == 0
            ? "Parámetro eliminado."
            : $"No se puede eliminar: {usos} registro(s) lo están usando. Edítalo en lugar de borrarlo.";

        return RedirectToAction(nameof(Parametros), new { grupo });
    }

    // ---------- Apoyo ----------

    private static IEnumerable<SelectListItem> ListaGrupos(int seleccionado) =>
        GrupoParametro.Nombres
            .OrderBy(g => g.Key)
            .Select(g => new SelectListItem
            {
                Value = g.Key.ToString(),
                Text = $"{g.Value} ({g.Key})",
                Selected = g.Key == seleccionado
            })
            .ToList();

    /// <summary>Impide que un administrador se bloquee o se elimine a si mismo, o a otro administrador.</summary>
    private async Task<(bool Permitido, string Motivo)> PuedeIntervenirAsync(IdentityUser usuario)
    {
        if (usuario.Id == _userManager.GetUserId(User))
            return (false, "No puedes aplicar esta acción sobre tu propia cuenta.");

        if (await _userManager.IsInRoleAsync(usuario, Roles.Administrador))
            return (false, "No puedes intervenir la cuenta de otro administrador desde el panel.");

        return (true, string.Empty);
    }
}
