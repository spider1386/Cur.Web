using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cur.Web.Services.Pdf;

/// <summary>
/// Plantilla Ejecutiva: barra lateral oscura con foto, contacto y competencias,
/// y columna principal con perfil, experiencia y formación.
/// </summary>
public class DocumentoEjecutivo : IDocument
{
    private const float AnchoLateral = 178f;

    private const string Lateral = "#1E293B";
    private const string LateralTexto = "#E2E8F0";
    private const string LateralSuave = "#94A3B8";
    private const string Acento = "#38BDF8";
    private const string Texto = "#111827";
    private const string Suave = "#6B7280";
    private const string Linea = "#E5E7EB";
    private const string Primario = "#1D4ED8";

    private readonly CurriculumViewModel _cv;
    private readonly byte[]? _foto;

    public DocumentoEjecutivo(CurriculumViewModel cv, byte[]? foto)
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
            page.Margin(0);
            page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(Texto).LineHeight(1.35f));

            // El nombre va en una franja a todo el ancho, no en la barra lateral: ahi se
            // partia en dos lineas y un ATS que toma "la primera linea" como nombre solo
            // capturaba el nombre de pila.
            page.Content().Column(col =>
            {
                col.Item().Element(BandaSuperior);

                col.Item().Row(row =>
                {
                    row.ConstantItem(AnchoLateral).ExtendVertical().Background(Lateral).Element(BarraLateral);
                    row.RelativeItem().Element(Principal);
                });
            });

            page.Footer().PaddingHorizontal(28).PaddingBottom(14).Row(row =>
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

    // ---------- Franja superior ----------

    /// <summary>Nombre y profesión a todo el ancho: es lo primero que lee un ATS.</summary>
    private void BandaSuperior(IContainer container)
    {
        var b = _cv.Basica;

        container.Background(Lateral).PaddingVertical(22).PaddingHorizontal(28).Column(col =>
        {
            col.Item().Text(b?.NombreCompleto ?? "Hoja de vida")
                .FontSize(22).SemiBold().FontColor("#FFFFFF");

            if (!string.IsNullOrWhiteSpace(b?.Profesion?.Descripcion))
                col.Item().PaddingTop(3).Text(b!.Profesion!.Descripcion)
                    .FontSize(11).FontColor(Acento);
        });
    }

    // ---------- Barra lateral ----------

    private void BarraLateral(IContainer container)
    {
        var b = _cv.Basica;

        container.PaddingVertical(24).PaddingHorizontal(20).Column(col =>
        {
            if (_foto is not null)
            {
                col.Item().AlignCenter()
                    .Width(96).Height(96)
                    .Border(2).BorderColor(Acento)
                    .Image(_foto).FitArea();
                col.Item().Height(22);
            }

            col.Item().Element(c => TituloLateral(c, "Contacto"));

            col.Item().PaddingTop(8).Column(datos =>
            {
                datos.Spacing(6);
                DatoLateral(datos, "Documento", b?.Documento.ToString("N0", PlantillaComun.Cultura));
                DatoLateral(datos, "Correo", b?.Email);
                DatoLateral(datos, "Celular", b?.TelefonoMovil);
                DatoLateral(datos, "Fijo", b?.TelefonoFijo);
                DatoLateral(datos, "Ubicación",
                    PlantillaComun.Unir(", ", b?.Ciudad?.Nombre, b?.Departamento?.Nombre));
            });

            var competencias = _cv.Experiencia.SelectMany(e => e.Competencias).ToList();
            if (competencias.Count > 0)
            {
                col.Item().PaddingTop(24).Element(c => TituloLateral(c, "Competencias"));

                col.Item().PaddingTop(8).Column(lista =>
                {
                    lista.Spacing(7);
                    foreach (var c in competencias)
                    {
                        lista.Item().Column(item =>
                        {
                            item.Item().Text(c.Descripcion).FontSize(8.5f).FontColor(LateralTexto);
                            item.Item().Text(PlantillaComun.Unir(" · ", c.TipoCompetencia?.Descripcion, c.Medicion))
                                .FontSize(7.5f).FontColor(LateralSuave);
                        });
                    }
                });
            }
        });
    }

    private static void TituloLateral(IContainer container, string titulo)
    {
        container.Column(col =>
        {
            col.Item().Text(titulo.ToUpper(PlantillaComun.Cultura))
                .FontSize(8.5f).Bold().FontColor(Acento).LetterSpacing(0.06f);
            col.Item().PaddingTop(4).LineHorizontal(0.8f).LineColor("#334155");
        });
    }

    private static void DatoLateral(ColumnDescriptor col, string etiqueta, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;

        col.Item().Column(item =>
        {
            item.Item().Text(etiqueta).FontSize(7.5f).FontColor(LateralSuave);
            item.Item().Text(valor).FontSize(8.5f).FontColor(LateralTexto);
        });
    }

    // ---------- Columna principal ----------

    private void Principal(IContainer container)
    {
        container.PaddingVertical(24).PaddingHorizontal(28).Column(col =>
        {
            col.Spacing(20);

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
                    TituloSeccion(s, "Experiencia laboral");
                    foreach (var exp in _cv.Experiencia)
                        s.Item().PaddingTop(12).Element(c => Experiencia(c, exp));
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
        });
    }

    private static void TituloSeccion(ColumnDescriptor col, string titulo)
    {
        col.Item().Text(titulo.ToUpper(PlantillaComun.Cultura))
            .FontSize(10).Bold().FontColor(Primario).LetterSpacing(0.06f);
        col.Item().PaddingTop(3).LineHorizontal(1).LineColor(Linea);
    }

    private static void Experiencia(IContainer container, InformacionLaboral exp)
    {
        container.Column(col =>
        {
            col.Item().Text(exp.Cargo?.Descripcion ?? "Cargo").FontSize(10.5f).SemiBold();

            col.Item().PaddingTop(1).Text(PlantillaComun.Unir(" · ",
                exp.Empresa,
                PlantillaComun.Periodo(exp.FechaInicio, exp.FechaRetiro),
                PlantillaComun.Duracion(exp.TiempoLaborado ?? exp.CalcularMesesLaborados())))
                .FontSize(8.5f).FontColor(Suave);

            if (!string.IsNullOrWhiteSpace(exp.Area?.Descripcion))
                col.Item().Text($"Área: {exp.Area!.Descripcion}").FontSize(8.5f).FontColor(Suave);

            var logros = exp.Logros.OrderBy(l => l.Tipo?.Descripcion).ToList();
            if (logros.Count == 0) return;

            col.Item().PaddingTop(6).Column(c =>
            {
                c.Spacing(2);
                foreach (var logro in logros)
                {
                    c.Item().Row(r =>
                    {
                        r.ConstantItem(11).PaddingTop(3).Height(3).Width(3).Background(Primario);
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

    private static void Formacion(IContainer container, FormacionAcademica f)
    {
        container.Column(col =>
        {
            col.Item().Text(f.TituloOtorgado).FontSize(10).SemiBold();
            col.Item().PaddingTop(1).Text(PlantillaComun.Unir(" · ",
                f.Institucion,
                PlantillaComun.Periodo(f.FechaInicio, f.FechaFinalizacion),
                f.TipoFormacion?.Descripcion,
                f.Estado?.Descripcion,
                string.IsNullOrWhiteSpace(f.Intensidad) ? null : $"{f.Intensidad} h"))
                .FontSize(8.5f).FontColor(Suave);
        });
    }
}
