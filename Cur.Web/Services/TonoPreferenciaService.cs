using System.Security.Claims;
using Cur.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Cur.Web.Services;

public interface ITonoPreferencia
{
    /// <summary>Lee el tono elegido desde los claims de la cookie de sesión.</summary>
    TonoCarta Obtener(ClaimsPrincipal usuario);

    /// <summary>Indica si el usuario ya respondió el cuestionario alguna vez.</summary>
    bool EstaDefinido(ClaimsPrincipal usuario);

    /// <summary>Persiste la elección y refresca la sesión para que el claim quede vigente.</summary>
    Task GuardarAsync(ClaimsPrincipal usuario, TonoCarta tono);
}

/// <summary>
/// Guarda el tono preferido como un claim en AspNetUserClaims, igual que
/// <see cref="PlantillaPreferenciaService"/>. Es un valor corto y de solo lectura para la
/// aplicación; el texto de la carta, que es largo, va en su propia tabla y nunca aquí,
/// porque los claims terminan viajando dentro de la cookie de autenticación.
/// </summary>
public class TonoPreferenciaService : ITonoPreferencia
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public TonoPreferenciaService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public TonoCarta Obtener(ClaimsPrincipal usuario) =>
        Tonos.Parsear(usuario.FindFirstValue(Tonos.TipoClaim));

    public bool EstaDefinido(ClaimsPrincipal usuario) =>
        usuario.FindFirst(Tonos.TipoClaim) is not null;

    public async Task GuardarAsync(ClaimsPrincipal usuario, TonoCarta tono)
    {
        var entidad = await _userManager.GetUserAsync(usuario);
        if (entidad is null) return;

        var existentes = (await _userManager.GetClaimsAsync(entidad))
            .Where(c => c.Type == Tonos.TipoClaim)
            .ToList();

        if (existentes.Count > 0)
            await _userManager.RemoveClaimsAsync(entidad, existentes);

        await _userManager.AddClaimAsync(entidad, new Claim(Tonos.TipoClaim, tono.ToString()));

        // Sin esto el claim nuevo no llega a la cookie hasta el proximo inicio de sesion.
        await _signInManager.RefreshSignInAsync(entidad);
    }
}
