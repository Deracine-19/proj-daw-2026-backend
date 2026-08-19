using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;

namespace proj_daw_2026_backend.Data.Entities;

public class AppDBContext : DbContext
{
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

    public DbSet<Rol> Roles { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Cancha> Canchas { get; set; }
    public DbSet<Articulo> Articulos { get; set; }
    public DbSet<Reserva> Reservas { get; set; }
    public DbSet<ReservaArticulo> ReservaArticulos { get; set; }
    public DbSet<Configuracion> Configuraciones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Rol>().ToTable("Rol");
        modelBuilder.Entity<Usuario>().ToTable("Usuario");
        modelBuilder.Entity<Usuario>().Property(u => u.Activo).HasDefaultValue(true);
        modelBuilder.Entity<Cancha>().ToTable("Cancha");
        modelBuilder.Entity<Articulo>().ToTable("Articulo");
        modelBuilder.Entity<Reserva>().ToTable("Reserva");
        modelBuilder.Entity<ReservaArticulo>().ToTable("Reserva_Articulo");
        modelBuilder.Entity<Configuracion>().ToTable("Configuracion");

        modelBuilder.Entity<Rol>().HasData(
           new Rol { Id = 1, Nombre = RolesConstantes.Cliente },
           new Rol { Id = 2, Nombre = RolesConstantes.Administrador },
           new Rol { Id = 3, Nombre = RolesConstantes.Operador }
       );

        // Fila única de configuración del negocio — mismos valores que tenía
        // HorarioNegocioConstantes (8:00–22:00), ahora editables desde el panel de admin.
        modelBuilder.Entity<Configuracion>().HasData(
            new Configuracion
            {
                Id = 1,
                NombreNegocio = "TURF",
                HoraApertura = new TimeSpan(8, 0, 0),
                HoraCierre = new TimeSpan(22, 0, 0)
            }
        );
    }
}