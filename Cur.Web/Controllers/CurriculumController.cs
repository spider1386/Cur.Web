using Cur.Web.Models;
using Cur.Web.Models.Entities;
using Cur.Web.Models.ViewModels;
using Cur.Web.Services;
using Cur.Web.Services.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cur.Web.Controllers;

[Authorize]
public class CurriculumController : Controller
{
    private readonly ICurriculumService _curriculum;
    private readonly ICatalogoService _catalogos;
    private readonly IFotoPerfilStorage _fotos;
    private readonly ICurriculumPdfGenerator _pdf;
    private readonly INotificadorCorreo _correo;
    private readonly IPlantillaPreferencia _plantillas;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<CurriculumController> _log;

    public CurriculumController(
        ICurriculumService curriculum,
        ICatalogoService catalogos,
        IFotoPerfilStorage fotos,
        ICurriculumPdfGenerator pdf,
        INotificadorCorreo correo,
        IPlantillaPreferencia plantillas,
        UserManager<IdentityUser> userManager,
        ILogger<CurriculumController> log)
    {
        _curriculum = curriculum;
        _catalogos = catalogos;
        _fotos = fotos;
        _pdf = pdf;
        _correo = correo;
        _plantillas = plantillas;
        _userManager = userManager;
        _log = log;
    }

    private string UserId => _userManager.GetUserId(User)!;

    // ---------- Panel ----------

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        cv.TiposLogro = await _catalogos.ParametrosAsync(GrupoParametro.TipoLogro, ct: ct);
        cv.TiposAptitud = await _catalogos.ParametrosAsync(GrupoParametro.TipoAptitud, ct: ct);
        return View(cv);
    }

    // ---------- Datos personales ----------

    [HttpGet]
    public async Task<IActionResult> Perfil(CancellationToken ct)
    {
        var basica = await _curriculum.ObtenerBasicaAsync(UserId, ct);

        var vm = basica is null
            ? new PerfilViewModel { Email = User.Identity?.Name ?? string.Empty }
            : new PerfilViewModel
            {
                BasicaId = basica.BasicaId,
                Nombres = basica.Nombres,
                Apellidos = basica.Apellidos,
                Documento = basica.Documento,
                Email = basica.Email,
                TelefonoFijo = basica.TelefonoFijo,
                TelefonoMovil = basica.TelefonoMovil,
                PerfilProfesional = basica.PerfilProfesional,
                ProfesionId = basica.ProfesionId,
                DepartamentoId = basica.DepartamentoId,
                CiudadId = basica.CiudadId,
                UrlImagen = basica.UrlImagen
            };

        await CargarCatalogosPerfilAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> Perfil(PerfilViewModel vm, CancellationToken ct)
    {
        string? urlImagen = null;

        if (vm.Foto is not null)
        {
            try
            {
                var actual = await _curriculum.ObtenerBasicaAsync(UserId, ct);
                urlImagen = await _fotos.GuardarAsync(vm.Foto, actual?.UrlImagen, ct);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.Foto), ex.Message);
            }
        }

        if (!ModelState.IsValid)
        {
            await CargarCatalogosPerfilAsync(vm, ct);
            return View(vm);
        }

        await _curriculum.GuardarPerfilAsync(UserId, vm, urlImagen, ct);

        await NotificarAsync(vm.Email, "Datos personales", $"{vm.Nombres} {vm.Apellidos}".Trim());

        TempData["Exito"] = "Datos personales guardados.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Alimenta el select de ciudades cuando cambia el departamento.</summary>
    [HttpGet]
    public async Task<IActionResult> Municipios(int departamentoId, CancellationToken ct)
    {
        var items = await _catalogos.MunicipiosAsync(departamentoId, ct: ct);
        return Json(items.Select(i => new { value = i.Value, text = i.Text }));
    }

    // ---------- Experiencia laboral ----------

    [HttpGet]
    public async Task<IActionResult> Experiencia(int? id, CancellationToken ct)
    {
        var vm = new ExperienciaViewModel();

        if (id is > 0)
        {
            var entidad = await _curriculum.ObtenerExperienciaAsync(id.Value, UserId, ct);
            if (entidad is null) return NotFound();

            vm = new ExperienciaViewModel
            {
                LaboralId = entidad.LaboralId,
                Empresa = entidad.Empresa,
                CargoId = entidad.CargoId,
                AreaId = entidad.AreaId,
                EstadoId = entidad.EstadoId,
                FechaInicio = entidad.FechaInicio,
                FechaRetiro = entidad.FechaRetiro,
                JefeInmediato = entidad.JefeInmediato,
                Contacto = entidad.Contacto
            };
        }

        await CargarCatalogosExperienciaAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Experiencia(ExperienciaViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await CargarCatalogosExperienciaAsync(vm, ct);
            return View(vm);
        }

        try
        {
            await _curriculum.GuardarExperienciaAsync(UserId, vm, ct);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Perfil));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        TempData["Exito"] = "Experiencia guardada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarExperiencia(int id, CancellationToken ct)
    {
        var eliminado = await _curriculum.EliminarExperienciaAsync(id, UserId, ct);
        TempData[eliminado ? "Exito" : "Error"] = eliminado
            ? "Experiencia eliminada."
            : "No encontramos esa experiencia.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarLogro(LogroInputModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revisa los datos del logro.";
            return AlPanel(vm.LaboralId);
        }

        if (!await _curriculum.AgregarLogroAsync(UserId, vm, ct)) return NotFound();

        TempData["Exito"] = "Logro agregado.";
        return AlPanel(vm.LaboralId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarLogro(int id, int laboralId, CancellationToken ct)
    {
        if (!await _curriculum.EliminarLogroAsync(id, UserId, ct)) return NotFound();

        TempData["Exito"] = "Logro eliminado.";
        return AlPanel(laboralId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarCompetencia(CompetenciaInputModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revisa los datos de la competencia.";
            return AlPanel(vm.LaboralId);
        }

        if (!await _curriculum.AgregarCompetenciaAsync(UserId, vm, ct)) return NotFound();

        TempData["Exito"] = "Competencia agregada.";
        return AlPanel(vm.LaboralId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCompetencia(int id, int laboralId, CancellationToken ct)
    {
        if (!await _curriculum.EliminarCompetenciaAsync(id, UserId, ct)) return NotFound();

        TempData["Exito"] = "Competencia eliminada.";
        return AlPanel(laboralId);
    }

    // ---------- Formacion academica ----------

    [HttpGet]
    public async Task<IActionResult> Formacion(int? id, CancellationToken ct)
    {
        var vm = new FormacionViewModel();

        if (id is > 0)
        {
            var entidad = await _curriculum.ObtenerFormacionAsync(id.Value, UserId, ct);
            if (entidad is null) return NotFound();

            vm = new FormacionViewModel
            {
                FormacionId = entidad.FormacionId,
                TipoFormacionId = entidad.TipoFormacionId ?? 0,
                AreaFormacionId = entidad.AreaFormacionId ?? 0,
                Institucion = entidad.Institucion,
                TituloOtorgado = entidad.TituloOtorgado,
                Intensidad = entidad.Intensidad,
                FechaInicio = entidad.FechaInicio,
                FechaFinalizacion = entidad.FechaFinalizacion,
                EstadoId = entidad.EstadoId ?? 0
            };
        }

        await CargarCatalogosFormacionAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Formacion(FormacionViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await CargarCatalogosFormacionAsync(vm, ct);
            return View(vm);
        }

        try
        {
            await _curriculum.GuardarFormacionAsync(UserId, vm, ct);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Perfil));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        TempData["Exito"] = "Formación guardada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarFormacion(int id, CancellationToken ct)
    {
        var eliminado = await _curriculum.EliminarFormacionAsync(id, UserId, ct);
        TempData[eliminado ? "Exito" : "Error"] = eliminado
            ? "Formación eliminada."
            : "No encontramos ese registro.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Vista previa, PDF y envio ----------

    [HttpGet]
    public async Task<IActionResult> VistaPrevia(CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil) return RedirectToAction(nameof(Perfil));

        return View(cv);
    }

    [HttpGet]
    public async Task<IActionResult> Descargar(CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil)
        {
            TempData["Error"] = "Completa tus datos personales antes de descargar el PDF.";
            return RedirectToAction(nameof(Perfil));
        }

        var bytes = _pdf.Generar(cv, _fotos.LeerBytes(cv.Basica!.UrlImagen), _plantillas.Obtener(User));
        return File(bytes, "application/pdf", _pdf.NombreArchivo(cv));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarPorCorreo(CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil)
        {
            TempData["Error"] = "Completa tus datos personales antes de enviar el PDF.";
            return RedirectToAction(nameof(Perfil));
        }

        var bytes = _pdf.Generar(cv, _fotos.LeerBytes(cv.Basica!.UrlImagen), _plantillas.Obtener(User));
        var nombreArchivo = _pdf.NombreArchivo(cv);
        var destino = cv.Basica.Email;

        var enviado = await _correo.EnviarAsync(
            destino,
            "Tu hoja de vida en PDF",
            PlantillasCorreo.CurriculumGenerado(cv.Basica.NombreCompleto),
            new[] { new AdjuntoCorreo(nombreArchivo, "application/pdf", bytes) },
            ct);

        TempData[enviado ? "Exito" : "Error"] = enviado
            ? $"Enviamos tu hoja de vida a {destino}."
            : "No pudimos enviar el correo. Revisa la configuración de Graph e intenta de nuevo.";

        return RedirectToAction(nameof(Index));
    }

    // ---------- Plantillas ----------

    [HttpGet]
    public async Task<IActionResult> Plantillas(CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil)
        {
            TempData["Error"] = "Completa tus datos personales para poder ver las plantillas.";
            return RedirectToAction(nameof(Perfil));
        }

        return View(new PlantillasViewModel { Seleccionada = _plantillas.Obtener(User) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Plantillas(PlantillaCv plantilla)
    {
        if (!Enum.IsDefined(plantilla))
        {
            TempData["Error"] = "Esa plantilla no existe.";
            return RedirectToAction(nameof(Plantillas));
        }

        await _plantillas.GuardarAsync(User, plantilla);

        TempData["Exito"] = $"Tu hoja de vida usará la plantilla {Models.Plantillas.Info(plantilla).Nombre}.";
        return RedirectToAction(nameof(Plantillas));
    }

    /// <summary>Devuelve el PDF en linea para mostrarlo en el iframe del selector.</summary>
    [HttpGet]
    public async Task<IActionResult> PrevisualizarPdf(PlantillaCv? plantilla, CancellationToken ct)
    {
        var cv = await _curriculum.ObtenerCurriculumAsync(UserId, ct);
        if (!cv.TienePerfil) return NotFound();

        var elegida = plantilla is not null && Enum.IsDefined(plantilla.Value)
            ? plantilla.Value
            : _plantillas.Obtener(User);

        var bytes = _pdf.Generar(cv, _fotos.LeerBytes(cv.Basica!.UrlImagen), elegida);

        Response.Headers.ContentDisposition = "inline; filename=vista-previa.pdf";
        return File(bytes, "application/pdf");
    }

    // ---------- Apoyo ----------

    /// <summary>Vuelve al panel dejando abierto el acordeon de la experiencia editada.</summary>
    private IActionResult AlPanel(int laboralId) =>
        Redirect(Url.Action(nameof(Index), "Curriculum")! + $"#exp-{laboralId}");

    private async Task NotificarAsync(string destino, string seccion, string nombre)
    {
        try
        {
            await _correo.EnviarAsync(destino, "Actualizamos tu hoja de vida",
                PlantillasCorreo.PerfilActualizado(nombre, seccion));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo notificar la actualización de {Seccion}", seccion);
        }
    }

    private async Task CargarCatalogosPerfilAsync(PerfilViewModel vm, CancellationToken ct)
    {
        vm.Profesiones = await _catalogos.ParametrosAsync(GrupoParametro.Titulacion, vm.ProfesionId, ct);
        vm.Departamentos = await _catalogos.DepartamentosAsync(vm.DepartamentoId, ct);
        vm.Ciudades = await _catalogos.MunicipiosAsync(vm.DepartamentoId, vm.CiudadId, ct);
    }

    private async Task CargarCatalogosExperienciaAsync(ExperienciaViewModel vm, CancellationToken ct)
    {
        vm.Cargos = await _catalogos.ParametrosAsync(GrupoParametro.Cargo, vm.CargoId, ct);
        vm.Areas = await _catalogos.ParametrosAsync(GrupoParametro.AreaOcupacional, vm.AreaId, ct);
        vm.Estados = await _catalogos.ParametrosAsync(GrupoParametro.EstadoLaboral, vm.EstadoId, ct);
    }

    private async Task CargarCatalogosFormacionAsync(FormacionViewModel vm, CancellationToken ct)
    {
        vm.Tipos = await _catalogos.ParametrosAsync(GrupoParametro.TipoFormacion, vm.TipoFormacionId, ct);
        vm.Areas = await _catalogos.ParametrosAsync(GrupoParametro.AreaOcupacional, vm.AreaFormacionId, ct);
        vm.Estados = await _catalogos.ParametrosAsync(GrupoParametro.EstadoFormacion, vm.EstadoId, ct);
    }
}
