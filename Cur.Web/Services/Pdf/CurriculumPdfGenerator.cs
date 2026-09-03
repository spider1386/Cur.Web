using Cur.Web.Models;
using Cur.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cur.Web.Services.Pdf;

public interface ICurriculumPdfGenerator
{
    /// <summary>
    /// Genera el PDF. Si <paramref name="carta"/> trae texto, se antepone como portada.
    /// </summary>
    byte[] Generar(CurriculumViewModel cv, byte[]? foto,
        PlantillaCv plantilla = Plantillas.PorDefecto, string? carta = null);

    string NombreArchivo(CurriculumViewModel cv);
}

public class CurriculumPdfGenerator : ICurriculumPdfGenerator
{
    public byte[] Generar(CurriculumViewModel cv, byte[]? foto,
        PlantillaCv plantilla = Plantillas.PorDefecto, string? carta = null) =>
        Documento(cv, foto, plantilla, carta).GeneratePdf();

    private static IDocument Documento(CurriculumViewModel cv, byte[]? foto, PlantillaCv plantilla, string? carta)
    {
        IDocument hojaDeVida = plantilla switch
        {
            PlantillaCv.Ejecutiva => new DocumentoEjecutivo(cv, foto),
            // La minimal es deliberadamente sin foto: apuesta por texto plano y filtros ATS.
            PlantillaCv.Minimal => new DocumentoMinimal(cv),
            PlantillaCv.Timeline => new DocumentoTimeline(cv, foto),
            _ => new CurriculumDocument(cv, foto)
        };

        return string.IsNullOrWhiteSpace(carta)
            ? hojaDeVida
            : new DocumentoConCarta(hojaDeVida, cv, carta, Acento(plantilla));
    }

    /// <summary>Color con el que la portada se alinea a la plantilla elegida.</summary>
    private static string Acento(PlantillaCv plantilla) => plantilla switch
    {
        PlantillaCv.Minimal => "#1A1A1A",
        PlantillaCv.Timeline => "#0F766E",
        _ => "#1D4ED8"
    };

    public string NombreArchivo(CurriculumViewModel cv) =>
        PlantillaComun.NombreArchivo(cv.Basica?.NombreCompleto, "pdf");
}
