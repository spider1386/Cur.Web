namespace Cur.Web.Models.Entities;

/// <summary>Experiencia laboral. Tabla Informacion_Laboral.</summary>
public class InformacionLaboral
{
    public int LaboralId { get; set; }
    public int BasicaId { get; set; }
    public int CargoId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaRetiro { get; set; }
    /// <summary>Meses laborados. Se recalcula al guardar.</summary>
    public int? TiempoLaborado { get; set; }
    public int EstadoId { get; set; }
    public string Empresa { get; set; } = string.Empty;
    public int AreaId { get; set; }
    public string JefeInmediato { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;

    public InformacionBasica? Basica { get; set; }
    public Parametro? Cargo { get; set; }
    public Parametro? Area { get; set; }
    public Parametro? Estado { get; set; }

    public ICollection<LogroLaboral> Logros { get; set; } = new List<LogroLaboral>();
    public ICollection<Competencia> Competencias { get; set; } = new List<Competencia>();

    /// <summary>Meses transcurridos entre el ingreso y el retiro (o la fecha actual si sigue vigente).</summary>
    public int CalcularMesesLaborados()
    {
        var fin = FechaRetiro ?? DateTime.Today;
        var meses = ((fin.Year - FechaInicio.Year) * 12) + fin.Month - FechaInicio.Month;
        if (fin.Day < FechaInicio.Day) meses--;
        return Math.Max(meses, 0);
    }
}
