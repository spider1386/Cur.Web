using System.Globalization;
using System.Text;

namespace Cur.Web.Services.Pdf;

/// <summary>Formateo compartido por todas las plantillas del PDF.</summary>
public static class PlantillaComun
{
    public static readonly CultureInfo Cultura = new("es-CO");

    public static string Periodo(DateTime inicio, DateTime? fin)
    {
        var desde = Capitalizar(inicio.ToString("MMM yyyy", Cultura));
        var hasta = fin is null ? "Actual" : Capitalizar(fin.Value.ToString("MMM yyyy", Cultura));
        return $"{desde} – {hasta}";
    }

    public static string Duracion(int meses)
    {
        var anios = meses / 12;
        var resto = meses % 12;
        if (anios == 0) return resto == 1 ? "1 mes" : $"{resto} meses";
        if (resto == 0) return anios == 1 ? "1 año" : $"{anios} años";
        return $"{anios} a {resto} m";
    }

    public static string Capitalizar(string valor) =>
        string.IsNullOrEmpty(valor) ? valor : char.ToUpper(valor[0], Cultura) + valor[1..];

    /// <summary>Los datos heredados traen guiones al inicio; se quitan para no duplicar la viñeta.</summary>
    public static string LimpiarVinieta(string valor) => valor.TrimStart('-', ' ', '•').Trim();

    /// <summary>Une los valores no vacíos con un separador, útil para líneas de metadatos.</summary>
    public static string Unir(string separador, params string?[] partes) =>
        string.Join(separador, partes.Where(p => !string.IsNullOrWhiteSpace(p)));

    public static string FechaGeneracion() =>
        $"Generado el {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", Cultura)}";

    /// <summary>
    /// Nombre del archivo descargado, con el mismo formato para todos los medios:
    /// HV-nombre-apellido-20260903.pdf / .html
    /// </summary>
    public static string NombreArchivo(string? nombreCompleto, string extension)
    {
        var limpio = new StringBuilder();

        foreach (var c in (nombreCompleto ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(c)) limpio.Append(char.ToLowerInvariant(c));
            else if (char.IsWhiteSpace(c) && limpio.Length > 0 && limpio[^1] != '-') limpio.Append('-');
        }

        var slug = limpio.ToString().Trim('-');
        if (slug.Length == 0) slug = "hoja-de-vida";

        return $"HV-{slug}-{DateTime.Now:yyyyMMdd}.{extension}";
    }
}
