using Cur.Web.Data;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cur.Web.Services;

public interface ICurriculumService
{
    Task<CurriculumViewModel> ObtenerCurriculumAsync(string userId, CancellationToken ct = default);
    Task<InformacionBasica?> ObtenerBasicaAsync(string userId, CancellationToken ct = default);
    Task<int> GuardarPerfilAsync(string userId, PerfilViewModel vm, string? urlImagen, CancellationToken ct = default);

    Task<InformacionLaboral?> ObtenerExperienciaAsync(int laboralId, string userId, CancellationToken ct = default);
    Task GuardarExperienciaAsync(string userId, ExperienciaViewModel vm, CancellationToken ct = default);
    Task<bool> EliminarExperienciaAsync(int laboralId, string userId, CancellationToken ct = default);

    Task<FormacionAcademica?> ObtenerFormacionAsync(int formacionId, string userId, CancellationToken ct = default);
    Task GuardarFormacionAsync(string userId, FormacionViewModel vm, CancellationToken ct = default);
    Task<bool> EliminarFormacionAsync(int formacionId, string userId, CancellationToken ct = default);

    Task<bool> AgregarLogroAsync(string userId, LogroInputModel vm, CancellationToken ct = default);
    Task<bool> EliminarLogroAsync(int logroId, string userId, CancellationToken ct = default);

    Task<bool> AgregarCompetenciaAsync(string userId, CompetenciaInputModel vm, CancellationToken ct = default);
    Task<bool> EliminarCompetenciaAsync(int competenciaId, string userId, CancellationToken ct = default);
}

/// <summary>
/// Toda la lectura y escritura de la hoja de vida. Cada operación se filtra por el
/// UserId de Identity, de forma que un usuario no puede tocar registros ajenos.
/// </summary>
public class CurriculumService : ICurriculumService
{
    private readonly ApplicationDbContext _db;

    public CurriculumService(ApplicationDbContext db) => _db = db;

    public async Task<InformacionBasica?> ObtenerBasicaAsync(string userId, CancellationToken ct = default) =>
        await _db.InformacionBasica
            .Include(b => b.Profesion)
            .Include(b => b.Departamento)
            .Include(b => b.Ciudad)
            .FirstOrDefaultAsync(b => b.UserId == userId, ct);

    public async Task<CurriculumViewModel> ObtenerCurriculumAsync(string userId, CancellationToken ct = default)
    {
        var basica = await _db.InformacionBasica.AsNoTracking()
            .Include(b => b.Profesion)
            .Include(b => b.Departamento)
            .Include(b => b.Ciudad)
            .FirstOrDefaultAsync(b => b.UserId == userId, ct);

        if (basica is null) return new CurriculumViewModel();

        var experiencia = await _db.InformacionLaboral.AsNoTracking()
            .Where(l => l.BasicaId == basica.BasicaId)
            .Include(l => l.Cargo)
            .Include(l => l.Area)
            .Include(l => l.Estado)
            .Include(l => l.Logros).ThenInclude(g => g.Tipo)
            .Include(l => l.Competencias).ThenInclude(c => c.TipoCompetencia)
            .OrderByDescending(l => l.FechaInicio)
            .ToListAsync(ct);

        var formacion = await _db.FormacionAcademica.AsNoTracking()
            .Where(f => f.BasicaId == basica.BasicaId)
            .Include(f => f.TipoFormacion)
            .Include(f => f.AreaFormacion)
            .Include(f => f.Estado)
            .OrderByDescending(f => f.FechaInicio)
            .ToListAsync(ct);

        return new CurriculumViewModel
        {
            Basica = basica,
            Experiencia = experiencia,
            Formacion = formacion
        };
    }

    public async Task<int> GuardarPerfilAsync(string userId, PerfilViewModel vm, string? urlImagen, CancellationToken ct = default)
    {
        var basica = await _db.InformacionBasica.FirstOrDefaultAsync(b => b.UserId == userId, ct);

        if (basica is null)
        {
            basica = new InformacionBasica { UserId = userId };
            _db.InformacionBasica.Add(basica);
        }

        basica.Nombres = vm.Nombres;
        basica.Apellidos = vm.Apellidos;
        basica.Documento = vm.Documento;
        basica.Email = vm.Email;
        basica.TelefonoFijo = vm.TelefonoFijo;
        basica.TelefonoMovil = vm.TelefonoMovil;
        basica.PerfilProfesional = vm.PerfilProfesional;
        basica.ProfesionId = vm.ProfesionId;
        basica.DepartamentoId = vm.DepartamentoId;
        basica.CiudadId = vm.CiudadId;

        if (!string.IsNullOrWhiteSpace(urlImagen))
            basica.UrlImagen = urlImagen;

        await _db.SaveChangesAsync(ct);
        return basica.BasicaId;
    }

    public async Task<InformacionLaboral?> ObtenerExperienciaAsync(int laboralId, string userId, CancellationToken ct = default) =>
        await _db.InformacionLaboral
            .Include(l => l.Basica)
            .Include(l => l.Cargo)
            .Include(l => l.Area)
            .Include(l => l.Estado)
            .Include(l => l.Logros).ThenInclude(g => g.Tipo)
            .Include(l => l.Competencias).ThenInclude(c => c.TipoCompetencia)
            .FirstOrDefaultAsync(l => l.LaboralId == laboralId && l.Basica!.UserId == userId, ct);

    public async Task GuardarExperienciaAsync(string userId, ExperienciaViewModel vm, CancellationToken ct = default)
    {
        var basicaId = await ExigirBasicaIdAsync(userId, ct);

        InformacionLaboral entidad;
        if (vm.LaboralId == 0)
        {
            entidad = new InformacionLaboral { BasicaId = basicaId };
            _db.InformacionLaboral.Add(entidad);
        }
        else
        {
            entidad = await _db.InformacionLaboral
                .FirstOrDefaultAsync(l => l.LaboralId == vm.LaboralId && l.BasicaId == basicaId, ct)
                ?? throw new KeyNotFoundException("La experiencia no existe o no te pertenece.");
        }

        entidad.Empresa = vm.Empresa;
        entidad.CargoId = vm.CargoId;
        entidad.AreaId = vm.AreaId;
        entidad.EstadoId = vm.EstadoId;
        entidad.FechaInicio = vm.FechaInicio;
        entidad.FechaRetiro = vm.FechaRetiro;
        entidad.JefeInmediato = vm.JefeInmediato;
        entidad.Contacto = vm.Contacto;
        entidad.TiempoLaborado = entidad.CalcularMesesLaborados();

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> EliminarExperienciaAsync(int laboralId, string userId, CancellationToken ct = default)
    {
        var entidad = await _db.InformacionLaboral
            .Include(l => l.Logros)
            .Include(l => l.Competencias)
            .FirstOrDefaultAsync(l => l.LaboralId == laboralId && l.Basica!.UserId == userId, ct);

        if (entidad is null) return false;

        // Las FK de la base no tienen ON DELETE CASCADE: hay que borrar los hijos a mano.
        _db.LogrosLaborales.RemoveRange(entidad.Logros);
        _db.Competencias.RemoveRange(entidad.Competencias);
        _db.InformacionLaboral.Remove(entidad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<FormacionAcademica?> ObtenerFormacionAsync(int formacionId, string userId, CancellationToken ct = default) =>
        await _db.FormacionAcademica
            .Include(f => f.Basica)
            .FirstOrDefaultAsync(f => f.FormacionId == formacionId && f.Basica!.UserId == userId, ct);

    public async Task GuardarFormacionAsync(string userId, FormacionViewModel vm, CancellationToken ct = default)
    {
        var basicaId = await ExigirBasicaIdAsync(userId, ct);

        FormacionAcademica entidad;
        if (vm.FormacionId == 0)
        {
            entidad = new FormacionAcademica { BasicaId = basicaId };
            _db.FormacionAcademica.Add(entidad);
        }
        else
        {
            entidad = await _db.FormacionAcademica
                .FirstOrDefaultAsync(f => f.FormacionId == vm.FormacionId && f.BasicaId == basicaId, ct)
                ?? throw new KeyNotFoundException("La formación no existe o no te pertenece.");
        }

        entidad.TipoFormacionId = vm.TipoFormacionId;
        entidad.AreaFormacionId = vm.AreaFormacionId;
        entidad.Institucion = vm.Institucion;
        entidad.TituloOtorgado = vm.TituloOtorgado;
        entidad.Intensidad = vm.Intensidad;
        entidad.FechaInicio = vm.FechaInicio;
        entidad.FechaFinalizacion = vm.FechaFinalizacion;
        entidad.EstadoId = vm.EstadoId;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> EliminarFormacionAsync(int formacionId, string userId, CancellationToken ct = default)
    {
        var entidad = await _db.FormacionAcademica
            .FirstOrDefaultAsync(f => f.FormacionId == formacionId && f.Basica!.UserId == userId, ct);

        if (entidad is null) return false;

        _db.FormacionAcademica.Remove(entidad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AgregarLogroAsync(string userId, LogroInputModel vm, CancellationToken ct = default)
    {
        if (!await EsExperienciaDelUsuarioAsync(vm.LaboralId, userId, ct)) return false;

        _db.LogrosLaborales.Add(new LogroLaboral
        {
            LaboralId = vm.LaboralId,
            TipoId = vm.TipoId,
            Logro = vm.Logro,
            Descripcion = vm.Descripcion
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> EliminarLogroAsync(int logroId, string userId, CancellationToken ct = default)
    {
        var entidad = await _db.LogrosLaborales
            .FirstOrDefaultAsync(l => l.LogroId == logroId && l.Laboral!.Basica!.UserId == userId, ct);

        if (entidad is null) return false;

        _db.LogrosLaborales.Remove(entidad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AgregarCompetenciaAsync(string userId, CompetenciaInputModel vm, CancellationToken ct = default)
    {
        if (!await EsExperienciaDelUsuarioAsync(vm.LaboralId, userId, ct)) return false;

        // Competencias.CmptnciaID no es IDENTITY en la base heredada. El id se calcula y se
        // inserta en una sola sentencia: UPDLOCK/HOLDLOCK bloquea el rango mientras dura el
        // INSERT, así que dos peticiones simultáneas no pueden tomar el mismo id. Se hace
        // asi, y no con una transaccion explicita, porque la conexion usa
        // EnableRetryOnFailure y esa estrategia no admite transacciones abiertas a mano.
        var filas = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO Competencias (CmptnciaID, LbralID, Tpo_cmptnciaID, Dscrpcion_Cmptncia, Medicion)
            SELECT ISNULL(MAX(c.CmptnciaID), 0) + 1,
                   {vm.LaboralId}, {vm.TipoCompetenciaId}, {vm.Descripcion}, {vm.Medicion}
            FROM Competencias AS c WITH (UPDLOCK, HOLDLOCK)", ct);

        return filas > 0;
    }

    public async Task<bool> EliminarCompetenciaAsync(int competenciaId, string userId, CancellationToken ct = default)
    {
        var entidad = await _db.Competencias
            .FirstOrDefaultAsync(c => c.CompetenciaId == competenciaId && c.Laboral!.Basica!.UserId == userId, ct);

        if (entidad is null) return false;

        _db.Competencias.Remove(entidad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private Task<bool> EsExperienciaDelUsuarioAsync(int laboralId, string userId, CancellationToken ct) =>
        _db.InformacionLaboral.AnyAsync(l => l.LaboralId == laboralId && l.Basica!.UserId == userId, ct);

    private async Task<int> ExigirBasicaIdAsync(string userId, CancellationToken ct)
    {
        var id = await _db.InformacionBasica
            .Where(b => b.UserId == userId)
            .Select(b => (int?)b.BasicaId)
            .FirstOrDefaultAsync(ct);

        return id ?? throw new InvalidOperationException("Primero debes completar tus datos personales.");
    }
}
