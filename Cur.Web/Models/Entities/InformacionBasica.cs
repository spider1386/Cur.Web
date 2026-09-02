namespace Cur.Web.Models.Entities;

/// <summary>Hoja de vida: datos personales. Tabla Informacion_Basica.</summary>
public class InformacionBasica
{
    public int BasicaId { get; set; }
    public string? UrlImagen { get; set; }
    public string? Nombres { get; set; }
    public string Apellidos { get; set; } = string.Empty;
    public decimal Documento { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TelefonoFijo { get; set; } = string.Empty;
    public string TelefonoMovil { get; set; } = string.Empty;
    public string PerfilProfesional { get; set; } = string.Empty;
    public int ProfesionId { get; set; }
    public int DepartamentoId { get; set; }
    public int CiudadId { get; set; }

    /// <summary>Id del usuario de Identity propietario de la hoja de vida.</summary>
    public string? UserId { get; set; }

    public Parametro? Profesion { get; set; }
    public Departamento? Departamento { get; set; }
    public Municipio? Ciudad { get; set; }

    public ICollection<InformacionLaboral> Experiencia { get; set; } = new List<InformacionLaboral>();
    public ICollection<FormacionAcademica> Formacion { get; set; } = new List<FormacionAcademica>();

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
