using System.Security.Claims;
using GanicaApi.Data;
using GanicaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

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
    // Administrador: todas.
    // Recolector: las recibidas mediante notificación.
    // Vecino: solamente las que creó.
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
            return Ok(await consulta.ToListAsync());
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

        if (User.IsInRole("vecino"))
        {
            var propias = await consulta
                .Where(s => s.UsuarioId == usuarioId)
                .ToListAsync();

            return Ok(propias);
        }

        return Forbid();
    }

    // Crear solicitudes:
    // Administrador y vecino.
    [HttpPost]
    [Authorize(Roles = "administrador,vecino")]
    public async Task<ActionResult<Solicitud>> PostSolicitud(
        [FromBody] Solicitud solicitud)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        if (solicitud == null)
        {
            return BadRequest(new
            {
                mensaje = "Los datos de la solicitud son obligatorios."
            });
        }

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

        // Validar que el tipo de residuo esté permitido.
        var tiposPermitidos = new[]
        {
            "Poda y Escombros",
            "Electrodomésticos-Voluminosos",
            "Aceite Vegetal Usado"
        };

        var tipoResiduo = tiposPermitidos.FirstOrDefault(t =>
            t.Equals(
                solicitud.TipoResiduo.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (tipoResiduo == null)
        {
            return BadRequest(new
            {
                mensaje = "El tipo de residuo indicado no es válido.",
                tiposPermitidos
            });
        }

        solicitud.TipoResiduo = tipoResiduo;
        solicitud.ServicioRecoleccionId = servicio.Id;
        solicitud.FechaCreacion = DateTime.Now;
        solicitud.Estado = "Pendiente";

        // Nunca confiar en un UsuarioId enviado desde el cliente.
        // Se utiliza la identidad autenticada del token.
        solicitud.UsuarioId = usuarioId;

        // Evitar que el cliente intente asignar otra entidad de navegación.
        solicitud.Usuario = null;
        solicitud.ServicioRecoleccion = null;

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
            Observacion = User.IsInRole("vecino")
                ? "Solicitud de recolección creada por un vecino."
                : "Solicitud de recolección creada por un administrador."
        });

        // Buscar recolectores activos.
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

    // Consultar historial: exclusivamente administrador.
    [HttpGet("historial")]
[Authorize(Roles = "administrador,vecino")]
public async Task<IActionResult> GetHistorialSolicitudes()
{
    if (!ObtenerUsuarioId(out var usuarioId))
        return Unauthorized();

    var consulta = _context.HistorialSolicitudes
        .AsNoTracking()
        .AsQueryable();

    // El administrador puede consultar toda la trazabilidad.
    if (User.IsInRole("administrador"))
    {
        // No se aplica ningún filtro.
    }
    // El vecino solo puede consultar los eventos
    // de solicitudes cuyo UsuarioId sea el suyo.
    else if (User.IsInRole("vecino"))
    {
        consulta = consulta.Where(h =>
            h.Solicitud != null &&
            h.Solicitud.UsuarioId == usuarioId);
    }
    else
    {
        return Forbid();
    }

    var historial = await consulta
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


    // Guardar la firma de conformidad del vecino.
    // Solo puede firmar el vecino propietario de la solicitud.
    [HttpPost("{id}/firma")]
    [Authorize(Roles = "vecino")]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> GuardarFirma(
        int id,
        [FromForm] IFormFile? firma)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        if (firma == null || firma.Length == 0)
        {
            return BadRequest(new
            {
                mensaje = "Debés adjuntar la firma del vecino."
            });
        }

        if (firma.Length > 1_500_000)
        {
            return BadRequest(new
            {
                mensaje = "La firma no puede superar 1,5 MB."
            });
        }

        if (!string.Equals(
            firma.ContentType,
            "image/png",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                mensaje = "La firma debe enviarse en formato PNG."
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

        // Seguridad: el vecino solo puede firmar su propia solicitud.
        if (solicitud.UsuarioId != usuarioId)
        {
            return Forbid();
        }

        // La firma solo está habilitada cuando el recolector
        // cambió el estado a "En camino".
        if (solicitud.Estado != "En camino")
        {
            return BadRequest(new
            {
                mensaje =
                    "Podés firmar la conformidad únicamente cuando la solicitud esté en estado 'En camino'."
            });
        }

        if (!string.IsNullOrWhiteSpace(
            solicitud.FirmaConformidadArchivo))
        {
            return BadRequest(new
            {
                mensaje = "Esta solicitud ya tiene una firma registrada."
            });
        }

        // Validar la firma binaria real del PNG.
        byte[] encabezado = new byte[8];

        await using (var stream = firma.OpenReadStream())
        {
            int leidos = await stream.ReadAsync(encabezado.AsMemory());

            byte[] firmaPng =
            {
                137, 80, 78, 71, 13, 10, 26, 10
            };

            if (leidos != 8 ||
                !encabezado.SequenceEqual(firmaPng))
            {
                return BadRequest(new
                {
                    mensaje = "El archivo enviado no es un PNG válido."
                });
            }
        }

        // Guardar la imagen fuera de wwwroot para que no
        // quede publicada directamente como archivo estático.
        var carpeta = Path.Combine(
            Directory.GetCurrentDirectory(),
            "App_Data",
            "FirmasSolicitudes");

        Directory.CreateDirectory(carpeta);

        var nombreArchivo = $"{Guid.NewGuid():N}.png";
        var rutaArchivo = Path.Combine(carpeta, nombreArchivo);

        var estadoAnterior = solicitud.Estado;

        try
        {
            await using (var destino = new FileStream(
                rutaArchivo,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await firma.CopyToAsync(destino);
            }

            solicitud.FirmaConformidadArchivo = nombreArchivo;
            solicitud.FirmaConformidadFecha = DateTimeOffset.UtcNow;
            solicitud.Estado = "Finalizada";

            _context.HistorialSolicitudes.Add(
                new HistorialSolicitud
                {
                    SolicitudId = solicitud.Id,

                    // El evento lo registra el vecino autenticado.
                    UsuarioId = usuarioId,

                    Accion = "FirmaConformidadRegistrada",
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = "Finalizada",
                    Fecha = DateTimeOffset.UtcNow,
                    Observacion =
                        "El vecino registró su firma de conformidad. La solicitud quedó finalizada."
                });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje =
                    "Firma de conformidad registrada correctamente.",
                solicitudId = solicitud.Id,
                estado = solicitud.Estado,
                firmaRegistrada = true,
                fechaFirma = solicitud.FirmaConformidadFecha
            });
        }
        catch
        {
            // Si falla la persistencia, evitar dejar el archivo
            // guardado sin su registro correspondiente.
            if (System.IO.File.Exists(rutaArchivo))
            {
                System.IO.File.Delete(rutaArchivo);
            }

            throw;
        }
    }


    // Actualizar estado: administrador o recolector.
    

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

    var estadoNuevo = estadosPermitidos.FirstOrDefault(e =>
        e.Equals(
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

    // Impedir cualquier modificación si ya fue finalizada.
    if (solicitud.Estado == "Finalizada")
    {
        return BadRequest(new
        {
            mensaje =
                "No se puede modificar una solicitud que ya fue finalizada."
        });
    }

    // La solicitud solo puede finalizarse mediante
    // el endpoint que guarda la firma de conformidad.
    if (estadoNuevo == "Finalizada")
    {
        return BadRequest(new
        {
            mensaje =
                "Para finalizar la solicitud, el vecino debe firmar la conformidad."
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
            return Forbid();
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

public class ActualizarEstadoSolicitudRequest
{
    public string Estado { get; set; } = string.Empty;

    public string? Observacion { get; set; }
}
