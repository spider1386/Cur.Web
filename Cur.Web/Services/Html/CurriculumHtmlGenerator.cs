using Cur.Web.Models;
using Cur.Web.Models.ViewModels;
using Cur.Web.Services.Pdf;

namespace Cur.Web.Services.Html;

public interface ICurriculumHtmlGenerator
{
    /// <summary>
    /// Genera la hoja de vida como un HTML autocontenido (estilos y foto embebidos).
    /// Si <paramref name="carta"/> trae texto, se antepone como portada.
    /// </summary>
    string Generar(CurriculumViewModel cv, byte[]? foto,
        PlantillaCv plantilla = Plantillas.PorDefecto, string? carta = null);

    string NombreArchivo(CurriculumViewModel cv);
}

public class CurriculumHtmlGenerator : ICurriculumHtmlGenerator
{
    public string Generar(CurriculumViewModel cv, byte[]? foto,
        PlantillaCv plantilla = Plantillas.PorDefecto, string? carta = null) =>
        new CurriculumHtmlDocument(cv, foto, TemaHtml.Para(plantilla), carta).Render();

    public string NombreArchivo(CurriculumViewModel cv) =>
        PlantillaComun.NombreArchivo(cv.Basica?.NombreCompleto, "html");
}
