namespace Cur.Web.Services;

public interface IFotoPerfilStorage
{
    /// <summary>Guarda la foto y devuelve la ruta pública (por ejemplo /fotos/abc.jpg).</summary>
    Task<string> GuardarAsync(IFormFile archivo, string? rutaAnterior, CancellationToken ct = default);

    /// <summary>Lee los bytes de una foto ya almacenada. Devuelve null si no existe.</summary>
    byte[]? LeerBytes(string? rutaPublica);

    void Eliminar(string? rutaPublica);
}

/// <summary>
/// Guarda las fotos de perfil en wwwroot/fotos, que es el mismo esquema de rutas
/// que ya trae la columna Informacion_Basica.UrlImgen.
/// </summary>
public class FotoPerfilStorage : IFotoPerfilStorage
{
    public const long TamanoMaximoBytes = 2 * 1024 * 1024;
    private const string CarpetaPublica = "fotos";

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    // Firmas binarias para no fiarnos solo de la extensión declarada por el cliente.
    private static readonly byte[][] Firmas =
    {
        new byte[] { 0xFF, 0xD8, 0xFF },                          // JPEG
        new byte[] { 0x89, 0x50, 0x4E, 0x47 },                    // PNG
        new byte[] { 0x52, 0x49, 0x46, 0x46 }                     // RIFF (WEBP)
    };

    private readonly IWebHostEnvironment _entorno;
    private readonly ILogger<FotoPerfilStorage> _log;

    public FotoPerfilStorage(IWebHostEnvironment entorno, ILogger<FotoPerfilStorage> log)
    {
        _entorno = entorno;
        _log = log;
    }

    public async Task<string> GuardarAsync(IFormFile archivo, string? rutaAnterior, CancellationToken ct = default)
    {
        if (archivo.Length == 0)
            throw new InvalidOperationException("El archivo está vacío.");

        if (archivo.Length > TamanoMaximoBytes)
            throw new InvalidOperationException("La foto no puede superar 2 MB.");

        var extension = Path.GetExtension(archivo.FileName);
        if (!ExtensionesPermitidas.Contains(extension))
            throw new InvalidOperationException("Formato no permitido. Usa JPG, PNG o WEBP.");

        await using var origen = archivo.OpenReadStream();
        var cabecera = new byte[8];
        var leidos = await origen.ReadAsync(cabecera, ct);
        if (!EsImagen(cabecera.AsSpan(0, leidos)))
            throw new InvalidOperationException("El archivo no es una imagen válida.");
        origen.Position = 0;

        var destinoDir = Path.Combine(_entorno.WebRootPath, CarpetaPublica);
        Directory.CreateDirectory(destinoDir);

        var nombre = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destino = Path.Combine(destinoDir, nombre);

        await using (var salida = File.Create(destino))
        {
            await origen.CopyToAsync(salida, ct);
        }

        Eliminar(rutaAnterior);
        return $"/{CarpetaPublica}/{nombre}";
    }

    public byte[]? LeerBytes(string? rutaPublica)
    {
        var fisica = ARutaFisica(rutaPublica);
        if (fisica is null || !File.Exists(fisica)) return null;

        try
        {
            return File.ReadAllBytes(fisica);
        }
        catch (IOException ex)
        {
            _log.LogWarning(ex, "No se pudo leer la foto {Ruta}", rutaPublica);
            return null;
        }
    }

    public void Eliminar(string? rutaPublica)
    {
        var fisica = ARutaFisica(rutaPublica);
        if (fisica is null || !File.Exists(fisica)) return;

        try
        {
            File.Delete(fisica);
        }
        catch (IOException ex)
        {
            _log.LogWarning(ex, "No se pudo eliminar la foto {Ruta}", rutaPublica);
        }
    }

    /// <summary>
    /// Traduce la ruta pública a ruta física dentro de wwwroot y descarta cualquier
    /// intento de salirse de esa carpeta. Los registros heredados con rutas absolutas
    /// de otra máquina se ignoran.
    /// </summary>
    private string? ARutaFisica(string? rutaPublica)
    {
        if (string.IsNullOrWhiteSpace(rutaPublica)) return null;
        if (!rutaPublica.StartsWith('/')) return null;

        var relativa = rutaPublica.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var raiz = Path.GetFullPath(_entorno.WebRootPath);
        var completa = Path.GetFullPath(Path.Combine(raiz, relativa));

        return completa.StartsWith(raiz, StringComparison.OrdinalIgnoreCase) ? completa : null;
    }

    private static bool EsImagen(ReadOnlySpan<byte> cabecera)
    {
        foreach (var firma in Firmas)
        {
            if (cabecera.Length >= firma.Length && cabecera[..firma.Length].SequenceEqual(firma))
                return true;
        }
        return false;
    }
}
