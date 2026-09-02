using System.ComponentModel.DataAnnotations;
using Cur.Web.Models;
using Cur.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cur.Web.Models.ViewModels;

/// <summary>Vista consolidada de la hoja de vida del usuario autenticado.</summary>
public class CurriculumViewModel
{
    public InformacionBasica? Basica { get; set; }
    public IReadOnlyList<InformacionLaboral> Experiencia { get; set; } = Array.Empty<InformacionLaboral>();
    public IReadOnlyList<FormacionAcademica> Formacion { get; set; } = Array.Empty<FormacionAcademica>();

    /// <summary>Catálogos para los formularios de logros y competencias embebidos en el acordeón.</summary>
    public IEnumerable<SelectListItem> TiposLogro { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> TiposAptitud { get; set; } = Array.Empty<SelectListItem>();

    public bool TienePerfil => Basica is not null;

    /// <summary>Porcentaje de avance para la barra de progreso del panel.</summary>
    public int PorcentajeCompletitud
    {
        get
        {
            if (Basica is null) return 0;
            var puntos = 40;
            if (!string.IsNullOrWhiteSpace(Basica.PerfilProfesional)) puntos += 10;
            if (!string.IsNullOrWhiteSpace(Basica.UrlImagen)) puntos += 10;
            if (Experiencia.Count > 0) puntos += 20;
            if (Formacion.Count > 0) puntos += 20;
            return Math.Min(puntos, 100);
        }
    }
}

public class PerfilViewModel
{
    public int BasicaId { get; set; }

    [Display(Name = "Nombres")]
    [StringLength(50)]
    public string? Nombres { get; set; }

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(50)]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento es obligatorio.")]
    [Range(1, 999999999999999, ErrorMessage = "Documento no válido.")]
    [Display(Name = "Documento de identidad")]
    public decimal Documento { get; set; }

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(50)]
    [Display(Name = "Correo de contacto")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono fijo es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Teléfono fijo")]
    public string TelefonoFijo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El celular es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Celular")]
    public string TelefonoMovil { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu perfil profesional.")]
    [Display(Name = "Perfil profesional")]
    public string PerfilProfesional { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona una profesión.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una profesión.")]
    [Display(Name = "Profesión")]
    public int ProfesionId { get; set; }

    [Required(ErrorMessage = "Selecciona un departamento.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un departamento.")]
    [Display(Name = "Departamento")]
    public int DepartamentoId { get; set; }

    [Required(ErrorMessage = "Selecciona una ciudad.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una ciudad.")]
    [Display(Name = "Ciudad")]
    public int CiudadId { get; set; }

    /// <summary>Ruta pública de la foto ya almacenada.</summary>
    public string? UrlImagen { get; set; }

    [Display(Name = "Foto de perfil")]
    public IFormFile? Foto { get; set; }

    public IEnumerable<SelectListItem> Profesiones { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Departamentos { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Ciudades { get; set; } = Array.Empty<SelectListItem>();
}

public class ExperienciaViewModel : IValidatableObject
{
    public int LaboralId { get; set; }

    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(250)]
    [Display(Name = "Empresa")]
    public string Empresa { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el cargo.")]
    [Display(Name = "Cargo")]
    public int CargoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el área ocupacional.")]
    [Display(Name = "Área ocupacional")]
    public int AreaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el estado.")]
    [Display(Name = "Estado")]
    public int EstadoId { get; set; }

    [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de ingreso")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de retiro")]
    public DateTime? FechaRetiro { get; set; }

    [Required(ErrorMessage = "El jefe inmediato es obligatorio.")]
    [StringLength(50)]
    [Display(Name = "Jefe inmediato")]
    public string JefeInmediato { get; set; } = string.Empty;

    [Required(ErrorMessage = "El contacto es obligatorio.")]
    [StringLength(30)]
    [Display(Name = "Teléfono de contacto")]
    public string Contacto { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> Cargos { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Areas { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Estados { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicio > DateTime.Today)
            yield return new ValidationResult("La fecha de ingreso no puede ser futura.", new[] { nameof(FechaInicio) });

        if (FechaRetiro.HasValue && FechaRetiro.Value < FechaInicio)
            yield return new ValidationResult("La fecha de retiro no puede ser anterior al ingreso.", new[] { nameof(FechaRetiro) });
    }
}

public class FormacionViewModel : IValidatableObject
{
    public int FormacionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el tipo de formación.")]
    [Display(Name = "Tipo de formación")]
    public int TipoFormacionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el área.")]
    [Display(Name = "Área de formación")]
    public int AreaFormacionId { get; set; }

    [Required(ErrorMessage = "La institución es obligatoria.")]
    [StringLength(150)]
    [Display(Name = "Institución")]
    public string Institucion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título obtenido es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Título otorgado")]
    public string TituloOtorgado { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Intensidad horaria")]
    public string? Intensidad { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de inicio")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de finalización")]
    public DateTime? FechaFinalizacion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el estado.")]
    [Display(Name = "Estado")]
    public int EstadoId { get; set; }

    public IEnumerable<SelectListItem> Tipos { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Areas { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Estados { get; set; } = Array.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaFinalizacion.HasValue && FechaFinalizacion.Value < FechaInicio)
            yield return new ValidationResult("La fecha de finalización no puede ser anterior al inicio.", new[] { nameof(FechaFinalizacion) });
    }
}

/// <summary>Selector de plantilla del PDF.</summary>
public class PlantillasViewModel
{
    public PlantillaCv Seleccionada { get; set; } = Models.Plantillas.PorDefecto;
    public IReadOnlyList<PlantillaInfo> Disponibles { get; set; } = Models.Plantillas.Catalogo;
}

public class LogroInputModel
{
    public int LaboralId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el tipo.")]
    [Display(Name = "Tipo")]
    public int TipoId { get; set; }

    [Required(ErrorMessage = "El título del logro es obligatorio.")]
    [StringLength(50)]
    [Display(Name = "Logro o función")]
    public string Logro { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(150)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;
}

public class CompetenciaInputModel
{
    public int LaboralId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el tipo.")]
    [Display(Name = "Tipo")]
    public int TipoCompetenciaId { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(250)]
    [Display(Name = "Competencia o habilidad")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica el nivel.")]
    [StringLength(250)]
    [Display(Name = "Nivel")]
    public string Medicion { get; set; } = string.Empty;
}
