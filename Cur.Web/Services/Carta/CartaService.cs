using Cur.Web.Data;
using Cur.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cur.Web.Services.Carta;

public interface ICartaService
{
    Task<CartaPresentacion?> ObtenerAsync(string userId, CancellationToken ct = default);

    /// <summary>Crea o actualiza la única carta del usuario.</summary>
    Task GuardarAsync(string userId, CartaPresentacion carta, CancellationToken ct = default);

    Task<bool> EliminarAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// Lectura y escritura de la carta de presentación. Como en el resto de servicios, todo
/// se filtra por el UserId de Identity para que nadie toque la carta de otro.
/// </summary>
public class CartaService : ICartaService
{
    private readonly ApplicationDbContext _db;

    public CartaService(ApplicationDbContext db) => _db = db;

    public async Task<CartaPresentacion?> ObtenerAsync(string userId, CancellationToken ct = default) =>
        await _db.CartasPresentacion.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task GuardarAsync(string userId, CartaPresentacion carta, CancellationToken ct = default)
    {
        var actual = await _db.CartasPresentacion.FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (actual is null)
        {
            actual = new CartaPresentacion { UserId = userId };
            _db.CartasPresentacion.Add(actual);
        }

        actual.CargoObjetivo = carta.CargoObjetivo;
        actual.Empresa = carta.Empresa;
        actual.Tono = carta.Tono;
        actual.Texto = carta.Texto;
        actual.IncluirEnHojaDeVida = carta.IncluirEnHojaDeVida;
        actual.ActualizadaEn = DateTime.Now;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> EliminarAsync(string userId, CancellationToken ct = default)
    {
        var actual = await _db.CartasPresentacion.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (actual is null) return false;

        _db.CartasPresentacion.Remove(actual);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
