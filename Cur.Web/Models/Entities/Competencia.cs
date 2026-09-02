namespace Cur.Web.Models.Entities;

/// <summary>
/// Competencias y habilidades asociadas a un cargo. Tabla Competencias.
/// Ojo: CmptnciaID no es IDENTITY en la base, el id se asigna desde el repositorio.
/// </summary>
public class Competencia
{
    public int CompetenciaId { get; set; }
    public int LaboralId { get; set; }
    public int TipoCompetenciaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Medicion { get; set; } = string.Empty;

    public InformacionLaboral? Laboral { get; set; }
    public Parametro? TipoCompetencia { get; set; }
}
