using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GanicaApi.Models;
using System.Security.Claims;
using GanicaApi.Data;

public class AsignarEntregaRequest
{
    public long RepartidorId { get; set; }
}

namespace GanicaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Descomentar si usás autenticación JWT en la API
    public class EntregasController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Cambiá por el nombre de tu DbContext si es diferente

        public EntregasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/entregas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Entrega>>> GetEntregas()
        {
            return await _context.Entregas.ToListAsync();
        }

        // PATCH: api/entregas/{id}/asignar
        [HttpPatch("{id}/asignar")]
public async Task<IActionResult> AsignarEntrega(
    long id,
    [FromBody] AsignarEntregaRequest request)
{
    var entrega = await _context.Entregas.FindAsync(id);

    if (entrega == null)
        return NotFound(new { mensaje = "La entrega no existe." });

    if (entrega.Estado != EstadoEntrega.Pendiente)
    {
        return BadRequest(new
        {
            mensaje = "La entrega ya no está pendiente."
        });
    }

    // Buscamos al usuario que se quiere asignar
    var recolector = await _context.Usuarios
        .Include(u => u.Rol)
        .FirstOrDefaultAsync(u =>
            u.Id == request.RepartidorId &&
            u.Activo &&
            u.Rol != null &&
            u.Rol.Codigo == "recolector");

    if (recolector == null)
    {
        return BadRequest(new
        {
            mensaje = "El usuario seleccionado no existe o no es un recolector."
        });
    }

    // Asignamos la entrega
    entrega.RepartidorId = recolector.Id;
    entrega.Estado = EstadoEntrega.Asignada;
    entrega.AsignadaEn = DateTimeOffset.UtcNow;

    // Creamos la notificación para ESE recolector
    var notificacion = new Notificacion
    {
        UsuarioId = recolector.Id,
        Titulo = "Nueva entrega asignada",
        Mensaje = $"Tenés una nueva tarea de recolección en {entrega.DireccionLinea}.",
        Tipo = TipoNotificacion.Informacion,
        LeidaEn = null,
        CreadoEn = DateTimeOffset.UtcNow
    };

    _context.Notificaciones.Add(notificacion);

    await _context.SaveChangesAsync();

    return Ok(entrega);
}

        // PATCH: api/entregas/{id}/estado
        [HttpPatch("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(long id, [FromBody] EstadoEntrega nuevoEstado)
        {
            var entrega = await _context.Entregas.FindAsync(id);
            if (entrega == null) return NotFound();

            // Lógica de transición de estados válida
            entrega.Estado = nuevoEstado;
            if (nuevoEstado == EstadoEntrega.Entregada)
            {
                entrega.EntregadaEn = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(entrega);
        }
    }
}