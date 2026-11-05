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

    public DbSet<Entrega> Entregas { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }

    public DbSet<HistorialEntrega> HistorialEntregas { get; set; } = null!;

    public DbSet<HistorialNotificacion> HistorialNotificaciones { get; set; }

    public DbSet<HistorialSolicitud> HistorialSolicitudes { get; set; }

    public DbSet<PuntoRetiro> PuntoRetiro { get; set; } = null!;

    // Nueva tabla para registrar celulares que reciben notificaciones push.
    public DbSet<DispositivoPush> DispositivosPush { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, Nombre = "Administrador", Codigo = "administrador", Activo = true },
            new Rol { Id = 2, Nombre = "Recolector", Codigo = "recolector", Activo = true },
            new Rol { Id = 3, Nombre = "Vecino", Codigo = "vecino", Activo = true }
        );

        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Email = "admin@ganica.gob.ar",
                PasswordHash = "$2a$11$qX7vHwK7hJgY5U7jJg8i2e1u5O0e4J8s7G3f2E1d0C9b8A7m6L5k4",
                RolId = 1,
                Activo = true
            }
        );

        modelBuilder.Entity<HistorialEntrega>()
            .HasOne(h => h.Entrega)
            .WithMany()
            .HasForeignKey(h => h.EntregaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HistorialEntrega>()
            .HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistorialNotificacion>()
            .HasOne(h => h.Notificacion)
            .WithMany()
            .HasForeignKey(h => h.NotificacionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HistorialNotificacion>()
            .HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistorialSolicitud>()
            .HasOne(h => h.Solicitud)
            .WithMany()
            .HasForeignKey(h => h.SolicitudId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HistorialSolicitud>()
            .HasOne(h => h.Usuario)
            .WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notificacion>()
            .HasOne(n => n.Solicitud)
            .WithMany()
            .HasForeignKey(n => n.SolicitudId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Solicitud>()
            .HasOne(s => s.Usuario)
            .WithMany()
            .HasForeignKey(s => s.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relación entre los dispositivos push y sus usuarios.
        modelBuilder.Entity<DispositivoPush>()
            .HasOne(d => d.Usuario)
            .WithMany()
            .HasForeignKey(d => d.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Un mismo token no puede registrarse dos veces.
        modelBuilder.Entity<DispositivoPush>()
            .HasIndex(d => d.Token)
            .IsUnique();
    }
}