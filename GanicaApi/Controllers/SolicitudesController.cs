using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GanicaApi.Data;
using GanicaApi.Models;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SolicitudesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudes()
    {
        return await _context.Solicitudes
            .Include(s => s.ServicioRecoleccion)
            .ToListAsync();
    }


[HttpPost]
public async Task<ActionResult<Solicitud>> PostSolicitud(Solicitud solicitud)
{
    // Validamos el tipo de residuo recibido desde el formulario.
    if (string.IsNullOrWhiteSpace(solicitud.TipoResiduo))
    {
        return BadRequest(new
        {
            mensaje = "Debés indicar el tipo de residuo."
        });
    }

    // Buscamos el servicio habilitado para solicitudes.
    var servicio = await _context.ServiciosRecoleccion
        .FirstOrDefaultAsync(s => s.RequiereSolicitud);

    if (servicio == null)
    {
        return BadRequest(new
        {
            mensaje = "No existe un servicio habilitado para solicitudes."
        });
    }

    // Asociamos la solicitud al servicio habilitado.
    solicitud.ServicioRecoleccionId = servicio.Id;

    solicitud.FechaCreacion = DateTime.Now;
    solicitud.Estado = "Pendiente";

    _context.Solicitudes.Add(solicitud);
    await _context.SaveChangesAsync();

    // Buscamos todos los usuarios que tengan rol recolector.
    var recolectores = await _context.Usuarios
        .Include(u => u.Rol)
        .Where(u =>
            u.Activo &&
            u.Rol != null &&
            u.Rol.Codigo == "recolector")
        .ToListAsync();

    // Notificamos el tipo de residuo elegido en el formulario.
    foreach (var recolector in recolectores)
    {
        var notificacion = new Notificacion
        {
            UsuarioId = recolector.Id,
            Titulo = "Nueva solicitud de recolección",
            Mensaje =
                $"Tenés una nueva tarea: {solicitud.TipoResiduo}. " +
                $"Dirección: {solicitud.Direccion}. " +
                $"Observaciones: {solicitud.Observaciones}",
            Tipo = TipoNotificacion.Entrega,
            CreadoEn = DateTimeOffset.UtcNow
        };

        _context.Notificaciones.Add(notificacion);
    }

    await _context.SaveChangesAsync();

    return Ok(solicitud);
}

}