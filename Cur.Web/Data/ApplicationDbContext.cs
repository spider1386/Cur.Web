using Cur.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cur.Web.Data;

/// <summary>
/// Contexto sobre la base BD_Curriculums ya existente. Las tablas de negocio usan
/// nombres abreviados heredados, por eso todo el mapeo es explicito y NO se generan
/// migraciones para ellas (Identity ya fue migrado en 20260430220058_InitialIdentity).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<InformacionBasica> InformacionBasica => Set<InformacionBasica>();
    public DbSet<InformacionLaboral> InformacionLaboral => Set<InformacionLaboral>();
    public DbSet<FormacionAcademica> FormacionAcademica => Set<FormacionAcademica>();
    public DbSet<LogroLaboral> LogrosLaborales => Set<LogroLaboral>();
    public DbSet<Competencia> Competencias => Set<Competencia>();
    public DbSet<CartaPresentacion> CartasPresentacion => Set<CartaPresentacion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Parametro>(e =>
        {
            e.ToTable("Parametros");
            e.HasKey(p => p.ParametroId);
            e.Property(p => p.ParametroId).HasColumnName("PrmtroID");
            e.Property(p => p.Tipo).HasColumnName("Tpo").HasMaxLength(30).IsRequired();
            e.Property(p => p.Descripcion).HasColumnName("Prmtro").HasMaxLength(200).IsRequired();
            e.Property(p => p.Codigo).HasColumnName("Cdgo");
        });

        builder.Entity<Departamento>(e =>
        {
            e.ToTable("Departamentos");
            e.HasKey(d => d.DepartamentoId);
            e.Property(d => d.DepartamentoId).HasColumnName("DprtmntoID").ValueGeneratedNever();
            e.Property(d => d.Nombre).HasColumnName("Dprtmntos").HasMaxLength(250).IsRequired();
        });

        builder.Entity<Municipio>(e =>
        {
            e.ToTable("Municipios");
            e.HasKey(m => m.MunicipioId);
            e.Property(m => m.MunicipioId).HasColumnName("CdadID");
            e.Property(m => m.DepartamentoId).HasColumnName("DprtmntoID");
            e.Property(m => m.Codigo).HasColumnName("Cdgo").HasMaxLength(50).IsRequired();
            e.Property(m => m.Nombre).HasColumnName("Mncpio").HasMaxLength(250).IsRequired();
            e.HasOne(m => m.Departamento).WithMany(d => d.Municipios)
                .HasForeignKey(m => m.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InformacionBasica>(e =>
        {
            e.ToTable("Informacion_Basica");
            e.HasKey(b => b.BasicaId);
            e.Property(b => b.BasicaId).HasColumnName("BscaID");
            e.Property(b => b.UrlImagen).HasColumnName("UrlImgen");
            e.Property(b => b.Nombres).HasColumnName("Nmbres").HasMaxLength(50);
            e.Property(b => b.Apellidos).HasColumnName("Aplldos").HasMaxLength(50).IsRequired();
            e.Property(b => b.Documento).HasColumnName("Dcmnto").HasColumnType("decimal(18,0)");
            e.Property(b => b.Email).HasColumnName("Email").HasMaxLength(50).IsRequired();
            e.Property(b => b.TelefonoFijo).HasColumnName("Tlfno_Fjo").HasMaxLength(30).IsRequired();
            e.Property(b => b.TelefonoMovil).HasColumnName("Tlfno_Mvil").HasMaxLength(30).IsRequired();
            e.Property(b => b.PerfilProfesional).HasColumnName("Prfil_Prfsnal").IsRequired();
            e.Property(b => b.ProfesionId).HasColumnName("PrfsionID");
            e.Property(b => b.DepartamentoId).HasColumnName("DprtmntoID");
            e.Property(b => b.CiudadId).HasColumnName("CdadID");
            e.Property(b => b.UserId).HasColumnName("UserId").HasMaxLength(450);

            e.HasOne(b => b.Profesion).WithMany()
                .HasForeignKey(b => b.ProfesionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Departamento).WithMany()
                .HasForeignKey(b => b.DepartamentoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Ciudad).WithMany()
                .HasForeignKey(b => b.CiudadId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(b => b.UserId);
            e.Ignore(b => b.NombreCompleto);
        });

        builder.Entity<InformacionLaboral>(e =>
        {
            e.ToTable("Informacion_Laboral");
            e.HasKey(l => l.LaboralId);
            e.Property(l => l.LaboralId).HasColumnName("LbralID");
            e.Property(l => l.BasicaId).HasColumnName("BscaID");
            e.Property(l => l.CargoId).HasColumnName("CrgoID");
            e.Property(l => l.FechaInicio).HasColumnName("Fcha_Incio").HasColumnType("date");
            e.Property(l => l.FechaRetiro).HasColumnName("Fcha_Rtro").HasColumnType("date");
            e.Property(l => l.TiempoLaborado).HasColumnName("Tmpo_Lbrdo");
            e.Property(l => l.EstadoId).HasColumnName("EstdoID");
            e.Property(l => l.Empresa).HasColumnName("Emprsa").HasMaxLength(250).IsRequired();
            e.Property(l => l.AreaId).HasColumnName("AreaID");
            e.Property(l => l.JefeInmediato).HasColumnName("Jfe_Inmdto").HasMaxLength(50).IsRequired();
            e.Property(l => l.Contacto).HasColumnName("Cntcto").HasMaxLength(30).IsRequired();

            e.HasOne(l => l.Basica).WithMany(b => b.Experiencia)
                .HasForeignKey(l => l.BasicaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Cargo).WithMany().HasForeignKey(l => l.CargoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Area).WithMany().HasForeignKey(l => l.AreaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Estado).WithMany().HasForeignKey(l => l.EstadoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FormacionAcademica>(e =>
        {
            e.ToTable("Formacion_Academica");
            e.HasKey(f => f.FormacionId);
            e.Property(f => f.FormacionId).HasColumnName("FrmcionID");
            e.Property(f => f.BasicaId).HasColumnName("BscaID");
            e.Property(f => f.TipoFormacionId).HasColumnName("Tpo_FrmcionID");
            e.Property(f => f.AreaFormacionId).HasColumnName("Area_FrmcionID");
            e.Property(f => f.Intensidad).HasColumnName("Intnsdad").HasMaxLength(50);
            e.Property(f => f.Institucion).HasColumnName("Insttcion").HasMaxLength(150).IsRequired();
            e.Property(f => f.FechaInicio).HasColumnName("Fcha_Incio").HasColumnType("date");
            e.Property(f => f.FechaFinalizacion).HasColumnName("Fcha_Finlzcion").HasColumnType("date");
            e.Property(f => f.EstadoId).HasColumnName("EstdoID");
            e.Property(f => f.TituloOtorgado).HasColumnName("Ttlo_Otrgdo").HasMaxLength(150).IsRequired();

            e.HasOne(f => f.Basica).WithMany(b => b.Formacion)
                .HasForeignKey(f => f.BasicaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.TipoFormacion).WithMany().HasForeignKey(f => f.TipoFormacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.AreaFormacion).WithMany().HasForeignKey(f => f.AreaFormacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Estado).WithMany().HasForeignKey(f => f.EstadoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LogroLaboral>(e =>
        {
            e.ToTable("Logros_Laborales");
            e.HasKey(l => l.LogroId);
            e.Property(l => l.LogroId).HasColumnName("LgrosID");
            e.Property(l => l.LaboralId).HasColumnName("LbralID");
            e.Property(l => l.TipoId).HasColumnName("TipoID");
            e.Property(l => l.Logro).HasColumnName("Lgro").HasMaxLength(50).IsRequired();
            e.Property(l => l.Descripcion).HasColumnName("Dscrpcion").HasMaxLength(150).IsRequired();

            e.HasOne(l => l.Laboral).WithMany(x => x.Logros)
                .HasForeignKey(l => l.LaboralId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Tipo).WithMany().HasForeignKey(l => l.TipoId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Competencia>(e =>
        {
            e.ToTable("Competencias");
            e.HasKey(c => c.CompetenciaId);
            // La columna no es IDENTITY en la base heredada: el id lo asigna CurriculumService.
            e.Property(c => c.CompetenciaId).HasColumnName("CmptnciaID").ValueGeneratedNever();
            e.Property(c => c.LaboralId).HasColumnName("LbralID");
            e.Property(c => c.TipoCompetenciaId).HasColumnName("Tpo_cmptnciaID");
            e.Property(c => c.Descripcion).HasColumnName("Dscrpcion_Cmptncia").HasMaxLength(250).IsRequired();
            e.Property(c => c.Medicion).HasColumnName("Medicion").HasMaxLength(250).IsRequired();

            e.HasOne(c => c.Laboral).WithMany(x => x.Competencias)
                .HasForeignKey(c => c.LaboralId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.TipoCompetencia).WithMany().HasForeignKey(c => c.TipoCompetenciaId).OnDelete(DeleteBehavior.Restrict);
        });

        // Tabla nueva (no heredada): nombres completos y una carta por usuario.
        builder.Entity<CartaPresentacion>(e =>
        {
            e.ToTable("Carta_Presentacion");
            e.HasKey(c => c.CartaId);
            e.Property(c => c.CartaId).HasColumnName("CartaID");
            e.Property(c => c.UserId).HasColumnName("UserId").HasMaxLength(450).IsRequired();
            e.Property(c => c.CargoObjetivo).HasColumnName("CargoObjetivo").HasMaxLength(150).IsRequired();
            e.Property(c => c.Empresa).HasColumnName("Empresa").HasMaxLength(250);
            e.Property(c => c.Tono).HasColumnName("Tono").HasConversion<int>();
            e.Property(c => c.Texto).HasColumnName("Texto").IsRequired();
            e.Property(c => c.IncluirEnHojaDeVida).HasColumnName("IncluirEnHojaDeVida");
            e.Property(c => c.ActualizadaEn).HasColumnName("ActualizadaEn").HasColumnType("datetime2");
            e.HasIndex(c => c.UserId).IsUnique();
        });
    }
}
