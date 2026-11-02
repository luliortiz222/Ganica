
using System.Security.Claims;
using System.Security.Cryptography;
using GanicaApi.Data;
using GanicaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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

    // LISTAR SOLICITUDES:
    // Administrador: todas las solicitudes.
    // Recolector: solamente las solicitudes que recibió mediante notificación.
    // Vecino: solamente las solicitudes que creó.
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

    // CREAR SOLICITUD:
    // Exclusivamente el vecino.
    [HttpPost]
    [Authorize(Roles = "vecino")]
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

        if (solicitud.ServicioRecoleccionId <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debés seleccionar un servicio de recolección."
            });
        }

        if (string.IsNullOrWhiteSpace(solicitud.Direccion))
        {
            return BadRequest(new
            {
                mensaje = "Debés indicar la dirección de recolección."
            });
        }

        // Buscar el servicio seleccionado por el vecino.
        // Solamente se permiten servicios que requieren solicitud.
        var servicio = await _context.ServiciosRecoleccion
            .FirstOrDefaultAsync(s =>
                s.Id == solicitud.ServicioRecoleccionId &&
                s.RequiereSolicitud);

        if (servicio == null)
        {
            return BadRequest(new
            {
                mensaje = "El servicio seleccionado no existe o no está habilitado para solicitudes."
            });
        }

        // El tipo de residuo se obtiene del servicio seleccionado.
        solicitud.TipoResiduo = servicio.Nombre;
        solicitud.ServicioRecoleccionId = servicio.Id;
        solicitud.FechaCreacion = DateTime.Now;
        solicitud.Estado = "Pendiente";

        // Generar un token público seguro de 32 caracteres.
        solicitud.TokenPublico =
            Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
                .ToLowerInvariant();

        // El UsuarioId siempre se obtiene del token.
        solicitud.UsuarioId = usuarioId;

        // Evitar asignaciones de navegación enviadas por el cliente.
        solicitud.Usuario = null;
        solicitud.ServicioRecoleccion = null;

        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync();

        // Registrar creación en el historial.
        _context.HistorialSolicitudes.Add(new HistorialSolicitud
        {
            SolicitudId = solicitud.Id,
            UsuarioId = usuarioId,
            Accion = "Creada",
            EstadoAnterior = null,
            EstadoNuevo = solicitud.Estado,
            Fecha = DateTimeOffset.UtcNow,
            Observacion = "Solicitud de recolección creada por un vecino."
        });

        // Buscar administradores activos.
        var administradores = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u =>
                u.Activo &&
                u.Rol != null &&
                u.Rol.Codigo == "administrador")
            .ToListAsync();

        // Notificar a todos los administradores activos.
        foreach (var administrador in administradores)
        {
            _context.Notificaciones.Add(new Notificacion
            {
                UsuarioId = administrador.Id,
                SolicitudId = solicitud.Id,
                Titulo = "Nueva solicitud de un vecino",
                Mensaje =
                    $"Un vecino generó una solicitud de {solicitud.TipoResiduo}. " +
                    $"Dirección: {solicitud.Direccion}. " +
                    $"Observaciones: {solicitud.Observaciones}",
                Tipo = TipoNotificacion.Entrega,
                CreadoEn = DateTimeOffset.UtcNow
            });
        }

        // Buscar recolectores activos.
        var recolectores = await _context.Usuarios
            .Include(u => u.Rol)
            .Where(u =>
                u.Activo &&
                u.Rol != null &&
                u.Rol.Codigo == "recolector")
            .ToListAsync();

        // Notificar a todos los recolectores activos.
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

        // Guardar historial y todas las notificaciones.
        await _context.SaveChangesAsync();

        return Ok(solicitud);
    }

    // GENERAR QR DE UNA SOLICITUD:
    // El vecino propietario y el administrador pueden generar el QR.
    [HttpGet("{id}/qr")]
    [Authorize(Roles = "vecino,administrador")]
    public async Task<IActionResult> GenerarQr(int id)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        var solicitud = await _context.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
        {
            return NotFound(new
            {
                mensaje = "La solicitud no existe."
            });
        }

        // El vecino solamente puede generar el QR de sus propias solicitudes.
        if (User.IsInRole("vecino") &&
            solicitud.UsuarioId != usuarioId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(solicitud.TokenPublico))
        {
            return BadRequest(new
            {
                mensaje = "La solicitud no tiene un token público."
            });
        }

        var baseUrl = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Publico:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return StatusCode(500, new
            {
                mensaje = "No está configurada la dirección pública de Gánica."
            });
        }

        var urlPublica =
            $"{baseUrl.TrimEnd('/')}/p/{solicitud.TokenPublico}";

        using var generador = new QRCodeGenerator();

        using var datosQr = generador.CreateQrCode(
            urlPublica,
            QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(datosQr);
        byte[] imagenQr = qrCode.GetGraphic(20);

        return File(
            imagenQr,
            "image/png",
            $"solicitud-{id}-qr.png");
    }

    // ==========================================
    // GENERAR COMPROBANTE PDF DE UNA SOLICITUD
    // El vecino propietario y el administrador
    // pueden descargar el comprobante.
    // ==========================================
    [HttpGet("{id}/pdf")]
    [Authorize(Roles = "vecino,administrador")]
    public async Task<IActionResult> DescargarComprobantePdf(int id)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        var solicitud = await _context.Solicitudes
            .AsNoTracking()
            .Include(s => s.ServicioRecoleccion)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (solicitud == null)
        {
            return NotFound(new
            {
                mensaje = "La solicitud no existe."
            });
        }

        // El vecino solamente puede descargar sus propios comprobantes.
        if (User.IsInRole("vecino") &&
            solicitud.UsuarioId != usuarioId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(solicitud.TokenPublico))
        {
            return BadRequest(new
            {
                mensaje = "La solicitud no tiene un enlace público disponible."
            });
        }

        var baseUrl = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Publico:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return StatusCode(500, new
            {
                mensaje = "No está configurada la dirección pública de Gánica."
            });
        }

        // El QR abre la consulta pública de esta solicitud.
        var urlPublica =
            $"{baseUrl.TrimEnd('/')}/p/{solicitud.TokenPublico}";

        // Generar imagen QR.
        using var generador = new QRCodeGenerator();

        using var datosQr = generador.CreateQrCode(
            urlPublica,
            QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(datosQr);
        byte[] imagenQr = qrCode.GetGraphic(20);

        var nombreServicio =
            solicitud.ServicioRecoleccion?.Nombre
            ?? solicitud.TipoResiduo;

        // Generar documento PDF.
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(35);

                page.Header()
                    .Column(header =>
                    {
                        header.Item()
                            .Text("GÁNICA")
                            .Bold()
                            .FontSize(24);

                        header.Item()
                            .Text("Comprobante de solicitud de recolección")
                            .FontSize(14);
                    });

                page.Content()
                    .PaddingVertical(20)
                    .Column(col =>
                    {
                        col.Spacing(12);

                        col.Item()
                            .Text($"Solicitud N.º {solicitud.Id}")
                            .Bold()
                            .FontSize(18);

                        col.Item()
                            .Text($"Fecha: {solicitud.FechaCreacion:dd/MM/yyyy HH:mm}");

                        col.Item()
                            .Text($"Servicio: {nombreServicio}");

                        col.Item()
                            .Text($"Tipo de residuo: {solicitud.TipoResiduo}");

                        col.Item()
                            .Text($"Dirección de recolección: {solicitud.Direccion}");

                        if (!string.IsNullOrWhiteSpace(solicitud.Observaciones))
                        {
                            col.Item()
                                .Text($"Observaciones: {solicitud.Observaciones}");
                        }

                        col.Item()
                            .PaddingTop(8)
                            .Text($"Estado actual: {solicitud.Estado}")
                            .Bold()
                            .FontSize(14);

                        col.Item()
                            .PaddingTop(20)
                            .AlignCenter()
                            .Text("Escaneá el código QR para consultar el estado de tu solicitud.")
                            .FontSize(11);

                        col.Item()
                            .PaddingTop(10)
                            .AlignCenter()
                            .Width(180)
                            .Image(imagenQr);

                        col.Item()
                            .PaddingTop(8)
                            .AlignCenter()
                            .Text("La consulta pública no requiere iniciar sesión.")
                            .FontSize(9);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Gánica - Sistema de gestión de recolección")
                    .FontSize(9);
            });
        });

        byte[] pdfBytes = documento.GeneratePdf();

        return File(
            pdfBytes,
            "application/pdf",
            $"comprobante-solicitud-{id}.pdf");
    }

    // CONSULTAR HISTORIAL:
    // Administrador: todo el historial.
    // Vecino: historial de sus propias solicitudes.
    [HttpGet("historial")]
    [Authorize(Roles = "administrador,vecino")]
    public async Task<IActionResult> GetHistorialSolicitudes()
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        var consulta = _context.HistorialSolicitudes
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole("administrador"))
        {
            // El administrador puede consultar todo el historial.
        }
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

    // GUARDAR FIRMA DE CONFORMIDAD:
    // Exclusivamente el vecino propietario de la solicitud.
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

        // El vecino solo puede firmar su propia solicitud.
        if (solicitud.UsuarioId != usuarioId)
            return Forbid();

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

        // Validar encabezado binario PNG.
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

        // Guardar fuera de wwwroot.
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
                mensaje = "Firma de conformidad registrada correctamente.",
                solicitudId = solicitud.Id,
                estado = solicitud.Estado,
                firmaRegistrada = true,
                fechaFirma = solicitud.FirmaConformidadFecha
            });
        }
        catch
        {
            if (System.IO.File.Exists(rutaArchivo))
            {
                System.IO.File.Delete(rutaArchivo);
            }

            throw;
        }
    }

    // ACTUALIZAR ESTADO:
    // Exclusivamente el recolector puede modificar solicitudes.
    // Solo puede modificar las solicitudes que le fueron notificadas.
    [HttpPatch("{id}/estado")]
    [Authorize(Roles = "recolector")]
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
            "En camino"
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

        if (solicitud.Estado == "Finalizada")
        {
            return BadRequest(new
            {
                mensaje =
                    "No se puede modificar una solicitud que ya fue finalizada."
            });
        }

        // La finalización se realiza únicamente mediante la firma.
        if (estadoNuevo == "Finalizada")
        {
            return BadRequest(new
            {
                mensaje =
                    "Para finalizar la solicitud, el vecino debe firmar la conformidad."
            });
        }

        // El recolector solo puede modificar solicitudes notificadas.
        var tieneAsignacion = await _context.Notificaciones
            .AnyAsync(n =>
                n.SolicitudId == id &&
                n.UsuarioId == usuarioId);

        if (!tieneAsignacion)
            return Forbid();

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
            solicitudId = id,
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
