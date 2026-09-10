using Microsoft.EntityFrameworkCore;
using GanicaApi.Models;

namespace GanicaApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ServicioRecoleccion> ServiciosRecoleccion { get; set; } = null!;
    public DbSet<PuntoRetiro> PuntosRetiro { get; set; } = null!;
    public DbSet<Solicitud> Solicitudes { get; set; } = null!;
}