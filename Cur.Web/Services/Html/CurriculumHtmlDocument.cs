using System.Globalization;
using System.Net;
using System.Text;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using Cur.Web.Services.Pdf;

namespace Cur.Web.Services.Html;

/// <summary>
/// Arma el HTML autocontenido de la hoja de vida: un solo archivo, con los estilos
/// embebidos y la foto en base64, para que se pueda abrir sin conexion y sin recursos
/// externos. El contenido es el mismo del PDF; solo cambia el medio.
/// </summary>
internal sealed class CurriculumHtmlDocument
{
    private static readonly CultureInfo Cultura = PlantillaComun.Cultura;

    private readonly CurriculumViewModel _cv;
    private readonly InformacionBasica _basica;
    private readonly TemaHtml _tema;
    private readonly string? _foto;
    private readonly string? _carta;
    private readonly IReadOnlyList<Competencia> _competencias;

    public CurriculumHtmlDocument(CurriculumViewModel cv, byte[]? foto, TemaHtml tema, string? carta = null)
    {
        _cv = cv;
        _basica = cv.Basica ?? new InformacionBasica();
        _tema = tema;
        _foto = tema.MostrarFoto ? ComoDataUri(foto) : null;
        _carta = string.IsNullOrWhiteSpace(carta) ? null : carta;
        _competencias = cv.Experiencia.SelectMany(e => e.Competencias).ToList();
    }

    public string Render()
    {
        var sb = new StringBuilder(16_384);
        var titulo = E($"Hoja de vida - {_basica.NombreCompleto}".Trim());

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"es-CO\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.AppendLine($"<meta name=\"author\" content=\"{E(_basica.NombreCompleto)}\" />");
        sb.AppendLine($"<title>{titulo}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(TemaHtml.CssBase);
        sb.AppendLine(_tema.Css);
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<article class=\"hv {_tema.Clase}\">");

        Portada(sb);

        if (_tema.BarraLateral) CuerpoConLateral(sb);
        else CuerpoSimple(sb);

        Pie(sb);

        sb.AppendLine("</article>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    // ---------- Estructuras de pagina ----------

    /// <summary>Carta de presentacion antepuesta a la hoja de vida. Al imprimir queda en su propia hoja.</summary>
    private void Portada(StringBuilder sb)
    {
        if (_carta is null) return;

        sb.AppendLine("<section class=\"hv-portada\">");
        sb.AppendLine("<h2 class=\"hv-seccion-titulo\">Carta de presentación</h2>");

        foreach (var bloque in Bloques(_carta))
            sb.AppendLine($"<p>{E(bloque)}</p>");

        sb.AppendLine("</section>");
    }

    /// <summary>
    /// Parte el texto en bloques separados por linea en blanco. Dentro de cada bloque los
    /// saltos se conservan con white-space: pre-wrap, para que las viñetas no se peguen.
    /// </summary>
    private static IEnumerable<string> Bloques(string texto) =>
        texto.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Trim('\n'))
            .Where(b => !string.IsNullOrWhiteSpace(b));

    private void CuerpoSimple(StringBuilder sb)
    {
        sb.AppendLine("<header class=\"hv-encabezado\">");
        if (_foto is not null)
            sb.AppendLine($"<img class=\"hv-foto\" src=\"{_foto}\" alt=\"Foto de perfil\" />");

        sb.AppendLine("<div>");
        sb.AppendLine($"<h1>{E(NombreOTitulo())}</h1>");
        if (!string.IsNullOrWhiteSpace(_basica.Profesion?.Descripcion))
            sb.AppendLine($"<p class=\"hv-profesion\">{E(_basica.Profesion!.Descripcion)}</p>");

        sb.AppendLine("<ul class=\"hv-contacto\">");
        foreach (var (etiqueta, valor) in Contacto())
            sb.AppendLine($"<li><b>{E(etiqueta)}:</b> {E(valor)}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("</div>");
        sb.AppendLine("</header>");
        sb.AppendLine("<div class=\"hv-regla\"></div>");

        sb.AppendLine("<div class=\"hv-cuerpo\">");
        SeccionPerfil(sb);
        SeccionExperiencia(sb);
        SeccionFormacion(sb);
        SeccionCompetencias(sb);
        sb.AppendLine("</div>");
    }

    private void CuerpoConLateral(StringBuilder sb)
    {
        sb.AppendLine("<div class=\"hv-cuerpo\">");

        sb.AppendLine("<aside class=\"hv-lateral\">");
        if (_foto is not null)
            sb.AppendLine($"<img class=\"hv-foto\" src=\"{_foto}\" alt=\"Foto de perfil\" />");

        sb.AppendLine($"<h1>{E(NombreOTitulo())}</h1>");
        if (!string.IsNullOrWhiteSpace(_basica.Profesion?.Descripcion))
            sb.AppendLine($"<p class=\"hv-profesion\">{E(_basica.Profesion!.Descripcion)}</p>");

        sb.AppendLine("<p class=\"hv-lateral-titulo\">Contacto</p>");
        foreach (var (etiqueta, valor) in Contacto())
            sb.AppendLine($"<div class=\"hv-dato\"><span>{E(etiqueta)}</span><b>{E(valor)}</b></div>");

        if (_competencias.Count > 0)
        {
            sb.AppendLine("<p class=\"hv-lateral-titulo\">Competencias</p>");
            sb.AppendLine("<ul class=\"hv-comp-lateral\">");
            foreach (var c in _competencias)
            {
                var detalle = PlantillaComun.Unir(" · ", c.TipoCompetencia?.Descripcion, c.Medicion);
                sb.Append($"<li>{E(c.Descripcion)}");
                if (!string.IsNullOrWhiteSpace(detalle)) sb.Append($"<small>{E(detalle)}</small>");
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul>");
        }
        sb.AppendLine("</aside>");

        sb.AppendLine("<div class=\"hv-principal\">");
        SeccionPerfil(sb);
        SeccionExperiencia(sb);
        SeccionFormacion(sb);
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
    }

    // ---------- Secciones ----------

    private void SeccionPerfil(StringBuilder sb)
    {
        if (string.IsNullOrWhiteSpace(_basica.PerfilProfesional)) return;

        AbrirSeccion(sb, "Perfil profesional");
        sb.AppendLine($"<p class=\"hv-perfil\">{EnParrafo(_basica.PerfilProfesional)}</p>");
        sb.AppendLine("</section>");
    }

    private void SeccionExperiencia(StringBuilder sb)
    {
        if (_cv.Experiencia.Count == 0) return;

        AbrirSeccion(sb, "Experiencia laboral");
        sb.AppendLine("<div class=\"hv-lista\">");

        foreach (var exp in _cv.Experiencia)
        {
            sb.AppendLine("<div class=\"hv-item\">");
            sb.AppendLine("<div class=\"hv-item-fila\">");
            sb.AppendLine("<div>");
            sb.AppendLine($"<p class=\"hv-item-titulo\">{E(exp.Cargo?.Descripcion ?? "Cargo")}</p>");
            sb.AppendLine($"<p class=\"hv-item-sub\">{E(exp.Empresa)}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"hv-fechas\">");
            sb.AppendLine($"<div>{E(PlantillaComun.Periodo(exp.FechaInicio, exp.FechaRetiro))}</div>");
            sb.AppendLine($"<div>{E(PlantillaComun.Duracion(exp.TiempoLaborado ?? exp.CalcularMesesLaborados()))}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            if (!string.IsNullOrWhiteSpace(exp.Area?.Descripcion))
                sb.AppendLine($"<p class=\"hv-meta\">Área: {E(exp.Area!.Descripcion)}</p>");

            var logros = exp.Logros.OrderBy(l => l.Tipo?.Descripcion).ToList();
            if (logros.Count > 0)
            {
                sb.AppendLine("<ul class=\"hv-logros\">");
                foreach (var logro in logros)
                {
                    sb.Append("<li><span class=\"hv-vineta\"></span><div>");
                    sb.Append($"<span class=\"hv-logro-nombre\">{E(PlantillaComun.LimpiarVinieta(logro.Logro))}</span>");
                    if (!string.IsNullOrWhiteSpace(logro.Descripcion))
                        sb.Append($" — {E(PlantillaComun.LimpiarVinieta(logro.Descripcion))}");
                    sb.AppendLine("</div></li>");
                }
                sb.AppendLine("</ul>");
            }

            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</section>");
    }

    private void SeccionFormacion(StringBuilder sb)
    {
        if (_cv.Formacion.Count == 0) return;

        AbrirSeccion(sb, "Formación académica");
        sb.AppendLine("<div class=\"hv-lista\">");

        foreach (var f in _cv.Formacion)
        {
            var detalle = PlantillaComun.Unir(" • ",
                f.TipoFormacion?.Descripcion,
                f.Estado?.Descripcion,
                string.IsNullOrWhiteSpace(f.Intensidad) ? null : $"{f.Intensidad} h");

            sb.AppendLine("<div class=\"hv-item\">");
            sb.AppendLine("<div class=\"hv-item-fila\">");
            sb.AppendLine("<div>");
            sb.AppendLine($"<p class=\"hv-item-titulo\">{E(f.TituloOtorgado)}</p>");
            sb.AppendLine($"<p class=\"hv-item-sub\">{E(f.Institucion)}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"hv-fechas\">");
            sb.AppendLine($"<div>{E(PlantillaComun.Periodo(f.FechaInicio, f.FechaFinalizacion))}</div>");
            if (!string.IsNullOrWhiteSpace(detalle))
                sb.AppendLine($"<div>{E(detalle)}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</div>");
        sb.AppendLine("</section>");
    }

    private void SeccionCompetencias(StringBuilder sb)
    {
        if (_competencias.Count == 0) return;

        AbrirSeccion(sb, "Competencias y habilidades");
        sb.AppendLine("<table class=\"hv-tabla\">");
        sb.AppendLine("<thead><tr><th>Competencia / habilidad</th><th>Tipo</th><th>Nivel</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var c in _competencias)
        {
            sb.AppendLine("<tr>" +
                $"<td>{E(c.Descripcion)}</td>" +
                $"<td>{E(c.TipoCompetencia?.Descripcion ?? "-")}</td>" +
                $"<td>{E(c.Medicion)}</td></tr>");
        }
        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine("</section>");
    }

    private void Pie(StringBuilder sb)
    {
        sb.AppendLine("<footer class=\"hv-pie\">");
        sb.AppendLine($"<span>{E(PlantillaComun.FechaGeneracion())}</span>");
        sb.AppendLine($"<span>{E(_basica.NombreCompleto)}</span>");
        sb.AppendLine("</footer>");
    }

    private static void AbrirSeccion(StringBuilder sb, string titulo)
    {
        sb.AppendLine("<section class=\"hv-seccion\">");
        sb.AppendLine($"<h2 class=\"hv-seccion-titulo\">{E(titulo)}</h2>");
    }

    // ---------- Apoyo ----------

    private string NombreOTitulo() =>
        string.IsNullOrWhiteSpace(_basica.NombreCompleto) ? "Hoja de vida" : _basica.NombreCompleto;

    private IEnumerable<(string Etiqueta, string Valor)> Contacto()
    {
        var ubicacion = PlantillaComun.Unir(", ", _basica.Ciudad?.Nombre, _basica.Departamento?.Nombre);

        var datos = new (string Etiqueta, string? Valor)[]
        {
            ("Documento", _basica.Documento == 0 ? null : _basica.Documento.ToString("N0", Cultura)),
            ("Correo", _basica.Email),
            ("Celular", _basica.TelefonoMovil),
            ("Fijo", _basica.TelefonoFijo),
            ("Ubicación", ubicacion)
        };

        return datos
            .Where(d => !string.IsNullOrWhiteSpace(d.Valor))
            .Select(d => (d.Etiqueta, d.Valor!));
    }

    /// <summary>Todo valor que venga de la base se escapa antes de entrar al documento.</summary>
    private static string E(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

    /// <summary>Escapa y conserva los saltos de linea que el usuario escribio en el perfil.</summary>
    private static string EnParrafo(string valor) =>
        E(valor).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br />");

    /// <summary>Empotra la foto como data URI para que el archivo no dependa del servidor.</summary>
    private static string? ComoDataUri(byte[]? foto)
    {
        if (foto is null || foto.Length < 4) return null;

        var mime = foto switch
        {
            [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
            [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
            [0x52, 0x49, 0x46, 0x46, ..] => "image/webp",
            _ => null
        };

        return mime is null ? null : $"data:{mime};base64,{Convert.ToBase64String(foto)}";
    }
}
