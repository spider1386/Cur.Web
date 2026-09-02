using System.ComponentModel.DataAnnotations;

namespace Cur.Web.Services;

/// <summary>Sección "AzureGraphMail" de appsettings.</summary>
public class GraphMailOptions
{
    public const string SectionName = "AzureGraphMail";

    [Required] public string TenantId { get; set; } = string.Empty;
    [Required] public string ClientId { get; set; } = string.Empty;
    [Required] public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Buzón desde el que se envían los correos (permiso Mail.Send de aplicación).</summary>
    [Required, EmailAddress] public string SenderEmail { get; set; } = string.Empty;

    /// <summary>Nombre visible del remitente.</summary>
    public string SenderName { get; set; } = "Curriculum";

    /// <summary>Deja el correo enviado en la carpeta Elementos enviados del buzón.</summary>
    public bool SaveToSentItems { get; set; } = true;
}
