namespace Cur.Web.Models.Entities;

/// <summary>
/// Carta de presentacion del usuario. Tabla Carta_Presentacion.
///
/// Es tabla nueva, no heredada: por eso las columnas llevan nombres completos y no
/// las abreviaturas del esquema viejo. Se crea con Data/Scripts/001_Carta_Presentacion.sql;
/// el proyecto no genera migraciones para las tablas de negocio.
/// </summary>
public class CartaPresentacion
{
    public int CartaId { get; set; }

    /// <summary>Id del usuario de Identity propietario de la carta.</summary>
    public string UserId { get; set; } = string.Empty;

    public string CargoObjetivo { get; set; } = string.Empty;
    public string? Empresa { get; set; }

    /// <summary>Tono con el que se genero el ultimo borrador.</summary>
    public TonoCarta Tono { get; set; } = Tonos.PorDefecto;

    /// <summary>Texto final. Lo escribe el generador pero manda siempre lo que deje el usuario.</summary>
    public string Texto { get; set; } = string.Empty;

    /// <summary>Si la carta debe anteponerse como portada en el PDF y el HTML.</summary>
    public bool IncluirEnHojaDeVida { get; set; }

    public DateTime ActualizadaEn { get; set; }
}
