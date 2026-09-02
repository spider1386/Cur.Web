using System.ComponentModel.DataAnnotations;
using Cur.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cur.Web.Models.ViewModels;

/// <summary>Cifras y accesos del tablero de administración.</summary>
public class AdminDashboardViewModel
{
    public int TotalUsuarios { get; set; }
    public int UsuariosBloqueados { get; set; }
    public int UsuariosSinConfirmar { get; set; }
    public int TotalHojasVida { get; set; }
    public int HojasVidaSinDuenio { get; set; }
    public int TotalExperiencias { get; set; }
    public int TotalFormaciones { get; set; }
    public int TotalLogros { get; set; }
    public int TotalCompetencias { get; set; }
    public int TotalParametros { get; set; }

    public IReadOnlyList<GrupoResumen> Grupos { get; set; } = Array.Empty<GrupoResumen>();

    /// <summary>Usuarios sin hoja de vida iniciada, para seguimiento.</summary>
    public int UsuariosSinHojaVida => Math.Max(TotalUsuarios - UsuariosConHojaVida, 0);
    public int UsuariosConHojaVida { get; set; }

    public record GrupoResumen(int Codigo, string Nombre, string Icono, int Cantidad);
}

/// <summary>Fila del listado de usuarios del panel.</summary>
public class UsuarioAdminViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmado { get; set; }
    public bool Bloqueado { get; set; }
    public DateTimeOffset? BloqueadoHasta { get; set; }
    public int AccesosFallidos { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public bool TieneHojaVida { get; set; }
    public string? NombreCompleto { get; set; }
    public int Experiencias { get; set; }
    public int Formaciones { get; set; }

    public bool EsAdministrador => Roles.Contains(Models.Roles.Administrador);
}

public class UsuariosAdminViewModel
{
    public string? Busqueda { get; set; }
    public IReadOnlyList<UsuarioAdminViewModel> Usuarios { get; set; } = Array.Empty<UsuarioAdminViewModel>();
}

/// <summary>Catálogo de Parametros agrupado por el código de grupo.</summary>
public class ParametrosAdminViewModel
{
    public IReadOnlyList<Grupo> Grupos { get; set; } = Array.Empty<Grupo>();
    public int? GrupoAbierto { get; set; }

    public record Grupo(int Codigo, string Nombre, string Icono, IReadOnlyList<Parametro> Parametros);
}

public class ParametroFormViewModel
{
    public int ParametroId { get; set; }

    [Required(ErrorMessage = "La etiqueta del tipo es obligatoria.")]
    [StringLength(30, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Etiqueta del tipo")]
    public string Tipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(200, ErrorMessage = "Máximo {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona el grupo.")]
    [Display(Name = "Grupo")]
    public int Codigo { get; set; }

    public IEnumerable<SelectListItem> Grupos { get; set; } = Array.Empty<SelectListItem>();

    public bool EsNuevo => ParametroId == 0;
}
