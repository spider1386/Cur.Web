using Cur.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cur.Web.Services.Pdf;

/// <summary>
/// Antepone la carta de presentación como primera página del PDF y delega el resto en la
/// plantilla que el usuario haya elegido. Se resuelve una sola vez y no dentro de cada
/// plantilla porque QuestPDF permite encadenar varias secuencias de página sobre el
/// mismo contenedor: asi la portada funciona igual con las cuatro.
/// </summary>
public class DocumentoConCarta : IDocument
{
    private const string Texto = "#111827";
    private const string Suave = "#6B7280";
    private const string Linea = "#E5E7EB";

    private readonly IDocument _hojaDeVida;
    private readonly CurriculumViewModel _cv;
    private readonly string _carta;
    private readonly string _acento;

    public DocumentoConCarta(IDocument hojaDeVida, CurriculumViewModel cv, string carta, string acento)
    {
        _hojaDeVida = hojaDeVida;
        _cv = cv;
        _carta = carta;
        _acento = acento;
    }

    public DocumentMetadata GetMetadata() => _hojaDeVida.GetMetadata();

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10.5f).FontColor(Texto).LineHeight(1.45f));

            page.Header().Element(Encabezado);
            page.Content().PaddingTop(20).Element(Cuerpo);
            page.Footer().Element(Pie);
        });

        _hojaDeVida.Compose(container);
    }

    private void Encabezado(IContainer container)
    {
        var basica = _cv.Basica;

        container.Column(col =>
        {
            col.Item().Text("Carta de presentación")
                .FontSize(10).Bold().FontColor(_acento).LetterSpacing(0.08f);

            if (basica is not null)
            {
                col.Item().PaddingTop(6).Text(basica.NombreCompleto).FontSize(18).SemiBold();

                var subtitulo = PlantillaComun.Unir(" · ",
                    basica.Profesion?.Descripcion,
                    PlantillaComun.Unir(", ", basica.Ciudad?.Nombre, basica.Departamento?.Nombre));

                if (!string.IsNullOrWhiteSpace(subtitulo))
                    col.Item().PaddingTop(2).Text(subtitulo).FontSize(9.5f).FontColor(Suave);
            }

            col.Item().PaddingTop(12).LineHorizontal(1).LineColor(_acento);
        });
    }

    private void Cuerpo(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(11);

            foreach (var bloque in Bloques())
            {
                // Un bloque puede ser un párrafo o una lista; cada renglón va como línea propia
                // para que las viñetas conserven su sangría.
                col.Item().Column(lineas =>
                {
                    lineas.Spacing(3);

                    foreach (var linea in bloque)
                    {
                        var esVinieta = linea.StartsWith("• ") || (linea.Length > 2 && char.IsDigit(linea[0]) && linea[1] == '.');

                        lineas.Item()
                            .PaddingLeft(esVinieta ? 10 : 0)
                            .Text(linea)
                            .Justify();
                    }
                });
            }
        });
    }

    /// <summary>Parte el texto en bloques (separados por línea en blanco) y estos en renglones.</summary>
    private IEnumerable<string[]> Bloques() =>
        _carta.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(b => b.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Where(b => b.Length > 0);

    private void Pie(IContainer container)
    {
        container.PaddingTop(8).BorderTop(0.8f).BorderColor(Linea).PaddingTop(6)
            .Text(PlantillaComun.FechaGeneracion())
            .FontSize(8).FontColor(Suave);
    }
}
