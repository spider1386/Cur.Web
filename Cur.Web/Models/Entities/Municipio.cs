namespace Cur.Web.Models.Entities;

public class Municipio
{
    public int MunicipioId { get; set; }
    public int DepartamentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    public Departamento? Departamento { get; set; }
}
