using System.Globalization;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cur.Web.Services.Pdf;

/// <summary>Plantilla del PDF de la hoja de vida.</summary>
public class CurriculumDocument : IDocument
{
    private static readonly CultureInfo Cultura = PlantillaComun.Cultura;

    private const string Primario = "#1D4ED8";
    private const string Texto = "#111827";
    private const string Suave = "#6B7280";
    private const string Linea = "#E5E7EB";

    private readonly CurriculumViewModel _cv;
    private readonly byte[]? _foto;

    public CurriculumDocument(CurriculumViewModel cv, byte[]? foto)
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
            page.Margin(1.6f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10).FontColor(Texto).LineHeight(1.3f));

            page.Header().Element(Encabezado);
            page.Content().PaddingTop(18).Element(Contenido);
            page.Footer().Element(PieDePagina);
        });
    }

    private void Encabezado(IContainer container)
    {
        var b = _cv.Basica;

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (_foto is not null)
                {
                    row.ConstantItem(78).Height(78)
                        .Border(2).BorderColor(Primario)
                        .Image(_foto).FitArea();
                    row.ConstantItem(16);
                }

                row.RelativeItem().Column(datos =>
                {
                    datos.Item().Text(b?.NombreCompleto ?? "Hoja de vida")
                        .FontSize(22).SemiBold().FontColor(Primario);

                    if (!string.IsNullOrWhiteSpace(b?.Profesion?.Descripcion))
                        datos.Item().PaddingTop(2).Text(b!.Profesion!.Descripcion)
                            .FontSize(12).FontColor(Suave);

                    datos.Item().PaddingTop(8).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(9).FontColor(Suave));
                        var ubicacion = string.Join(", ",
                            new[] { b?.Ciudad?.Nombre, b?.Departamento?.Nombre }
                                .Where(x => !string.IsNullOrWhiteSpace(x)));

                        AgregarDato(t, "Documento", b?.Documento.ToString("N0", Cultura));
                        AgregarDato(t, "Correo", b?.Email);
                        AgregarDato(t, "Celular", b?.TelefonoMovil);
                        AgregarDato(t, "Fijo", b?.TelefonoFijo);
                        AgregarDato(t, "Ubicación", ubicacion, ultimo: true);
                    });
                });
            });

            col.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor(Primario);
        });
    }

    private static void AgregarDato(TextDescriptor t, string etiqueta, string? valor, bool ultimo = false)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        t.Span($"{etiqueta}: ").SemiBold();
        t.Span(valor);
        if (!ultimo) t.Span("   •   ");
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
                    s.Item().PaddingTop(6).Text(_cv.Basica!.PerfilProfesional).Justify();
                });
            }

            if (_cv.Experiencia.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Experiencia laboral");
                    foreach (var exp in _cv.Experiencia)
                        s.Item().PaddingTop(10).Element(c => Experiencia(c, exp));
                });
            }

            if (_cv.Formacion.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Formación académica");
                    foreach (var f in _cv.Formacion)
                        s.Item().PaddingTop(10).Element(c => Formacion(c, f));
                });
            }

            var competencias = _cv.Experiencia.SelectMany(e => e.Competencias).ToList();
            if (competencias.Count > 0)
            {
                col.Item().Column(s =>
                {
                    TituloSeccion(s, "Competencias y habilidades");
                    s.Item().PaddingTop(8).Element(c => Competencias(c, competencias));
                });
            }
        });
    }

    private static void TituloSeccion(ColumnDescriptor col, string titulo)
    {
        col.Item().Text(titulo.ToUpper(Cultura))
            .FontSize(11).Bold().FontColor(Primario).LetterSpacing(0.08f);
        col.Item().PaddingTop(3).LineHorizontal(0.8f).LineColor(Linea);
    }

    private void Experiencia(IContainer container, InformacionLaboral exp)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(exp.Cargo?.Descripcion ?? "Cargo").FontSize(11).SemiBold();
                    c.Item().Text(exp.Empresa).FontSize(10).FontColor(Suave);
                });

                row.ConstantItem(170).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(PlantillaComun.Periodo(exp.FechaInicio, exp.FechaRetiro))
                        .FontSize(9).FontColor(Suave);
                    c.Item().AlignRight().Text(PlantillaComun.Duracion(exp.TiempoLaborado ?? exp.CalcularMesesLaborados()))
                        .FontSize(9).FontColor(Suave);
                });
            });

            if (!string.IsNullOrWhiteSpace(exp.Area?.Descripcion))
                col.Item().PaddingTop(2).Text($"Área: {exp.Area!.Descripcion}").FontSize(9).FontColor(Suave);

            var logros = exp.Logros.OrderBy(l => l.Tipo?.Descripcion).ToList();
            if (logros.Count > 0)
            {
                col.Item().PaddingTop(5).Column(c =>
                {
                    foreach (var logro in logros)
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(12).PaddingTop(1).Text("•").FontColor(Primario);
                            r.RelativeItem().Text(t =>
                            {
                                t.Span(PlantillaComun.LimpiarVinieta(logro.Logro)).SemiBold().FontSize(9.5f);
                                if (!string.IsNullOrWhiteSpace(logro.Descripcion))
                                    t.Span($" — {PlantillaComun.LimpiarVinieta(logro.Descripcion)}").FontSize(9.5f);
                            });
                        });
                    }
                });
            }
        });
    }

    private void Formacion(IContainer container, FormacionAcademica f)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(f.TituloOtorgado).FontSize(11).SemiBold();
                    c.Item().Text(f.Institucion).FontSize(10).FontColor(Suave);
                });

                row.ConstantItem(170).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(PlantillaComun.Periodo(f.FechaInicio, f.FechaFinalizacion))
                        .FontSize(9).FontColor(Suave);

                    var detalle = string.Join(" • ", new[]
                    {
                        f.TipoFormacion?.Descripcion,
                        f.Estado?.Descripcion,
                        string.IsNullOrWhiteSpace(f.Intensidad) ? null : $"{f.Intensidad} h"
                    }.Where(x => !string.IsNullOrWhiteSpace(x)));

                    if (!string.IsNullOrWhiteSpace(detalle))
                        c.Item().AlignRight().Text(detalle).FontSize(9).FontColor(Suave);
                });
            });
        });
    }

    private void Competencias(IContainer container, IReadOnlyList<Competencia> competencias)
    {
        container.Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3);
                c.RelativeColumn(1.4f);
                c.RelativeColumn(1.4f);
            });

            tabla.Header(h =>
            {
                h.Cell().Element(CeldaEncabezado).Text("Competencia / habilidad");
                h.Cell().Element(CeldaEncabezado).Text("Tipo");
                h.Cell().Element(CeldaEncabezado).Text("Nivel");
            });

            foreach (var c in competencias)
            {
                tabla.Cell().Element(Celda).Text(c.Descripcion);
                tabla.Cell().Element(Celda).Text(c.TipoCompetencia?.Descripcion ?? "-");
                tabla.Cell().Element(Celda).Text(c.Medicion);
            }
        });

        static IContainer CeldaEncabezado(IContainer c) => c
            .Background(Colors.Grey.Lighten4)
            .BorderBottom(1).BorderColor(Linea)
            .PaddingVertical(5).PaddingHorizontal(4)
            .DefaultTextStyle(t => t.SemiBold().FontSize(9));

        static IContainer Celda(IContainer c) => c
            .BorderBottom(1).BorderColor(Linea)
            .PaddingVertical(4).PaddingHorizontal(4)
            .DefaultTextStyle(t => t.FontSize(9));
    }

    private void PieDePagina(IContainer container)
    {
        container.PaddingTop(8).BorderTop(0.8f).BorderColor(Linea).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(PlantillaComun.FechaGeneracion())
                .FontSize(8).FontColor(Suave);

            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Suave));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    }

}
