using System.Globalization;
using Cur.Web.Data;
using Cur.Web.Services;
using Cur.Web.Services.Pdf;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF Community: gratuita mientras la facturacion anual sea inferior a USD 1M.
QuestPDF.Settings.License = LicenseType.Community;

var cadena = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexion 'DefaultConnection'.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(cadena, sql => sql.EnableRetryOnFailure(3)));

if (builder.Environment.IsDevelopment())
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

        // La base heredada trae usuarios con EmailConfirmed = 0; exigir confirmacion
        // los dejaria fuera. El correo de confirmacion se sigue enviando al registrarse.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.LogoutPath = "/Cuenta/Logout";
    options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // En desarrollo se usa http://localhost; en produccion la cookie solo viaja por HTTPS.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddOptions<GraphMailOptions>()
    .Bind(builder.Configuration.GetSection(GraphMailOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
    GraphNotificadorCorreo.CrearCliente(sp.GetRequiredService<IOptions<GraphMailOptions>>().Value));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<INotificadorCorreo, GraphNotificadorCorreo>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<ICurriculumService, CurriculumService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPlantillaPreferencia, PlantillaPreferenciaService>();
builder.Services.AddScoped<IFotoPerfilStorage, FotoPerfilStorage>();
builder.Services.AddSingleton<ICurriculumPdfGenerator, CurriculumPdfGenerator>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Fechas y numeros en espanol de Colombia en toda la app (vistas y PDF).
var cultura = new CultureInfo("es-CO");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultura),
    SupportedCultures = new[] { cultura },
    SupportedUICultures = new[] { cultura }
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
