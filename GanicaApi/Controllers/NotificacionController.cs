using System.Security.Claims;
using GanicaApi.Data;
using GanicaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GanicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool ObtenerUsuarioId(out long usuarioId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out usuarioId);
        }

        private bool EsAdministrador()
        {
            return User.IsInRole("administrador");
        }

        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<Notificacion>>> GetNotificaciones(
            long usuarioId)
        {
            if (!ObtenerUsuarioId(out var usuarioAutenticadoId))
                return Unauthorized();

            if (!EsAdministrador() && usuarioId != usuarioAutenticadoId)
                return Forbid();

            var notificaciones = await _context.Notificaciones
                .AsNoTracking()
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.CreadoEn)
                .ToListAsync();

            return Ok(notificaciones);
        }

        [HttpPatch("{id}/leer")]
        public async Task<IActionResult> MarcarComoLeida(long id)
        {
            if (!ObtenerUsuarioId(out var usuarioId))
                return Unauthorized();

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notificacion == null)
            {
                return NotFound(new
                {
                    mensaje = "La notificación no existe."
                });
            }

            if (!EsAdministrador() && notificacion.UsuarioId != usuarioId)
                return Forbid();

            // Si ya fue leída, no duplicar los registros del historial.
            if (notificacion.LeidaEn != null)
                return Ok(notificacion);

            var ahora = DateTimeOffset.UtcNow;
            notificacion.LeidaEn = ahora;

            // Registrar la lectura de la notificación.
            _context.HistorialNotificaciones.Add(
                new HistorialNotificacion
                {
                    NotificacionId = notificacion.Id,
                    UsuarioId = usuarioId,
                    Accion = "Leida",
                    Fecha = ahora
                });

            // Si la notificación corresponde a una solicitud,
            // registrar también el evento en el historial de esa solicitud.
            if (notificacion.SolicitudId.HasValue)
            {
                _context.HistorialSolicitudes.Add(
                    new HistorialSolicitud
                    {
                        SolicitudId = notificacion.SolicitudId.Value,
                        UsuarioId = usuarioId,
                        Accion = "NotificacionLeida",
                        EstadoAnterior = null,
                        EstadoNuevo = null,
                        Fecha = ahora,
                        Observacion =
                            $"El usuario leyó la notificación {notificacion.Id}."
                    });
            }

            await _context.SaveChangesAsync();

            return Ok(notificacion);
        }
    }
}