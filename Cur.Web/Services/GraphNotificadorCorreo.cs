using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace Cur.Web.Services;

/// <summary>
/// Envío de correo con Microsoft Graph usando client credentials.
/// Requiere el permiso de aplicación Mail.Send con consentimiento del administrador.
/// </summary>
public class GraphNotificadorCorreo : INotificadorCorreo
{
    private readonly GraphServiceClient _graph;
    private readonly GraphMailOptions _opciones;
    private readonly ILogger<GraphNotificadorCorreo> _log;

    public GraphNotificadorCorreo(
        GraphServiceClient graph,
        IOptions<GraphMailOptions> opciones,
        ILogger<GraphNotificadorCorreo> log)
    {
        _graph = graph;
        _opciones = opciones.Value;
        _log = log;
    }

    public async Task<bool> EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        IEnumerable<AdjuntoCorreo>? adjuntos = null,
        CancellationToken ct = default)
    {
        var mensaje = new Message
        {
            Subject = asunto,
            Body = new ItemBody { ContentType = BodyType.Html, Content = cuerpoHtml },
            ToRecipients = new List<Recipient>
            {
                new() { EmailAddress = new EmailAddress { Address = destinatario } }
            },
            From = new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = _opciones.SenderEmail,
                    Name = _opciones.SenderName
                }
            }
        };

        if (adjuntos is not null)
        {
            mensaje.Attachments = adjuntos
                .Select(a => (Attachment)new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = a.NombreArchivo,
                    ContentType = a.ContentType,
                    ContentBytes = a.Contenido
                })
                .ToList();
        }

        try
        {
            await _graph.Users[_opciones.SenderEmail]
                .SendMail
                .PostAsync(
                    new SendMailPostRequestBody
                    {
                        Message = mensaje,
                        SaveToSentItems = _opciones.SaveToSentItems
                    },
                    cancellationToken: ct);

            _log.LogInformation("Correo enviado a {Destinatario}: {Asunto}", destinatario, asunto);
            return true;
        }
        catch (Exception ex)
        {
            // El envío de correo nunca debe tumbar el flujo de registro o descarga.
            _log.LogError(ex, "Fallo al enviar correo a {Destinatario}: {Asunto}", destinatario, asunto);
            return false;
        }
    }

    /// <summary>Crea el cliente de Graph con las credenciales de la aplicación.</summary>
    public static GraphServiceClient CrearCliente(GraphMailOptions opciones)
    {
        var credencial = new ClientSecretCredential(
            opciones.TenantId,
            opciones.ClientId,
            opciones.ClientSecret);

        return new GraphServiceClient(credencial, new[] { "https://graph.microsoft.com/.default" });
    }
}
