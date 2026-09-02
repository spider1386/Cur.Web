namespace Cur.Web.Models.Entities;

public class Departamento
{
    public int DepartamentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Municipio> Municipios { get; set; } = new List<Municipio>();
}
