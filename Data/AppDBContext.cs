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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Rol>().ToTable("Rol");
        modelBuilder.Entity<Usuario>().ToTable("Usuario");
        modelBuilder.Entity<Cancha>().ToTable("Cancha");
        modelBuilder.Entity<Articulo>().ToTable("Articulo");
        modelBuilder.Entity<Reserva>().ToTable("Reserva");
        modelBuilder.Entity<ReservaArticulo>().ToTable("Reserva_Articulo");
    }
}