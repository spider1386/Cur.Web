namespace Cur.Web.Models;

/// <summary>
/// Roles de la tabla AspNetRoles. El registro publico siempre asigna
/// <see cref="Usuario"/>; <see cref="Administrador"/> no se otorga desde la aplicacion.
/// </summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Usuario = "Usuario";
}
