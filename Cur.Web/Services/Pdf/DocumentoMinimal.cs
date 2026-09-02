using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cur.Web.Services.Pdf;

/// <summary>
/// Plantilla Minimal: una columna, sin fondos de color. Toda la jerarquía la dan
/// la tipografía y unas pocas líneas finas. Es la más segura para filtros ATS.
/// </summary>
public class DocumentoMinimal : IDocument
{
    private const string Texto = "#1A1A1A";
    private const string Suave = "#707070";
    private const string Linea = "#D4D4D4";

    private readonly CurriculumViewModel _cv;

    public DocumentoMinimal(CurriculumViewModel cv) => _cv = cv;

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Hoja de vida - {_cv.Basica?.NombreCompleto}",
        Author = _cv.Basica?.NombreCompleto ?? "Curriculum",
        Subject = "Hoja de vida"
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2.2f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10).FontColor(Texto).LineHeight(1.45f));

            page.Header().Element(Encabezado);
            page.Content().PaddingTop(24).Element(Contenido);

            page.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Suave));
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    private void Encabezado(IContainer container)
    {
        var b = _cv.Basica;

        container.Column(col =>
        {
            col.Item().AlignCenter().Text((b?.NombreCompleto ?? "Hoja de vida").ToUpper(PlantillaComun.Cultura))
                .FontSize(19).Light();

            if (!string.IsNullOrWhiteSpace(b?.Profesion?.Descripcion))
                col.Item().PaddingTop(5).AlignCenter().Text(b!.Profesion!.Descripcion)
                    .FontSize(9.5f).FontColor(Suave);

            col.Item().PaddingTop(10).AlignCenter().Text(PlantillaComun.Unir("   ·   ",
                b?.Email,
                b?.TelefonoMovil,
                b?.TelefonoFijo,
                PlantillaComun.Unir(", ", b?.Ciudad?.Nombre, b?.Departamento?.Nombre),
                b is null ? null : $"C.C. {b.Documento.ToString("N0", PlantillaComun.Cultura)}"))
                .FontSize(8.5f).FontColor(Suave);

            col.Item().PaddingTop(14).LineHorizontal(0.6f).LineColor(Linea);
        });
    }

    private void Contenido(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(22);

            if (!string.IsNullOrWhiteSpace(_cv.Basica?.PerfilProfesional))
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Perfil");
                    s.Item().PaddingTop(8).Text(_cv.Basica!.PerfilProfesional).Justify();
                });
            }

            if (_cv.Experiencia.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Experiencia");
                    foreach (var exp in _cv.Experiencia)
                        s.Item().PaddingTop(14).Element(c => Experiencia(c, exp));
                });
            }

            if (_cv.Formacion.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Formación");
                    foreach (var f in _cv.Formacion)
                        s.Item().PaddingTop(11).Element(c => Formacion(c, f));
                });
            }

            var competencias = _cv.Experiencia.SelectMany(e => e.Competencias).ToList();
            if (competencias.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Competencias");
                    s.Item().PaddingTop(9).Column(lista =>
                    {
                        lista.Spacing(3);
                        foreach (var c in competencias)
                        {
                            lista.Item().Text(t =>
                            {
                                t.Span(c.Descripcion).SemiBold().FontSize(9.5f);
                                var detalle = PlantillaComun.Unir(" · ", c.TipoCompetencia?.Descripcion, c.Medicion);
                                if (!string.IsNullOrWhiteSpace(detalle))
                                    t.Span($"  {detalle}").FontSize(9).FontColor(Suave);
                            });
                        }
                    });
                });
            }
        });
    }

    private static void TituloSeccion(ColumnDescriptor col, string titulo)
    {
        col.Item().Text(titulo.ToUpper(PlantillaComun.Cultura))
            .FontSize(9).SemiBold().LetterSpacing(0.06f).FontColor(Suave);
        col.Item().PaddingTop(4).LineHorizontal(0.6f).LineColor(Linea);
    }

    private static void Experiencia(IContainer container, InformacionLaboral exp)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(PlantillaComun.Unir(", ", exp.Cargo?.Descripcion, exp.Empresa))
                    .FontSize(10.5f).SemiBold();

                row.ConstantItem(150).AlignRight().Text(PlantillaComun.Periodo(exp.FechaInicio, exp.FechaRetiro))
                    .FontSize(9).FontColor(Suave);
            });

            var detalle = PlantillaComun.Unir(" · ",
                exp.Area?.Descripcion,
                PlantillaComun.Duracion(exp.TiempoLaborado ?? exp.CalcularMesesLaborados()));

            if (!string.IsNullOrWhiteSpace(detalle))
                col.Item().PaddingTop(1).Text(detalle).FontSize(9).FontColor(Suave);

            var logros = exp.Logros.OrderBy(l => l.Tipo?.Descripcion).ToList();
            if (logros.Count == 0) return;

            col.Item().PaddingTop(6).Column(c =>
            {
                c.Spacing(2);
                foreach (var logro in logros)
                {
                    c.Item().Row(r =>
                    {
                        r.ConstantItem(14).Text("—").FontSize(9).FontColor(Suave);
                        r.RelativeItem().Text(PlantillaComun.Unir(" — ",
                            PlantillaComun.LimpiarVinieta(logro.Logro),
                            PlantillaComun.LimpiarVinieta(logro.Descripcion)))
                            .FontSize(9.5f);
                    });
                }
            });
        });
    }

    private static void Formacion(IContainer container, FormacionAcademica f)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(f.TituloOtorgado).FontSize(10).SemiBold();
                col.Item().Text(PlantillaComun.Unir(" · ",
                    f.Institucion,
                    f.TipoFormacion?.Descripcion,
                    f.Estado?.Descripcion,
                    string.IsNullOrWhiteSpace(f.Intensidad) ? null : $"{f.Intensidad} h"))
                    .FontSize(9).FontColor(Suave);
            });

            row.ConstantItem(150).AlignRight().Text(PlantillaComun.Periodo(f.FechaInicio, f.FechaFinalizacion))
                .FontSize(9).FontColor(Suave);
        });
    }
}
