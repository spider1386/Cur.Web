namespace Cur.Web.Models.Entities;

/// <summary>Formacion academica. Tabla Formacion_Academica.</summary>
public class FormacionAcademica
{
    public int FormacionId { get; set; }
    public int BasicaId { get; set; }
    public int? TipoFormacionId { get; set; }
    public int? AreaFormacionId { get; set; }
    public string? Intensidad { get; set; }
    public string Institucion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public int? EstadoId { get; set; }
    public string TituloOtorgado { get; set; } = string.Empty;

    public InformacionBasica? Basica { get; set; }
    public Parametro? TipoFormacion { get; set; }
    public Parametro? AreaFormacion { get; set; }
    public Parametro? Estado { get; set; }
}
