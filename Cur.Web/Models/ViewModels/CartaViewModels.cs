using System.ComponentModel.DataAnnotations;
using Cur.Web.Models;

namespace Cur.Web.Models.ViewModels;

/// <summary>Un logro de la hoja de vida, listo para ofrecerse como evidencia en la carta.</summary>
public record OpcionLogro(int Id, string Titulo, string Descripcion, string Origen);

/// <summary>Una competencia con su nivel declarado y el cargo del que proviene.</summary>
public record OpcionCompetencia(int Id, string Descripcion, string Nivel, string Origen);

public class CartaViewModel
{
    [Required(ErrorMessage = "Escribe el cargo al que aspiras.")]
    [StringLength(150)]
    [Display(Name = "Cargo al que aspiras")]
    public string CargoObjetivo { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Empresa o convocatoria")]
    public string? Empresa { get; set; }

    [StringLength(600, ErrorMessage = "Deja la motivación en 600 caracteres o menos.")]
    [Display(Name = "¿Por qué te interesa?")]
    public string? Motivacion { get; set; }

    [Display(Name = "Logros que quieres destacar")]
    public List<int> LogrosIds { get; set; } = new();

    [Display(Name = "Competencias que quieres destacar")]
    public List<int> CompetenciasIds { get; set; } = new();

    [Display(Name = "Texto de la carta")]
    public string? Texto { get; set; }

    [Display(Name = "Anteponer la carta a mi hoja de vida")]
    public bool IncluirEnHojaDeVida { get; set; }

    /// <summary>Tono vigente del usuario, elegido en la pantalla de estilo.</summary>
    public TonoCarta Tono { get; set; } = Tonos.PorDefecto;

    /// <summary>Falso mientras el usuario no haya pasado por la pantalla de estilo.</summary>
    public bool TonoDefinido { get; set; }

    /// <summary>Verdadero si ya hay una carta guardada en la base.</summary>
    public bool TieneCartaGuardada { get; set; }

    public IReadOnlyList<OpcionLogro> Logros { get; set; } = Array.Empty<OpcionLogro>();
    public IReadOnlyList<OpcionCompetencia> Competencias { get; set; } = Array.Empty<OpcionCompetencia>();

    public TonoInfo InfoTono => Tonos.Info(Tono);
    public bool HayEvidencia => Logros.Count > 0 || Competencias.Count > 0;
}

/// <summary>Cuestionario corto que sugiere el tono de redacción.</summary>
public class EstiloCartaViewModel
{
    /// <summary>Una respuesta por pregunta, en el mismo orden de <see cref="CuestionarioTono.Preguntas"/>.</summary>
    public List<int> Respuestas { get; set; } = new();

    /// <summary>Tono guardado hoy.</summary>
    public TonoCarta Actual { get; set; } = Tonos.PorDefecto;

    /// <summary>Falso mientras el usuario no haya definido un tono.</summary>
    public bool Definido { get; set; }

    /// <summary>Resultado recién calculado, solo para mostrar la confirmación.</summary>
    public TonoCarta? Resultado { get; set; }

    public IReadOnlyList<PreguntaTono> Preguntas => CuestionarioTono.Preguntas;
    public IReadOnlyList<string> Escala => CuestionarioTono.Escala;
    public IReadOnlyList<TonoInfo> Disponibles => Tonos.Catalogo;
}
