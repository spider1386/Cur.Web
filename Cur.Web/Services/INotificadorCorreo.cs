namespace Cur.Web.Services;

/// <summary>Adjunto en memoria para un correo saliente.</summary>
public record AdjuntoCorreo(string NombreArchivo, string ContentType, byte[] Contenido);

public interface INotificadorCorreo
{
    /// <summary>Envía un correo HTML. Nunca lanza: registra el fallo y devuelve false.</summary>
    Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        IEnumerable<AdjuntoCorreo>? adjuntos = null,
        CancellationToken ct = default);
}
