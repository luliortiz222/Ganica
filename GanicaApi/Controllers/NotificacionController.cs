using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GanicaApi.Models;
using GanicaApi.Data;

namespace GanicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/notificaciones/usuario/{usuarioId}
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<Notificacion>>> GetNotificaciones(long usuarioId)
        {
            return await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId)
                .OrderByDescending(n => n.CreadoEn)
                .ToListAsync();
        }

        // PATCH: api/notificaciones/{id}/leer
        [HttpPatch("{id}/leer")]
        public async Task<IActionResult> MarcarComoLeida(long id)
        {
            var notificacion = await _context.Notificaciones.FindAsync(id);
            if (notificacion == null) return NotFound();

            notificacion.LeidaEn = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(notificacion);
        }
    }
}