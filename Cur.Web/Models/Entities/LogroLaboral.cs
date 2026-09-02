namespace Cur.Web.Models.Entities;

/// <summary>Logros y funciones de un cargo. Tabla Logros_Laborales.</summary>
public class LogroLaboral
{
    public int LogroId { get; set; }
    public int LaboralId { get; set; }
    public int? TipoId { get; set; }
    public string Logro { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public InformacionLaboral? Laboral { get; set; }
    public Parametro? Tipo { get; set; }
}
