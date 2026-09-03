namespace Cur.Web.Models;

public record PreguntaTono(int Numero, string Texto, TonoCarta Dimension);

/// <summary>
/// Cuestionario corto que sugiere el <see cref="TonoCarta"/> con el que se redacta la carta.
///
/// Ocho afirmaciones, dos por dimension, con escala de 1 a 5. Deliberadamente NO es
/// ipsativo ("lo que mas / lo que menos me describe"): esa forma produce puntajes que
/// solo valen dentro de la misma persona y aqui no hay nada que comparar entre personas.
/// El resultado es una sugerencia de redaccion, siempre editable por el usuario.
/// </summary>
public static class CuestionarioTono
{
    public const int Minimo = 1;
    public const int Maximo = 5;
    public const int Neutro = 3;

    /// <summary>Puntaje que se obtiene respondiendo las dos preguntas de una dimension en el punto neutro.</summary>
    public const int Umbral = Neutro * 2;

    public static readonly IReadOnlyList<string> Escala = new[]
    {
        "Nada de acuerdo",
        "Poco de acuerdo",
        "Ni de acuerdo ni en desacuerdo",
        "De acuerdo",
        "Muy de acuerdo"
    };

    public static readonly IReadOnlyList<PreguntaTono> Preguntas = new List<PreguntaTono>
    {
        new(1, "Prefiero ir al grano: primero el resultado y después el contexto.", TonoCarta.Directo),
        new(2, "Me siento cómodo decidiendo rápido y asumiendo lo que implique.", TonoCarta.Directo),

        new(3, "Me motiva convencer y entusiasmar a otros con una idea.", TonoCarta.Cercano),
        new(4, "Hago relaciones con facilidad y rindo mejor rodeado de gente.", TonoCarta.Cercano),

        new(5, "Valoro la estabilidad y sostengo mis compromisos en el tiempo.", TonoCarta.Sereno),
        new(6, "Prefiero que al equipo le vaya bien antes que destacar yo.", TonoCarta.Sereno),

        new(7, "Reviso el detalle y verifico los datos antes de dar algo por terminado.", TonoCarta.Preciso),
        new(8, "Trabajo mejor con métodos, normas y procedimientos claros.", TonoCarta.Preciso)
    };

    /// <summary>
    /// Suma por dimension y devuelve la mas alta. Si ninguna supera el punto neutro, o si
    /// hay empate en el primer lugar, devuelve <see cref="TonoCarta.Equilibrado"/>: no hay
    /// preferencia marcada y forzar una seria inventarla.
    /// </summary>
    public static TonoCarta Calcular(IReadOnlyList<int> respuestas)
    {
        if (respuestas.Count != Preguntas.Count) return Tonos.PorDefecto;

        var puntajes = new Dictionary<TonoCarta, int>();

        for (var i = 0; i < Preguntas.Count; i++)
        {
            var valor = Math.Clamp(respuestas[i], Minimo, Maximo);
            var dimension = Preguntas[i].Dimension;
            puntajes[dimension] = puntajes.GetValueOrDefault(dimension) + valor;
        }

        var mayor = puntajes.Values.Max();
        if (mayor <= Umbral) return Tonos.PorDefecto;

        var lideres = puntajes.Where(p => p.Value == mayor).Select(p => p.Key).ToList();
        return lideres.Count == 1 ? lideres[0] : Tonos.PorDefecto;
    }
}
