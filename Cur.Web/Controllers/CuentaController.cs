using System.Text;
using Cur.Web.Models.ViewModels;
using Cur.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Cur.Web.Controllers;

[AllowAnonymous]
public class CuentaController : Controller
{
    private const string RolPorDefecto = "Usuario";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly INotificadorCorreo _correo;
    private readonly ILogger<CuentaController> _log;

    public CuentaController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        INotificadorCorreo correo,
        ILogger<CuentaController> log)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _correo = correo;
        _log = log;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var resultado = await _signInManager.PasswordSignInAsync(
            vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);

        if (resultado.Succeeded)
            return RedirigirSeguro(vm.ReturnUrl);

        if (resultado.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Cuenta bloqueada temporalmente por intentos fallidos. Intenta en unos minutos.");
            return View(vm);
        }

        // Mensaje generico: no revelamos si el correo existe.
        ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
        return View(vm);
    }

    [HttpGet]
    public IActionResult Registro() => View(new RegistroViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = new IdentityUser
        {
            UserName = vm.Email,
            Email = vm.Email
        };

        var resultado = await _userManager.CreateAsync(usuario, vm.Password);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
                ModelState.AddModelError(string.Empty, Traducir(error));
            return View(vm);
        }

        await _userManager.AddToRoleAsync(usuario, RolPorDefecto);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
        var tokenCodificado = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var url = Url.Action(nameof(ConfirmarCorreo), "Cuenta",
            new { userId = usuario.Id, token = tokenCodificado }, Request.Scheme)!;

        await _correo.EnviarAsync(vm.Email, "Confirma tu correo", PlantillasCorreo.Bienvenida(vm.Email, url));

        await _signInManager.SignInAsync(usuario, isPersistent: false);
        TempData["Exito"] = "Cuenta creada. Te enviamos un correo para confirmar tu dirección.";
        return RedirectToAction("Index", "Curriculum");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmarCorreo(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return View("ConfirmacionFallida");

        var usuario = await _userManager.FindByIdAsync(userId);
        if (usuario is null) return View("ConfirmacionFallida");

        string tokenPlano;
        try
        {
            tokenPlano = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return View("ConfirmacionFallida");
        }

        var resultado = await _userManager.ConfirmEmailAsync(usuario, tokenPlano);
        return View(resultado.Succeeded ? "ConfirmacionExitosa" : "ConfirmacionFallida");
    }

    [HttpGet]
    public IActionResult OlvideClave() => View(new OlvideClaveViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OlvideClave(OlvideClaveViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await _userManager.FindByEmailAsync(vm.Email);
        if (usuario is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var tokenCodificado = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = Url.Action(nameof(RestablecerClave), "Cuenta",
                new { email = vm.Email, token = tokenCodificado }, Request.Scheme)!;

            await _correo.EnviarAsync(vm.Email, "Restablece tu contraseña", PlantillasCorreo.RestablecerClave(url));
        }
        else
        {
            _log.LogInformation("Solicitud de restablecimiento para un correo inexistente.");
        }

        // Respuesta identica exista o no la cuenta, para no filtrar correos registrados.
        TempData["Exito"] = "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult RestablecerClave(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        return View(new RestablecerClaveViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerClave(RestablecerClaveViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var usuario = await _userManager.FindByEmailAsync(vm.Email);
        if (usuario is null)
        {
            TempData["Exito"] = "Contraseña actualizada. Ya puedes iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        string tokenPlano;
        try
        {
            tokenPlano = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(vm.Token));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "El enlace no es válido o ya caducó.");
            return View(vm);
        }

        var resultado = await _userManager.ResetPasswordAsync(usuario, tokenPlano, vm.Password);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
                ModelState.AddModelError(string.Empty, Traducir(error));
            return View(vm);
        }

        TempData["Exito"] = "Contraseña actualizada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccesoDenegado() => View();

    private IActionResult RedirigirSeguro(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Curriculum");

    private static string Traducir(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Ya existe una cuenta con ese correo.",
        "PasswordTooShort" => "La contraseña es demasiado corta.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
        "PasswordRequiresLower" => "La contraseña debe incluir al menos una minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe incluir al menos una mayúscula.",
        "InvalidEmail" => "El correo no tiene un formato válido.",
        _ => error.Description
    };
}
