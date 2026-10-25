using System.Security.Claims;
using GanicaApi.Data;
using GanicaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GanicaApi.Controllers;

public class AsignarEntregaRequest
{
    public long RepartidorId { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EntregasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EntregasController(ApplicationDbContext context)
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

    // GET: api/Entregas
    // El administrador ve todas; el recolector, las asignadas a él.
    [HttpGet]
    [Authorize(Roles = "administrador,recolector")]
    public async Task<ActionResult<IEnumerable<Entrega>>> GetEntregas()
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        var consulta = _context.Entregas.AsNoTracking();

        if (!EsAdministrador())
        {
            consulta = consulta.Where(e => e.RepartidorId == usuarioId);
        }

        return Ok(await consulta.ToListAsync());
    }

    // PATCH: api/Entregas/{id}/asignar
    [HttpPatch("{id}/asignar")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> AsignarEntrega(
        long id,
        [FromBody] AsignarEntregaRequest request)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        var entrega = await _context.Entregas.FindAsync(id);

        if (entrega == null)
        {
            return NotFound(new
            {
                mensaje = "La entrega no existe."
            });
        }

        if (entrega.Estado != EstadoEntrega.Pendiente)
        {
            return BadRequest(new
            {
                mensaje = "Solo se pueden asignar entregas pendientes."
            });
        }

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
                mensaje = "El usuario seleccionado no existe o no es un recolector activo."
            });
        }

        var estadoAnterior = entrega.Estado;
        var ahora = DateTimeOffset.UtcNow;

        entrega.RepartidorId = recolector.Id;
        entrega.Estado = EstadoEntrega.Asignada;
        entrega.AsignadaEn = ahora;

        _context.HistorialEntregas.Add(new HistorialEntrega
        {
            EntregaId = entrega.Id,
            UsuarioId = usuarioId,
            EstadoAnterior = estadoAnterior.ToString(),
            EstadoNuevo = entrega.Estado.ToString(),
            Fecha = ahora,
            Observacion = $"Entrega asignada al recolector {recolector.Id}."
        });

        _context.Notificaciones.Add(new Notificacion
        {
            UsuarioId = recolector.Id,
            Titulo = "Nueva entrega asignada",
            Mensaje = $"Tenés una nueva tarea de recolección en {entrega.DireccionLinea}.",
            Tipo = TipoNotificacion.Informacion,
            CreadoEn = ahora
        });

        // Entrega, historial y notificación se guardan juntos.
        await _context.SaveChangesAsync();

        return Ok(entrega);
    }

    // PATCH: api/Entregas/{id}/estado
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "administrador,recolector")]
    public async Task<IActionResult> CambiarEstado(
        long id,
        [FromBody] EstadoEntrega nuevoEstado)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        if (!Enum.IsDefined(typeof(EstadoEntrega), nuevoEstado))
        {
            return BadRequest(new
            {
                mensaje = "El estado indicado no es válido."
            });
        }

        var entrega = await _context.Entregas.FindAsync(id);

        if (entrega == null)
        {
            return NotFound(new
            {
                mensaje = "La entrega no existe."
            });
        }

        // Un recolector solo puede modificar sus propias entregas.
        if (!EsAdministrador() && entrega.RepartidorId != usuarioId)
        {
            return Forbid();
        }

        var estadoAnterior = entrega.Estado;

        if (estadoAnterior == nuevoEstado)
        {
            return BadRequest(new
            {
                mensaje = "La entrega ya tiene ese estado."
            });
        }

        // Las transiciones válidas para el flujo de recolección.
        var transicionValida = estadoAnterior switch
        {
            EstadoEntrega.Asignada =>
                nuevoEstado == EstadoEntrega.EnCamino ||
                nuevoEstado == EstadoEntrega.Cancelada,

            EstadoEntrega.EnCamino =>
                nuevoEstado == EstadoEntrega.Entregada ||
                nuevoEstado == EstadoEntrega.Cancelada,

            EstadoEntrega.Pendiente =>
                EsAdministrador() &&
                nuevoEstado == EstadoEntrega.Cancelada,

            _ => false
        };

        if (!transicionValida)
        {
            return BadRequest(new
            {
                mensaje = $"No se permite cambiar de {estadoAnterior} a {nuevoEstado}."
            });
        }

        var ahora = DateTimeOffset.UtcNow;

        entrega.Estado = nuevoEstado;

        if (nuevoEstado == EstadoEntrega.Entregada)
        {
            entrega.EntregadaEn = ahora;
        }

        _context.HistorialEntregas.Add(new HistorialEntrega
        {
            EntregaId = entrega.Id,
            UsuarioId = usuarioId,
            EstadoAnterior = estadoAnterior.ToString(),
            EstadoNuevo = nuevoEstado.ToString(),
            Fecha = ahora,
            Observacion = null
        });

        // EF Core guarda los cambios pendientes en una transacción
        // cuando el proveedor de base de datos lo permite.
        await _context.SaveChangesAsync();

        return Ok(entrega);
    }

    // GET: api/Entregas/{id}/historial
    [HttpGet("{id}/historial")]
    [Authorize(Roles = "administrador,recolector")]
    public async Task<IActionResult> ObtenerHistorial(long id)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        var entrega = await _context.Entregas
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entrega == null)
        {
            return NotFound(new
            {
                mensaje = "La entrega no existe."
            });
        }

        if (!EsAdministrador() && entrega.RepartidorId != usuarioId)
        {
            return Forbid();
        }

        var historial = await _context.HistorialEntregas
            .AsNoTracking()
            .Where(h => h.EntregaId == id)
            .OrderByDescending(h => h.Fecha)
            .Select(h => new
            {
                h.Id,
                h.EntregaId,
                h.UsuarioId,
                h.EstadoAnterior,
                h.EstadoNuevo,
                h.Fecha,
                h.Observacion
            })
            .ToListAsync();

        return Ok(historial);
    }
}
