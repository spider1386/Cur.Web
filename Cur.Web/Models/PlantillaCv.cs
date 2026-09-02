namespace Cur.Web.Models;

/// <summary>Diseños disponibles para el PDF de la hoja de vida.</summary>
public enum PlantillaCv
{
    Clasica = 0,
    Ejecutiva = 1,
    Minimal = 2,
    Timeline = 3
}

public record PlantillaInfo(PlantillaCv Valor, string Nombre, string Descripcion, string Icono);

public static class Plantillas
{
    /// <summary>Tipo de claim donde se guarda la preferencia del usuario.</summary>
    public const string TipoClaim = "cv:plantilla";

    public const PlantillaCv PorDefecto = PlantillaCv.Clasica;

    public static readonly IReadOnlyList<PlantillaInfo> Catalogo = new List<PlantillaInfo>
    {
        new(PlantillaCv.Clasica, "Clásica",
            "Encabezado azul con foto, una columna y tabla de competencias. El diseño original.",
            "bi-file-earmark-text"),

        new(PlantillaCv.Ejecutiva, "Ejecutiva",
            "Dos columnas: barra lateral oscura con foto, contacto y competencias; el resto a la derecha.",
            "bi-layout-sidebar-inset"),

        new(PlantillaCv.Minimal, "Minimal",
            "Una columna, sin color de fondo. Jerarquía por tipografía y líneas finas. Ideal para filtros ATS.",
            "bi-type"),

        new(PlantillaCv.Timeline, "Timeline",
            "Experiencia y formación sobre una línea de tiempo vertical con marcadores por periodo.",
            "bi-list-nested")
    };

    public static PlantillaInfo Info(PlantillaCv plantilla) =>
        Catalogo.FirstOrDefault(p => p.Valor == plantilla) ?? Catalogo[0];

    /// <summary>Convierte el valor guardado en el claim; ante cualquier basura vuelve al valor por defecto.</summary>
    public static PlantillaCv Parsear(string? valor) =>
        Enum.TryParse<PlantillaCv>(valor, ignoreCase: true, out var p) && Enum.IsDefined(p)
            ? p
            : PorDefecto;
}
