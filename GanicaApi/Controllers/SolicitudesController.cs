using System.Security.Claims;
using GanicaApi.Data;
using GanicaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SolicitudesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SolicitudesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool ObtenerUsuarioId(out long usuarioId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out usuarioId);
    }

    // Listar solicitudes:
    // Administrador: todas las solicitudes.
    // Recolector: solamente las solicitudes que recibió mediante notificación.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudes()
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        var consulta = _context.Solicitudes
            .AsNoTracking()
            .Include(s => s.ServicioRecoleccion)
            .AsQueryable();

        if (User.IsInRole("administrador"))
        {
            var todas = await consulta.ToListAsync();
            return Ok(todas);
        }

        if (User.IsInRole("recolector"))
        {
            var recibidas = await consulta
                .Where(s => _context.Notificaciones.Any(n =>
                    n.SolicitudId == s.Id &&
                    n.UsuarioId == usuarioId))
                .ToListAsync();

            return Ok(recibidas);
        }

        return Forbid();
    }

    // Crear solicitudes: exclusivamente el administrador.
    [HttpPost]
    [Authorize(Roles = "administrador")]
    public async Task<ActionResult<Solicitud>> PostSolicitud(
        [FromBody] Solicitud solicitud)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(solicitud.TipoResiduo))
        {
            return BadRequest(new
            {
                mensaje = "Debés indicar el tipo de residuo."
            });
        }

        if (string.IsNullOrWhiteSpace(solicitud.Direccion))
        {
            return BadRequest(new
            {
                mensaje = "Debés indicar la dirección de recolección."
            });
        }

        var servicio = await _context.ServiciosRecoleccion
            .FirstOrDefaultAsync(s => s.RequiereSolicitud);

        if (servicio == null)
        {
            return BadRequest(new
            {
                mensaje = "No existe un servicio habilitado para solicitudes."
            });
        }

        solicitud.ServicioRecoleccionId = servicio.Id;
        solicitud.FechaCreacion = DateTime.Now;
        solicitud.Estado = "Pendiente";

        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync();

        // Registrar la creación en el historial.
        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = solicitud.Id,
            UsuarioId = usuarioId,
            Accion = "Creada",
            EstadoAnterior = null,
            EstadoNuevo = solicitud.Estado,
            Fecha = DateTimeOffset.UtcNow,
            Observacion = "Solicitud de recolección creada."
        });

        // Buscar los recolectores activos.
        var recolectores = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u =>
                u.Activo &&
                u.Rol != null &&
                u.Rol.Codigo == "recolector")
            .ToListAsync();

        // Notificar a cada recolector activo.
        foreach (var recolector in recolectores)
        {
            _context.Notificaciones.Add(new Notificacion
            {
                UsuarioId = recolector.Id,
                SolicitudId = solicitud.Id,
                Titulo = "Nueva solicitud de recolección",
                Mensaje =
                    $"Tenés una nueva tarea: {solicitud.TipoResiduo}. " +
                    $"Dirección: {solicitud.Direccion}. " +
                    $"Observaciones: {solicitud.Observaciones}",
                Tipo = TipoNotificacion.Entrega,
                CreadoEn = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return Ok(solicitud);
    }

    // Consultar el historial: exclusivamente el administrador.
    [HttpGet("historial")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> GetHistorialSolicitudes()
    {
        var historial = await _context.HistorialSolicitudes
            .AsNoTracking()
            .Include(h => h.Solicitud)
            .Include(h => h.Usuario)
            .OrderByDescending(h => h.Fecha)
            .Select(h => new
            {
                h.Id,
                h.SolicitudId,
                h.UsuarioId,
                UsuarioEmail = h.Usuario != null
                    ? h.Usuario.Email
                    : null,
                h.Accion,
                h.EstadoAnterior,
                h.EstadoNuevo,
                h.Fecha,
                h.Observacion,
                TipoResiduo = h.Solicitud != null
                    ? h.Solicitud.TipoResiduo
                    : null,
                Direccion = h.Solicitud != null
                    ? h.Solicitud.Direccion
                    : null
            })
            .ToListAsync();

        return Ok(historial);
    }

    // Actualizar el estado de una solicitud.
    // Solo administrador o recolector.
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "administrador,recolector")]
    public async Task<IActionResult> ActualizarEstado(
        int id,
        [FromBody] ActualizarEstadoSolicitudRequest request)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        if (request == null ||
            string.IsNullOrWhiteSpace(request.Estado))
        {
            return BadRequest(new
            {
                mensaje = "Debés indicar el nuevo estado."
            });
        }

        var estadosPermitidos = new[]
        {
            "Pendiente",
            "Observada",
            "En camino",
            "Finalizada"
        };

        var estadoNuevo = estadosPermitidos.FirstOrDefault(
            e => e.Equals(
                request.Estado.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (estadoNuevo == null)
        {
            return BadRequest(new
            {
                mensaje = "El estado indicado no es válido.",
                estadosPermitidos
            });
        }

        var solicitud = await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
        {
            return NotFound(new
            {
                mensaje = "La solicitud no existe."
            });
        }

        // El recolector solo puede modificar solicitudes
        // que le fueron notificadas.
        if (User.IsInRole("recolector"))
        {
            var tieneAsignacion = await _context.Notificaciones
                .AnyAsync(n =>
                    n.SolicitudId == id &&
                    n.UsuarioId == usuarioId);

            if (!tieneAsignacion)
            {
                return Forbid();
            }
        }

        var estadoAnterior = solicitud.Estado;

        if (string.Equals(
            estadoAnterior,
            estadoNuevo,
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                mensaje = "La solicitud ya tiene ese estado."
            });
        }

        solicitud.Estado = estadoNuevo;

        // Registrar el cambio en el historial.
        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = solicitud.Id,
            UsuarioId = usuarioId,
            Accion = "EstadoActualizado",
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            Fecha = DateTimeOffset.UtcNow,
            Observacion = string.IsNullOrWhiteSpace(request.Observacion)
                ? $"Estado cambiado de {estadoAnterior} a {estadoNuevo}."
                : request.Observacion.Trim()
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Estado actualizado correctamente.",
            solicitudId = solicitud.Id,
            estadoAnterior,
            estadoNuevo
        });
    }
}

// Datos recibidos para actualizar el estado.
public class ActualizarEstadoSolicitudRequest
{
    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }
}
