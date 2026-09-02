using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cur.Web.Services.Pdf;

/// <summary>
/// Plantilla Timeline: experiencia y formación colgadas de una línea de tiempo
/// vertical, con el periodo a la izquierda y un marcador por entrada.
/// </summary>
public class DocumentoTimeline : IDocument
{
    private const float AnchoPeriodo = 96f;
    private const float AnchoMarcador = 14f;

    private const string Primario = "#0F766E";
    private const string Acento = "#14B8A6";
    private const string Texto = "#111827";
    private const string Suave = "#6B7280";
    private const string Linea = "#E5E7EB";

    private readonly CurriculumViewModel _cv;
    private readonly byte[]? _foto;

    public DocumentoTimeline(CurriculumViewModel cv, byte[]? foto)
    {
        _cv = cv;
        _foto = foto;
    }

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
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(Texto).LineHeight(1.35f));

            page.Header().Element(Encabezado);
            page.Content().PaddingTop(18).Element(Contenido);

            page.Footer().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text(PlantillaComun.FechaGeneracion()).FontSize(7.5f).FontColor(Suave);
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Suave));
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });
    }

    private void Encabezado(IContainer container)
    {
        var b = _cv.Basica;

        container.Background(Primario).Padding(18).Row(row =>
        {
            if (_foto is not null)
            {
                row.ConstantItem(70).Height(70).Border(2).BorderColor(Acento).Image(_foto).FitArea();
                row.ConstantItem(16);
            }

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(b?.NombreCompleto ?? "Hoja de vida")
                    .FontSize(19).SemiBold().FontColor("#FFFFFF");

                if (!string.IsNullOrWhiteSpace(b?.Profesion?.Descripcion))
                    col.Item().PaddingTop(2).Text(b!.Profesion!.Descripcion)
                        .FontSize(10).FontColor("#A7F3D0");

                col.Item().PaddingTop(8).Text(PlantillaComun.Unir("   ·   ",
                    b?.Email,
                    b?.TelefonoMovil,
                    b?.TelefonoFijo,
                    PlantillaComun.Unir(", ", b?.Ciudad?.Nombre, b?.Departamento?.Nombre)))
                    .FontSize(8.5f).FontColor("#CCFBF1");
            });
        });
    }

    private void Contenido(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(18);

            if (!string.IsNullOrWhiteSpace(_cv.Basica?.PerfilProfesional))
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Perfil profesional");
                    s.Item().PaddingTop(7).Text(_cv.Basica!.PerfilProfesional).Justify();
                });
            }

            if (_cv.Experiencia.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Trayectoria laboral");
                    s.Item().PaddingTop(10).Column(linea =>
                    {
                        foreach (var exp in _cv.Experiencia)
                            linea.Item().Element(c => EntradaExperiencia(c, exp));
                    });
                });
            }

            if (_cv.Formacion.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Formación académica");
                    s.Item().PaddingTop(10).Column(linea =>
                    {
                        foreach (var f in _cv.Formacion)
                            linea.Item().Element(c => EntradaFormacion(c, f));
                    });
                });
            }

            var competencias = _cv.Experiencia.SelectMany(e => e.Competencias).ToList();
            if (competencias.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Competencias y habilidades");
                    s.Item().PaddingTop(9).Column(lista =>
                    {
                        lista.Spacing(4);
                        foreach (var c in competencias)
                        {
                            lista.Item().Row(r =>
                            {
                                r.ConstantItem(AnchoPeriodo).Text(c.TipoCompetencia?.Descripcion ?? "")
                                    .FontSize(8.5f).FontColor(Suave);
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span(c.Descripcion).SemiBold().FontSize(9.5f);
                                    if (!string.IsNullOrWhiteSpace(c.Medicion))
                                        t.Span($" — {c.Medicion}").FontSize(9).FontColor(Suave);
                                });
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
            .FontSize(10).Bold().FontColor(Primario).LetterSpacing(0.06f);
        col.Item().PaddingTop(3).LineHorizontal(1).LineColor(Linea);
    }

    /// <summary>
    /// Fila con el periodo a la izquierda y el detalle colgando de la línea de tiempo.
    /// La línea es el borde izquierdo de la columna de detalle: se estira sola con el
    /// contenido. No usar ExtendVertical aquí, porque hace que cada entrada ocupe una
    /// página completa.
    /// </summary>
    private static void Entrada(IContainer container, string periodo, string? subPeriodo, Action<ColumnDescriptor> detalle)
    {
        container.Row(row =>
        {
            row.ConstantItem(AnchoPeriodo).PaddingTop(2).PaddingRight(10).AlignRight().Column(c =>
            {
                c.Item().AlignRight().Text(periodo).FontSize(8.5f).SemiBold().FontColor(Primario);
                if (!string.IsNullOrWhiteSpace(subPeriodo))
                    c.Item().AlignRight().Text(subPeriodo).FontSize(8).FontColor(Suave);
            });

            row.RelativeItem()
                .BorderLeft(1).BorderColor(Linea)
                .PaddingLeft(AnchoMarcador).PaddingBottom(14)
                .Column(col =>
                {
                    col.Item().Row(marcador =>
                    {
                        marcador.ConstantItem(12).PaddingTop(4).Height(7).Width(7).Background(Acento);
                        marcador.RelativeItem().Column(detalle);
                    });
                });
        });
    }

    private static void EntradaExperiencia(IContainer container, InformacionLaboral exp)
    {
        Entrada(container,
            PlantillaComun.Periodo(exp.FechaInicio, exp.FechaRetiro),
            PlantillaComun.Duracion(exp.TiempoLaborado ?? exp.CalcularMesesLaborados()),
            col =>
            {
                col.Item().Text(exp.Cargo?.Descripcion ?? "Cargo").FontSize(10.5f).SemiBold();
                col.Item().Text(PlantillaComun.Unir(" · ", exp.Empresa, exp.Area?.Descripcion))
                    .FontSize(8.5f).FontColor(Suave);

                var logros = exp.Logros.OrderBy(l => l.Tipo?.Descripcion).ToList();
                if (logros.Count == 0) return;

                col.Item().PaddingTop(5).Column(c =>
                {
                    c.Spacing(2);
                    foreach (var logro in logros)
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(11).PaddingTop(3).Height(3).Width(3).Background(Acento);
                            r.RelativeItem().Text(t =>
                            {
                                t.Span(PlantillaComun.LimpiarVinieta(logro.Logro)).SemiBold().FontSize(9);
                                if (!string.IsNullOrWhiteSpace(logro.Descripcion))
                                    t.Span($" — {PlantillaComun.LimpiarVinieta(logro.Descripcion)}").FontSize(9);
                            });
                        });
                    }
                });
            });
    }

    private static void EntradaFormacion(IContainer container, FormacionAcademica f)
    {
        Entrada(container,
            PlantillaComun.Periodo(f.FechaInicio, f.FechaFinalizacion),
            f.Estado?.Descripcion,
            col =>
            {
                col.Item().Text(f.TituloOtorgado).FontSize(10).SemiBold();
                col.Item().Text(PlantillaComun.Unir(" · ",
                    f.Institucion,
                    f.TipoFormacion?.Descripcion,
                    string.IsNullOrWhiteSpace(f.Intensidad) ? null : $"{f.Intensidad} h"))
                    .FontSize(8.5f).FontColor(Suave);
            });
    }
}
