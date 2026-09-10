using Microsoft.EntityFrameworkCore;
using GanicaApi.Models;

namespace GanicaApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ServicioRecoleccion> ServiciosRecoleccion { get; set; } = null!;
    public DbSet<PuntoRetiro> PuntosRetiro { get; set; } = null!;
    public DbSet<Solicitud> Solicitudes { get; set; } = null!;

    public DbSet<Rol> Roles { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, Nombre = "Administrador", Codigo = "administrador", Activo = true },
            new Rol { Id = 2, Nombre = "Recolector", Codigo = "recolector", Activo = true },
            new Rol { Id = 3, Nombre = "Vecino", Codigo = "vecino", Activo = true }
        );

        // 2. Sembrar el administrador inicial (Password: Admin123)[cite: 1]
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Email = "admin@ganica.gob.ar",
                // Hash generado con BCrypt para 'Admin123'
                PasswordHash = "$2a$11$qX7vHwK7hJgY5U7jJg8i2e1u5O0e4J8s7G3f2E1d0C9b8A7m6L5k4", 
                RolId = 1, // Administrador
                Activo = true
            }
        );
    }
}