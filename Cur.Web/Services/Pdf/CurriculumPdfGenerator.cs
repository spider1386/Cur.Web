using System.Text;
using Cur.Web.Models;
using Cur.Web.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cur.Web.Services.Pdf;

public interface ICurriculumPdfGenerator
{
    byte[] Generar(CurriculumViewModel cv, byte[]? foto, PlantillaCv plantilla = Plantillas.PorDefecto);
    string NombreArchivo(CurriculumViewModel cv);
}

public class CurriculumPdfGenerator : ICurriculumPdfGenerator
{
    public byte[] Generar(CurriculumViewModel cv, byte[]? foto, PlantillaCv plantilla = Plantillas.PorDefecto) =>
        Documento(cv, foto, plantilla).GeneratePdf();

    private static IDocument Documento(CurriculumViewModel cv, byte[]? foto, PlantillaCv plantilla) => plantilla switch
    {
        PlantillaCv.Ejecutiva => new DocumentoEjecutivo(cv, foto),
        // La minimal es deliberadamente sin foto: apuesta por texto plano y filtros ATS.
        PlantillaCv.Minimal => new DocumentoMinimal(cv),
        PlantillaCv.Timeline => new DocumentoTimeline(cv, foto),
        _ => new CurriculumDocument(cv, foto)
    };

    public string NombreArchivo(CurriculumViewModel cv)
    {
        var nombre = cv.Basica?.NombreCompleto ?? "hoja-de-vida";
        var limpio = new StringBuilder();

        foreach (var c in nombre.Normalize(NormalizationForm.FormD))
        {
            var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == System.Globalization.UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(c)) limpio.Append(char.ToLowerInvariant(c));
            else if (char.IsWhiteSpace(c) && limpio.Length > 0 && limpio[^1] != '-') limpio.Append('-');
        }

        var slug = limpio.ToString().Trim('-');
        if (slug.Length == 0) slug = "hoja-de-vida";

        return $"HV-{slug}-{DateTime.Now:yyyyMMdd}.pdf";
    }
}
