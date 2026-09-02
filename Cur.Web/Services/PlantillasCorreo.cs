using System.Net;

namespace Cur.Web.Services;

/// <summary>Plantillas HTML de los correos transaccionales.</summary>
public static class PlantillasCorreo
{
    private const string Marca = "Curriculum";

    private static string Envolver(string titulo, string contenido) => $@"
<!DOCTYPE html>
<html lang=""es"">
<body style=""margin:0;padding:0;background:#f4f6fb;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#1f2937;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f4f6fb;padding:32px 12px;"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:560px;background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 6px 24px rgba(15,23,42,.08);"">
        <tr><td style=""background:#1d4ed8;padding:22px 28px;color:#ffffff;font-size:18px;font-weight:600;"">{Marca}</td></tr>
        <tr><td style=""padding:28px;"">
          <h1 style=""margin:0 0 14px;font-size:20px;color:#111827;"">{titulo}</h1>
          {contenido}
        </td></tr>
        <tr><td style=""padding:18px 28px;background:#f9fafb;color:#6b7280;font-size:12px;"">
          Este mensaje se generó automáticamente. Si no reconoces esta actividad, ignóralo.
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

    private static string Boton(string url, string texto) => $@"
<p style=""margin:24px 0;"">
  <a href=""{WebUtility.HtmlEncode(url)}"" style=""display:inline-block;background:#1d4ed8;color:#ffffff;text-decoration:none;padding:12px 22px;border-radius:8px;font-weight:600;"">{texto}</a>
</p>
<p style=""margin:0;font-size:12px;color:#6b7280;word-break:break-all;"">Si el botón no funciona, copia este enlace: {WebUtility.HtmlEncode(url)}</p>";

    public static string Bienvenida(string email, string urlConfirmacion) => Envolver(
        "Confirma tu correo",
        $@"<p style=""margin:0;line-height:1.6;"">Hola, creamos tu cuenta con el correo <strong>{WebUtility.HtmlEncode(email)}</strong>.
           Confirma tu dirección para empezar a construir tu hoja de vida.</p>{Boton(urlConfirmacion, "Confirmar correo")}");

    public static string RestablecerClave(string urlRestablecer) => Envolver(
        "Restablece tu contraseña",
        $@"<p style=""margin:0;line-height:1.6;"">Recibimos una solicitud para cambiar tu contraseña.
           El enlace es de un solo uso y caduca en poco tiempo.</p>{Boton(urlRestablecer, "Crear nueva contraseña")}");

    public static string CurriculumGenerado(string nombre) => Envolver(
        "Tu hoja de vida está lista",
        $@"<p style=""margin:0;line-height:1.6;"">Hola {WebUtility.HtmlEncode(nombre)}, adjuntamos el PDF de tu hoja de vida
           generado el {DateTime.Now:dd/MM/yyyy 'a las' HH:mm}.</p>
           <p style=""margin:16px 0 0;line-height:1.6;"">Puedes volver a descargarlo cuando quieras desde tu panel.</p>");

    public static string PerfilActualizado(string nombre, string seccion) => Envolver(
        "Actualizamos tu hoja de vida",
        $@"<p style=""margin:0;line-height:1.6;"">Hola {WebUtility.HtmlEncode(nombre)}, registramos un cambio en la sección
           <strong>{WebUtility.HtmlEncode(seccion)}</strong> el {DateTime.Now:dd/MM/yyyy HH:mm}.</p>");
}
