using System.Text;
using Cur.Web.Models;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;

namespace Cur.Web.Services.Carta;

/// <summary>Lo que el usuario aporta desde el formulario para armar el borrador.</summary>
public record SolicitudCarta(
    string CargoObjetivo,
    string? Empresa,
    string? Motivacion,
    IReadOnlyList<int> LogrosIds,
    IReadOnlyList<int> CompetenciasIds);

public interface IRedactorCarta
{
    /// <summary>
    /// Arma un BORRADOR de carta con los datos reales de la hoja de vida. El texto que
    /// vale es el que el usuario deje después de editarlo: esto solo evita la hoja en blanco.
    /// </summary>
    string Redactar(CurriculumViewModel cv, SolicitudCarta solicitud, TonoCarta tono);
}

/// <summary>
/// Redacción por plantillas: sin dependencias externas y sin que los datos del candidato
/// salgan del servidor. El tono cambia la apertura, los conectores, el orden y el cierre;
/// los hechos (logros y competencias) siempre salen tal como el usuario los escribió, sin
/// adjetivos añadidos, porque son la parte que un reclutador puede verificar.
///
/// Si más adelante se quiere redacción con un modelo de lenguaje, basta con otra
/// implementación de <see cref="IRedactorCarta"/>: nada más del proyecto cambia.
/// </summary>
public class RedactorCarta : IRedactorCarta
{
    public string Redactar(CurriculumViewModel cv, SolicitudCarta solicitud, TonoCarta tono)
    {
        var basica = cv.Basica;
        var logros = LogrosElegidos(cv, solicitud.LogrosIds);
        var competencias = CompetenciasElegidas(cv, solicitud.CompetenciasIds);

        var cargo = solicitud.CargoObjetivo.Trim();
        var empresa = string.IsNullOrWhiteSpace(solicitud.Empresa) ? null : solicitud.Empresa.Trim();
        var enEmpresa = empresa is null ? string.Empty : $" en {empresa}";
        var profesion = basica?.Profesion?.Descripcion?.ToLowerInvariant() ?? "profesional";
        var trayectoria = Trayectoria(cv);
        var reciente = PasoReciente(cv, tono);

        var partes = new List<string>
        {
            empresa is null ? "Respetados señores:" : $"Señores {empresa}:"
        };

        partes.Add(tono switch
        {
            TonoCarta.Directo => Unir(
                $"Me postulo al cargo de {cargo}{enEmpresa}",
                $"Soy {profesion} y llevo {trayectoria} de ejercicio{reciente}",
                "Estos son los resultados que traigo"),

            TonoCarta.Cercano => Unir(
                $"Me entusiasma postularme al cargo de {cargo}{enEmpresa}",
                $"Soy {profesion} y llevo {trayectoria} trabajando codo a codo con equipos muy distintos{reciente}"),

            TonoCarta.Sereno => Unir(
                $"Presento mi candidatura al cargo de {cargo}{enEmpresa}",
                $"Soy {profesion} y he construido {trayectoria} de trayectoria con continuidad y compromiso{reciente}"),

            TonoCarta.Preciso => Unir(
                $"Me permito postularme al cargo de {cargo}{enEmpresa}",
                $"Soy {profesion}, con {trayectoria} de experiencia{reciente}"),

            _ => Unir(
                $"Me permito presentar mi candidatura al cargo de {cargo}{enEmpresa}",
                $"Soy {profesion} con {trayectoria} de experiencia{reciente}")
        });

        if (logros.Count > 0)
        {
            var encabezado = tono switch
            {
                TonoCarta.Directo => null, // el párrafo anterior ya anuncia los resultados
                TonoCarta.Cercano => "Si tuviera que contarles lo que más me representa, sería esto:",
                TonoCarta.Sereno => "Estos son algunos de los resultados que he sostenido en el tiempo:",
                TonoCarta.Preciso => "Relaciono los resultados más relevantes, con su alcance:",
                _ => "Estos son algunos de los resultados que respaldan mi postulación:"
            };

            if (encabezado is not null) partes.Add(encabezado);
            partes.Add(ListaLogros(logros, tono));
        }

        if (competencias.Count > 0)
        {
            if (tono == TonoCarta.Preciso)
            {
                partes.Add("Competencias y nivel declarado:");
                partes.Add(ListaCompetencias(competencias));
            }
            else
            {
                var enumeradas = Enumerar(competencias.Select(c => c.Descripcion.Trim()).ToList());

                partes.Add(tono switch
                {
                    TonoCarta.Directo => Unir($"Lo hago con {enumeradas}"),
                    TonoCarta.Cercano => Unir($"En el día a día aporto {enumeradas}"),
                    TonoCarta.Sereno => Unir($"Aporto además {enumeradas}, y una forma de trabajo estable y colaborativa"),
                    _ => Unir($"Para lograrlo me apoyo en {enumeradas}")
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(solicitud.Motivacion))
            partes.Add(Unir(solicitud.Motivacion));

        partes.Add(tono switch
        {
            TonoCarta.Directo => "Quedo atento para concretar una entrevista.",
            TonoCarta.Cercano => "Me encantaría conversar con ustedes y conocer de cerca lo que están construyendo.",
            TonoCarta.Sereno => "Agradezco su tiempo y quedo a su disposición para ampliar cualquier punto.",
            TonoCarta.Preciso => "Quedo atento a la etapa que corresponda dentro del proceso de selección.",
            _ => "Quedo atento a la posibilidad de una entrevista. Agradezco su tiempo."
        });

        partes.Add(Firma(basica));

        return string.Join("\n\n", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    // ---------- Bloques ----------

    private static string ListaLogros(IReadOnlyList<LogroLaboral> logros, TonoCarta tono)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < logros.Count; i++)
        {
            var logro = logros[i];
            var titulo = Pdf.PlantillaComun.LimpiarVinieta(logro.Logro);
            var detalle = Pdf.PlantillaComun.LimpiarVinieta(logro.Descripcion);

            // La numeración refuerza el registro del tono preciso; el resto lleva viñeta.
            sb.Append(tono == TonoCarta.Preciso ? $"{i + 1}. " : "• ");
            sb.Append(titulo);
            if (!string.IsNullOrWhiteSpace(detalle)) sb.Append($": {detalle}");
            if (i < logros.Count - 1) sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string ListaCompetencias(IReadOnlyList<Competencia> competencias) =>
        string.Join('\n', competencias.Select(c =>
        {
            var nivel = string.IsNullOrWhiteSpace(c.Medicion) ? null : $" — {c.Medicion.Trim()}";
            return $"• {c.Descripcion.Trim()}{nivel}";
        }));

    private static string Firma(InformacionBasica? basica)
    {
        var lineas = new List<string> { "Cordialmente," };

        if (basica is null) return string.Join("\n", lineas);

        lineas.Add(string.Empty);
        lineas.Add(basica.NombreCompleto);

        if (!string.IsNullOrWhiteSpace(basica.Profesion?.Descripcion))
            lineas.Add(basica.Profesion!.Descripcion);

        var contacto = Pdf.PlantillaComun.Unir(" · ", basica.Email, basica.TelefonoMovil);
        if (!string.IsNullOrWhiteSpace(contacto)) lineas.Add(contacto);

        return string.Join("\n", lineas);
    }

    // ---------- Apoyo ----------

    private static IReadOnlyList<LogroLaboral> LogrosElegidos(CurriculumViewModel cv, IReadOnlyList<int> ids) =>
        cv.Experiencia
            .SelectMany(e => e.Logros)
            .Where(l => ids.Contains(l.LogroId))
            .ToList();

    private static IReadOnlyList<Competencia> CompetenciasElegidas(CurriculumViewModel cv, IReadOnlyList<int> ids) =>
        cv.Experiencia
            .SelectMany(e => e.Competencias)
            .Where(c => ids.Contains(c.CompetenciaId))
            .ToList();

    /// <summary>
    /// Años entre el primer ingreso y el último retiro. Se mide el lapso y no la suma de
    /// cada cargo, que inflaría el dato cuando hay empleos solapados.
    /// </summary>
    private static string Trayectoria(CurriculumViewModel cv)
    {
        if (cv.Experiencia.Count == 0) return "mi experiencia";

        var inicio = cv.Experiencia.Min(e => e.FechaInicio);
        var fin = cv.Experiencia.Max(e => e.FechaRetiro ?? DateTime.Today);

        var meses = ((fin.Year - inicio.Year) * 12) + fin.Month - inicio.Month;
        if (fin.Day < inicio.Day) meses--;

        var anios = Math.Max(meses, 0) / 12;

        return anios switch
        {
            0 => "menos de un año",
            1 => "un año",
            _ => $"{anios} años"
        };
    }

    private static string PasoReciente(CurriculumViewModel cv, TonoCarta tono)
    {
        // La lista ya viene ordenada de la experiencia más nueva a la más antigua.
        var ultima = cv.Experiencia.FirstOrDefault();
        if (ultima is null) return string.Empty;

        var cargo = ultima.Cargo?.Descripcion;
        if (string.IsNullOrWhiteSpace(cargo)) return string.Empty;

        var vigente = ultima.FechaRetiro is null;

        return tono switch
        {
            TonoCarta.Cercano when vigente => $", hoy como {cargo} en {ultima.Empresa}",
            TonoCarta.Cercano => $", la más reciente como {cargo} en {ultima.Empresa}",
            _ when vigente => $", actualmente como {cargo} en {ultima.Empresa}",
            _ => $", con mi paso más reciente como {cargo} en {ultima.Empresa}"
        };
    }

    /// <summary>
    /// Cierra cada oración con un solo punto y las une con un espacio. Los datos traen
    /// razones sociales como "Sistemas Integrados S.A.S.", que ya terminan en punto:
    /// concatenar a ciegas produciría "S.A.S..".
    /// </summary>
    private static string Unir(params string[] oraciones) =>
        string.Join(' ', oraciones
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .Select(o => o.EndsWith('.') || o.EndsWith('!') || o.EndsWith('?') ? o : o + '.'));

    /// <summary>Enumera en español: "a", "a y b", "a, b y c".</summary>
    private static string Enumerar(IReadOnlyList<string> valores) => valores.Count switch
    {
        0 => string.Empty,
        1 => valores[0],
        2 => $"{valores[0]} y {valores[1]}",
        _ => $"{string.Join(", ", valores.Take(valores.Count - 1))} y {valores[^1]}"
    };
}
