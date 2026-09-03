using Cur.Web.Models;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using Cur.Web.Services;
using Cur.Web.Services.Carta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cur.Web.Controllers;

/// <summary>
/// Carta de presentación: el cuestionario de estilo, el borrador y su edición.
/// El texto que se guarda es siempre el que deja el usuario, no el que propuso el
/// generador; la carta la firma la persona, no la aplicación.
/// </summary>
[Authorize]
public class CartaController : Controller
{
    private readonly ICurriculumService _curriculum;
    private readonly ICartaService _cartas;
    private readonly IRedactorCarta _redactor;
    private readonly ITonoPreferencia _tonos;
    private readonly UserManager<IdentityUser> _userManager;

    public CartaController(
        ICurriculumService curriculum,
        ICartaService cartas,
        IRedactorCarta redactor,
        ITonoPreferencia tonos,
        UserManager<IdentityUser> userManager)
    {
        _curriculum = curriculum;
        _cartas = cartas;
        _redactor = redactor;
        _tonos = tonos;
        _userManager = userManager;
    }

    private string UserId => _userManager.GetUserId(User)!;

    // ---------- Carta ----------

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil)
        {
            TempData["Error"] = "Completa tus datos personales antes de escribir la carta.";
            return RedirectToAction("Perfil", "Curriculum");
        }

        var guardada = await _cartas.ObtenerAsync(UserId, ct);

        var vm = new CartaViewModel
        {
            CargoObjetivo = guardada?.CargoObjetivo ?? string.Empty,
            Empresa = guardada?.Empresa,
            Texto = guardada?.Texto,
            IncluirEnHojaDeVida = guardada?.IncluirEnHojaDeVida ?? false,
            TieneCartaGuardada = guardada is not null
        };

        Preparar(vm, cv);
        return View(vm);
    }

    /// <summary>Arma un borrador nuevo. No lo guarda: el usuario decide después.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar(CartaViewModel vm, CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil) return RedirectToAction("Perfil", "Curriculum");

        Preparar(vm, cv);

        if (!ModelState.IsValid) return View(nameof(Index), vm);

        var solicitud = new SolicitudCarta(
            vm.CargoObjetivo, vm.Empresa, vm.Motivacion, vm.LogrosIds, vm.CompetenciasIds);

        vm.Texto = _redactor.Redactar(cv, solicitud, vm.Tono);

        TempData["Exito"] = "Listo, este es tu borrador. Léelo, ajústalo a tu voz y guárdalo.";
        return View(nameof(Index), vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(CartaViewModel vm, CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil) return RedirectToAction("Perfil", "Curriculum");

        if (string.IsNullOrWhiteSpace(vm.Texto))
            ModelState.AddModelError(nameof(vm.Texto), "La carta está vacía. Genera un borrador o escríbela.");

        if (!ModelState.IsValid)
        {
            Preparar(vm, cv);
            return View(nameof(Index), vm);
        }

        await _cartas.GuardarAsync(UserId, new CartaPresentacion
        {
            CargoObjetivo = vm.CargoObjetivo,
            Empresa = vm.Empresa,
            Tono = _tonos.Obtener(User),
            Texto = vm.Texto!.Trim(),
            IncluirEnHojaDeVida = vm.IncluirEnHojaDeVida
        }, ct);

        TempData["Exito"] = vm.IncluirEnHojaDeVida
            ? "Carta guardada. Se antepondrá a tu hoja de vida al descargarla."
            : "Carta guardada.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(CancellationToken ct)
    {
        var eliminada = await _cartas.EliminarAsync(UserId, ct);
        TempData[eliminada ? "Exito" : "Error"] = eliminada
            ? "Carta eliminada."
            : "No tenías ninguna carta guardada.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- Estilo ----------

    [HttpGet]
    public IActionResult Estilo() => View(new EstiloCartaViewModel
    {
        Actual = _tonos.Obtener(User),
        Definido = _tonos.EstaDefinido(User)
    });

    /// <summary>Calcula el tono sugerido a partir del cuestionario y lo guarda.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Estilo(EstiloCartaViewModel vm)
    {
        if (vm.Respuestas.Count != CuestionarioTono.Preguntas.Count ||
            vm.Respuestas.Any(r => r < CuestionarioTono.Minimo || r > CuestionarioTono.Maximo))
        {
            TempData["Error"] = "Responde las ocho afirmaciones para calcular tu estilo.";
            return RedirectToAction(nameof(Estilo));
        }

        var tono = CuestionarioTono.Calcular(vm.Respuestas);
        await _tonos.GuardarAsync(User, tono);

        TempData["Exito"] = $"Tu carta se redactará en tono {Tonos.Info(tono).Nombre.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Estilo));
    }

    /// <summary>Permite fijar el tono a mano, sin pasar por el cuestionario.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EstiloDirecto(TonoCarta tono)
    {
        if (!Enum.IsDefined(tono))
        {
            TempData["Error"] = "Ese estilo no existe.";
            return RedirectToAction(nameof(Estilo));
        }

        await _tonos.GuardarAsync(User, tono);

        TempData["Exito"] = $"Tu carta se redactará en tono {Tonos.Info(tono).Nombre.ToLowerInvariant()}.";
        return RedirectToAction(nameof(Estilo));
    }

    // ---------- Apoyo ----------

    /// <summary>Rellena el tono vigente y el catálogo de evidencia que trae la hoja de vida.</summary>
    private void Preparar(CartaViewModel vm, CurriculumViewModel cv)
    {
        vm.Tono = _tonos.Obtener(User);
        vm.TonoDefinido = _tonos.EstaDefinido(User);

        vm.Logros = cv.Experiencia
            .SelectMany(e => e.Logros.Select(l => new OpcionLogro(
                l.LogroId,
                Services.Pdf.PlantillaComun.LimpiarVinieta(l.Logro),
                Services.Pdf.PlantillaComun.LimpiarVinieta(l.Descripcion),
                Services.Pdf.PlantillaComun.Unir(" · ", e.Cargo?.Descripcion, e.Empresa))))
            .ToList();

        vm.Competencias = cv.Experiencia
            .SelectMany(e => e.Competencias.Select(c => new OpcionCompetencia(
                c.CompetenciaId,
                c.Descripcion,
                c.Medicion,
                Services.Pdf.PlantillaComun.Unir(" · ", e.Cargo?.Descripcion, e.Empresa))))
            .ToList();
    }
}
