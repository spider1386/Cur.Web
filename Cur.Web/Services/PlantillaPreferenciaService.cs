using System.Security.Claims;
using Cur.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Cur.Web.Services;

public interface IPlantillaPreferencia
{
    /// <summary>Lee la plantilla elegida desde los claims de la cookie de sesión.</summary>
    PlantillaCv Obtener(ClaimsPrincipal usuario);

    /// <summary>Persiste la elección y refresca la sesión para que el claim quede vigente.</summary>
    Task GuardarAsync(ClaimsPrincipal usuario, PlantillaCv plantilla);
}

/// <summary>
/// Guarda la plantilla preferida como un claim en AspNetUserClaims. Se usa esa tabla,
/// que ya existe en la base, para no alterar el esquema heredado de negocio.
/// </summary>
public class PlantillaPreferenciaService : IPlantillaPreferencia
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public PlantillaPreferenciaService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public PlantillaCv Obtener(ClaimsPrincipal usuario) =>
        Plantillas.Parsear(usuario.FindFirstValue(Plantillas.TipoClaim));

    public async Task GuardarAsync(ClaimsPrincipal usuario, PlantillaCv plantilla)
    {
        var entidad = await _userManager.GetUserAsync(usuario);
        if (entidad is null) return;

        var existentes = (await _userManager.GetClaimsAsync(entidad))
            .Where(c => c.Type == Plantillas.TipoClaim)
            .ToList();

        if (existentes.Count > 0)
            await _userManager.RemoveClaimsAsync(entidad, existentes);

        await _userManager.AddClaimAsync(entidad, new Claim(Plantillas.TipoClaim, plantilla.ToString()));

        // Sin esto el claim nuevo no llega a la cookie hasta el proximo inicio de sesion.
        await _signInManager.RefreshSignInAsync(entidad);
    }
}
