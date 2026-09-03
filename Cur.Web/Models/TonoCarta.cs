namespace Cur.Web.Models;

/// <summary>
/// Registro con el que se redacta la carta de presentacion.
///
/// Internamente las cuatro opciones siguen las dimensiones del modelo DISC de Marston
/// (D dominancia, I influencia, S estabilidad, C cumplimiento), pero de cara al usuario
/// esto es una preferencia de redaccion y NO un perfil de personalidad: no se presenta
/// como prueba, no se muestra un tipo y no se usa para evaluar ni comparar a nadie.
/// Las preguntas son propias; no se copio ningun cuestionario comercial.
/// </summary>
public enum TonoCarta
{
    Equilibrado = 0,
    Directo = 1,
    Cercano = 2,
    Sereno = 3,
    Preciso = 4
}

public record TonoInfo(TonoCarta Valor, string Nombre, string Descripcion, string Icono);

public static class Tonos
{
    /// <summary>Tipo de claim donde se guarda la preferencia del usuario.</summary>
    public const string TipoClaim = "cv:tono";

    public const TonoCarta PorDefecto = TonoCarta.Equilibrado;

    public static readonly IReadOnlyList<TonoInfo> Catalogo = new List<TonoInfo>
    {
        new(TonoCarta.Equilibrado, "Equilibrado",
            "Profesional y neutro. Sirve para casi cualquier convocatoria.",
            "bi-circle-half"),

        new(TonoCarta.Directo, "Directo",
            "Al grano: primero el resultado, frases cortas y cifras al frente.",
            "bi-lightning-charge"),

        new(TonoCarta.Cercano, "Cercano",
            "Cálido y narrativo, con énfasis en el equipo y la relación.",
            "bi-people"),

        new(TonoCarta.Sereno, "Sereno",
            "Tranquilo y colaborativo, centrado en continuidad y compromiso.",
            "bi-hand-thumbs-up"),

        new(TonoCarta.Preciso, "Preciso",
            "Estructurado y verificable, con método y datos por delante.",
            "bi-rulers")
    };

    public static TonoInfo Info(TonoCarta tono) =>
        Catalogo.FirstOrDefault(t => t.Valor == tono) ?? Catalogo[0];

    /// <summary>Convierte el valor guardado en el claim; ante cualquier basura vuelve al valor por defecto.</summary>
    public static TonoCarta Parsear(string? valor) =>
        Enum.TryParse<TonoCarta>(valor, ignoreCase: true, out var t) && Enum.IsDefined(t)
            ? t
            : PorDefecto;
}
