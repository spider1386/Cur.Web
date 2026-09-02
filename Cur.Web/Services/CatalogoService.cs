using Cur.Web.Data;
using Cur.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cur.Web.Services;

public interface ICatalogoService
{
    Task<IEnumerable<SelectListItem>> ParametrosAsync(int grupo, int? seleccionado = null, CancellationToken ct = default);
    Task<IEnumerable<SelectListItem>> DepartamentosAsync(int? seleccionado = null, CancellationToken ct = default);
    Task<IEnumerable<SelectListItem>> MunicipiosAsync(int departamentoId, int? seleccionado = null, CancellationToken ct = default);
}

/// <summary>
/// Lee los catálogos (Parametros, Departamentos, Municipios) y los expone como listas
/// desplegables. Son datos casi estáticos, por eso se cachean en memoria.
/// </summary>
public class CatalogoService : ICatalogoService
{
    private static readonly TimeSpan Expiracion = TimeSpan.FromHours(6);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public CatalogoService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IEnumerable<SelectListItem>> ParametrosAsync(int grupo, int? seleccionado = null, CancellationToken ct = default)
    {
        var items = await _cache.GetOrCreateAsync($"cat:param:{grupo}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Expiracion;
            return await _db.Parametros.AsNoTracking()
                .Where(p => p.Codigo == grupo)
                .OrderBy(p => p.Descripcion)
                .Select(p => new OpcionCatalogo(p.ParametroId, p.Descripcion))
                .ToListAsync(ct);
        }) ?? new List<OpcionCatalogo>();

        return AConSelectList(items, seleccionado);
    }

    public async Task<IEnumerable<SelectListItem>> DepartamentosAsync(int? seleccionado = null, CancellationToken ct = default)
    {
        var items = await _cache.GetOrCreateAsync("cat:dptos", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Expiracion;
            return await _db.Departamentos.AsNoTracking()
                .OrderBy(d => d.Nombre)
                .Select(d => new OpcionCatalogo(d.DepartamentoId, d.Nombre))
                .ToListAsync(ct);
        }) ?? new List<OpcionCatalogo>();

        return AConSelectList(items, seleccionado);
    }

    public async Task<IEnumerable<SelectListItem>> MunicipiosAsync(int departamentoId, int? seleccionado = null, CancellationToken ct = default)
    {
        if (departamentoId <= 0) return Array.Empty<SelectListItem>();

        var items = await _cache.GetOrCreateAsync($"cat:mpios:{departamentoId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Expiracion;
            return await _db.Municipios.AsNoTracking()
                .Where(m => m.DepartamentoId == departamentoId)
                .OrderBy(m => m.Nombre)
                .Select(m => new OpcionCatalogo(m.MunicipioId, m.Nombre))
                .ToListAsync(ct);
        }) ?? new List<OpcionCatalogo>();

        return AConSelectList(items, seleccionado);
    }

    private static IEnumerable<SelectListItem> AConSelectList(IEnumerable<OpcionCatalogo> items, int? seleccionado) =>
        items.Select(i => new SelectListItem
        {
            Value = i.Id.ToString(),
            Text = i.Texto,
            Selected = seleccionado.HasValue && seleccionado.Value == i.Id
        }).ToList();

    private record OpcionCatalogo(int Id, string Texto);
}
