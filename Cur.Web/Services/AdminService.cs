using Cur.Web.Data;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cur.Web.Services;

public interface IAdminService
{
    Task<AdminDashboardViewModel> ObtenerDashboardAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UsuarioAdminViewModel>> ListarUsuariosAsync(string? busqueda, CancellationToken ct = default);

    Task<ParametrosAdminViewModel> ListarParametrosAsync(CancellationToken ct = default);
    Task<Parametro?> ObtenerParametroAsync(int id, CancellationToken ct = default);
    Task GuardarParametroAsync(ParametroFormViewModel vm, CancellationToken ct = default);

    /// <summary>Borra un parametro. Devuelve el numero de registros que lo usan si no se puede borrar.</summary>
    Task<int> EliminarParametroAsync(int id, CancellationToken ct = default);

    /// <summary>Borra la hoja de vida completa de un usuario (sin tocar su cuenta de Identity).</summary>
    Task EliminarHojaVidaAsync(string userId, CancellationToken ct = default);
}

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IMemoryCache _cache;

    public AdminService(ApplicationDbContext db, UserManager<IdentityUser> userManager, IMemoryCache cache)
    {
        _db = db;
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<AdminDashboardViewModel> ObtenerDashboardAsync(CancellationToken ct = default)
    {
        var ahora = DateTimeOffset.UtcNow;

        var grupos = await _db.Parametros.AsNoTracking()
            .GroupBy(p => p.Codigo)
            .Select(g => new { Codigo = g.Key, Cantidad = g.Count() })
            .ToListAsync(ct);

        return new AdminDashboardViewModel
        {
            TotalUsuarios = await _db.Users.CountAsync(ct),
            UsuariosBloqueados = await _db.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > ahora, ct),
            UsuariosSinConfirmar = await _db.Users.CountAsync(u => !u.EmailConfirmed, ct),
            UsuariosConHojaVida = await _db.InformacionBasica.CountAsync(b => b.UserId != null, ct),
            TotalHojasVida = await _db.InformacionBasica.CountAsync(ct),
            HojasVidaSinDuenio = await _db.InformacionBasica.CountAsync(b => b.UserId == null, ct),
            TotalExperiencias = await _db.InformacionLaboral.CountAsync(ct),
            TotalFormaciones = await _db.FormacionAcademica.CountAsync(ct),
            TotalLogros = await _db.LogrosLaborales.CountAsync(ct),
            TotalCompetencias = await _db.Competencias.CountAsync(ct),
            TotalParametros = grupos.Sum(g => g.Cantidad),
            Grupos = grupos
                .OrderBy(g => g.Codigo)
                .Select(g => new AdminDashboardViewModel.GrupoResumen(
                    g.Codigo,
                    GrupoParametro.Nombre(g.Codigo),
                    GrupoParametro.Icono(g.Codigo),
                    g.Cantidad))
                .ToList()
        };
    }

    public async Task<IReadOnlyList<UsuarioAdminViewModel>> ListarUsuariosAsync(string? busqueda, CancellationToken ct = default)
    {
        var consulta = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();
            consulta = consulta.Where(u => u.Email != null && EF.Functions.Like(u.Email, $"%{termino}%"));
        }

        var usuarios = await consulta.OrderBy(u => u.Email).Take(200).ToListAsync(ct);
        var ids = usuarios.Select(u => u.Id).ToList();

        // Una sola consulta para los roles y otra para las hojas de vida, en vez de N+1.
        var roles = await (from ur in _db.UserRoles
                           join r in _db.Roles on ur.RoleId equals r.Id
                           where ids.Contains(ur.UserId)
                           select new { ur.UserId, Rol = r.Name! })
                          .ToListAsync(ct);

        var hojas = await _db.InformacionBasica.AsNoTracking()
            .Where(b => b.UserId != null && ids.Contains(b.UserId))
            .Select(b => new
            {
                b.UserId,
                b.BasicaId,
                Nombre = (b.Nombres ?? "") + " " + b.Apellidos,
                Experiencias = b.Experiencia.Count(),
                Formaciones = b.Formacion.Count()
            })
            .ToListAsync(ct);

        var ahora = DateTimeOffset.UtcNow;

        return usuarios.Select(u =>
        {
            var hoja = hojas.FirstOrDefault(h => h.UserId == u.Id);

            return new UsuarioAdminViewModel
            {
                Id = u.Id,
                Email = u.Email ?? u.UserName ?? "(sin correo)",
                EmailConfirmado = u.EmailConfirmed,
                Bloqueado = u.LockoutEnd is not null && u.LockoutEnd > ahora,
                BloqueadoHasta = u.LockoutEnd,
                AccesosFallidos = u.AccessFailedCount,
                Roles = roles.Where(r => r.UserId == u.Id).Select(r => r.Rol).ToList(),
                TieneHojaVida = hoja is not null,
                NombreCompleto = hoja?.Nombre.Trim(),
                Experiencias = hoja?.Experiencias ?? 0,
                Formaciones = hoja?.Formaciones ?? 0
            };
        }).ToList();
    }

    public async Task<ParametrosAdminViewModel> ListarParametrosAsync(CancellationToken ct = default)
    {
        var parametros = await _db.Parametros.AsNoTracking()
            .OrderBy(p => p.Codigo).ThenBy(p => p.Descripcion)
            .ToListAsync(ct);

        var grupos = parametros
            .GroupBy(p => p.Codigo)
            .OrderBy(g => g.Key)
            .Select(g => new ParametrosAdminViewModel.Grupo(
                g.Key,
                GrupoParametro.Nombre(g.Key),
                GrupoParametro.Icono(g.Key),
                g.ToList()))
            .ToList();

        // Los grupos que la app consume pero que quedaron vacios tambien deben verse.
        foreach (var codigo in GrupoParametro.EnUso.Where(c => grupos.All(g => g.Codigo != c)))
        {
            grupos.Add(new ParametrosAdminViewModel.Grupo(
                codigo, GrupoParametro.Nombre(codigo), GrupoParametro.Icono(codigo), Array.Empty<Parametro>()));
        }

        return new ParametrosAdminViewModel
        {
            Grupos = grupos.OrderBy(g => g.Codigo).ToList()
        };
    }

    public Task<Parametro?> ObtenerParametroAsync(int id, CancellationToken ct = default) =>
        _db.Parametros.FirstOrDefaultAsync(p => p.ParametroId == id, ct);

    public async Task GuardarParametroAsync(ParametroFormViewModel vm, CancellationToken ct = default)
    {
        Parametro entidad;

        if (vm.ParametroId == 0)
        {
            entidad = new Parametro();
            _db.Parametros.Add(entidad);
        }
        else
        {
            entidad = await _db.Parametros.FirstOrDefaultAsync(p => p.ParametroId == vm.ParametroId, ct)
                ?? throw new KeyNotFoundException("El parámetro no existe.");
        }

        entidad.Tipo = vm.Tipo.Trim();
        entidad.Descripcion = vm.Descripcion.Trim();
        entidad.Codigo = vm.Codigo;

        await _db.SaveChangesAsync(ct);
        InvalidarCacheCatalogos();
    }

    public async Task<int> EliminarParametroAsync(int id, CancellationToken ct = default)
    {
        var entidad = await _db.Parametros.FirstOrDefaultAsync(p => p.ParametroId == id, ct);
        if (entidad is null) return 0;

        var usos =
            await _db.InformacionBasica.CountAsync(b => b.ProfesionId == id, ct) +
            await _db.InformacionLaboral.CountAsync(l => l.CargoId == id || l.AreaId == id || l.EstadoId == id, ct) +
            await _db.FormacionAcademica.CountAsync(f => f.TipoFormacionId == id || f.AreaFormacionId == id || f.EstadoId == id, ct) +
            await _db.LogrosLaborales.CountAsync(l => l.TipoId == id, ct) +
            await _db.Competencias.CountAsync(c => c.TipoCompetenciaId == id, ct);

        if (usos > 0) return usos;

        _db.Parametros.Remove(entidad);
        await _db.SaveChangesAsync(ct);
        InvalidarCacheCatalogos();
        return 0;
    }

    public async Task EliminarHojaVidaAsync(string userId, CancellationToken ct = default)
    {
        var basica = await _db.InformacionBasica
            .Include(b => b.Experiencia).ThenInclude(l => l.Logros)
            .Include(b => b.Experiencia).ThenInclude(l => l.Competencias)
            .Include(b => b.Formacion)
            .FirstOrDefaultAsync(b => b.UserId == userId, ct);

        if (basica is null) return;

        // Las FK de la base no tienen ON DELETE CASCADE: se borra de hijos a padres.
        foreach (var experiencia in basica.Experiencia)
        {
            _db.LogrosLaborales.RemoveRange(experiencia.Logros);
            _db.Competencias.RemoveRange(experiencia.Competencias);
        }

        _db.InformacionLaboral.RemoveRange(basica.Experiencia);
        _db.FormacionAcademica.RemoveRange(basica.Formacion);
        _db.InformacionBasica.Remove(basica);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Los catalogos se cachean 6 horas; al tocar Parametros hay que refrescarlos.</summary>
    private void InvalidarCacheCatalogos()
    {
        foreach (var codigo in GrupoParametro.Nombres.Keys)
            _cache.Remove($"cat:param:{codigo}");
    }
}
