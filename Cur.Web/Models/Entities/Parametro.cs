namespace Cur.Web.Models.Entities;

/// <summary>
/// Catalogo generico de la tabla Parametros. El campo <see cref="Codigo"/> agrupa
/// los valores por tipo (ver <see cref="GrupoParametro"/>).
/// </summary>
public class Parametro
{
    public int ParametroId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Codigo { get; set; }
}

/// <summary>Valores de Parametros.Cdgo que agrupan cada catalogo.</summary>
public static class GrupoParametro
{
    public const int EstadoLaboral = 10;
    public const int EstadoFormacion = 11;
    public const int Rol = 12;
    public const int AreaOcupacional = 13;
    public const int Cargo = 14;
    public const int Titulacion = 15;
    public const int TipoFormacion = 16;
    public const int TipoAptitud = 17;
    public const int TipoLogro = 18;

    /// <summary>Nombre legible de cada grupo, para el panel de administracion.</summary>
    public static readonly IReadOnlyDictionary<int, string> Nombres = new Dictionary<int, string>
    {
        [EstadoLaboral] = "Estados laborales",
        [EstadoFormacion] = "Estados de formación",
        [Rol] = "Roles",
        [AreaOcupacional] = "Áreas ocupacionales",
        [Cargo] = "Cargos",
        [Titulacion] = "Titulaciones",
        [TipoFormacion] = "Tipos de formación",
        [TipoAptitud] = "Tipos de aptitud",
        [TipoLogro] = "Tipos de logro"
    };

    /// <summary>Icono de Bootstrap Icons asociado a cada grupo.</summary>
    public static readonly IReadOnlyDictionary<int, string> Iconos = new Dictionary<int, string>
    {
        [EstadoLaboral] = "bi-toggle-on",
        [EstadoFormacion] = "bi-hourglass-split",
        [Rol] = "bi-shield-lock",
        [AreaOcupacional] = "bi-diagram-3",
        [Cargo] = "bi-person-badge",
        [Titulacion] = "bi-mortarboard",
        [TipoFormacion] = "bi-journal-bookmark",
        [TipoAptitud] = "bi-lightbulb",
        [TipoLogro] = "bi-trophy"
    };

    public static string Nombre(int codigo) =>
        Nombres.TryGetValue(codigo, out var nombre) ? nombre : $"Grupo {codigo}";

    public static string Icono(int codigo) =>
        Iconos.TryGetValue(codigo, out var icono) ? icono : "bi-tag";

    /// <summary>Grupos que la aplicacion consume directamente y no conviene dejar vacios.</summary>
    public static readonly IReadOnlySet<int> EnUso = new HashSet<int>
    {
        EstadoLaboral, EstadoFormacion, AreaOcupacional, Cargo,
        Titulacion, TipoFormacion, TipoAptitud, TipoLogro
    };
}
