
using System.Text.Encodings.Web;
using GanicaApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/publico")]
[AllowAnonymous]
public class PublicoController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PublicoController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Buscar una solicitud utilizando su token público.
    private async Task<SolicitudPublicaDto?> BuscarSolicitud(string token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length != 32 ||
            !token.All(Uri.IsHexDigit))
        {
            return null;
        }

        return await _context.Solicitudes
            .AsNoTracking()
            .Where(s => s.TokenPublico == token.ToLowerInvariant())
            .Select(s => new SolicitudPublicaDto
            {
                NumeroSolicitud = s.Id,
                TipoResiduo = s.TipoResiduo,
                Estado = s.Estado,
                FechaCreacion = s.FechaCreacion,
                Servicio = s.ServicioRecoleccion != null
                    ? s.ServicioRecoleccion.Nombre
                    : null
            })
            .FirstOrDefaultAsync();
    }

    // CONSULTA JSON:
    // GET api/publico/pedidos/{token}
    [HttpGet("pedidos/{token}")]
    public async Task<IActionResult> ConsultarSolicitud(string token)
    {
        var solicitud = await BuscarSolicitud(token);

        if (solicitud == null)
        {
            return NotFound(new
            {
                mensaje = "La solicitud no existe o el enlace no es válido."
            });
        }

        return Ok(new
        {
            numeroSolicitud = solicitud.NumeroSolicitud,
            tipoResiduo = solicitud.TipoResiduo,
            estado = solicitud.Estado,
            fechaCreacion = solicitud.FechaCreacion,
            servicio = solicitud.Servicio
        });
    }

    // PÁGINA WEB PÚBLICA:
    // GET /p/{token}
    [HttpGet("~/p/{token}")]
    public async Task<IActionResult> VerSolicitud(string token)
    {
        var solicitud = await BuscarSolicitud(token);

        if (solicitud == null)
        {
            return Content("""
                <!DOCTYPE html>
                <html lang="es">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>Solicitud no encontrada - Gánica</title>
                </head>
                <body style="font-family:Arial,sans-serif;text-align:center;padding:50px;background:#f5f5f5;">
                    <h1>Gánica</h1>
                    <h2>Solicitud no encontrada</h2>
                    <p>El enlace no es válido o la solicitud no existe.</p>
                </body>
                </html>
                """, "text/html; charset=utf-8");
        }

        // Codificar los datos antes de insertarlos en HTML.
        var encoder = HtmlEncoder.Default;

        var numero = solicitud.NumeroSolicitud.ToString();
        var tipo = encoder.Encode(solicitud.TipoResiduo ?? "");
        var estado = encoder.Encode(solicitud.Estado ?? "");
        var servicio = encoder.Encode(solicitud.Servicio ?? "");
        var fecha = encoder.Encode(
            solicitud.FechaCreacion.ToString("dd/MM/yyyy HH:mm"));

        
var html = $$"""
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="theme-color" content="#245B45">
    <title>Solicitud #{{numero}} - Gánica</title>
    <style>
        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            padding: 24px;
            min-height: 100vh;
            font-family: Arial, sans-serif;
            background: #F3F6F3;
            color: #26352D;
            display: flex;
            justify-content: center;
            align-items: center;
        }

        .contenedor {
            width: 100%;
            max-width: 540px;
            background: white;
            padding: 32px;
            border-radius: 18px;
            box-shadow: 0 5px 24px rgba(0,0,0,0.08);
        }

        .marca {
            color: #245B45;
            font-size: 30px;
            font-weight: bold;
            text-align: center;
            margin-bottom: 8px;
        }

        .subtitulo {
            text-align: center;
            color: #69756E;
            margin-bottom: 28px;
        }

        .numero {
            background: #EAF2EC;
            color: #245B45;
            padding: 16px;
            border-radius: 12px;
            text-align: center;
            font-size: 22px;
            font-weight: bold;
            margin-bottom: 24px;
        }

        .dato {
            padding: 15px 0;
            border-bottom: 1px solid #E7ECE8;
        }

        .etiqueta {
            display: block;
            color: #69756E;
            font-size: 13px;
            margin-bottom: 6px;
        }

        .valor {
            font-size: 17px;
            font-weight: 600;
            overflow-wrap: anywhere;
        }

        .estado {
            display: inline-block;
            background: #FFF1CC;
            color: #805D13;
            padding: 8px 14px;
            border-radius: 20px;
            font-size: 15px;
        }

        .pie {
            text-align: center;
            color: #7A857E;
            font-size: 13px;
            margin-top: 28px;
        }

        @media (max-width: 480px) {
            body {
                padding: 14px;
            }

            .contenedor {
                padding: 24px 20px;
            }
        }
    </style>
</head>
<body>
    <main class="contenedor">
        <div class="marca">GÁNICA</div>

        <p class="subtitulo">
            Consulta pública de solicitud
        </p>

        <div class="numero">
            Solicitud #{{numero}}
        </div>

        <div class="dato">
            <span class="etiqueta">Estado actual</span>
            <span class="estado">{{estado}}</span>
        </div>

        <div class="dato">
            <span class="etiqueta">Servicio</span>
            <div class="valor">{{servicio}}</div>
        </div>

        <div class="dato">
            <span class="etiqueta">Tipo de residuo</span>
            <div class="valor">{{tipo}}</div>
        </div>

        <div class="dato">
            <span class="etiqueta">Fecha de creación</span>
            <div class="valor">{{fecha}}</div>
        </div>

        <p class="pie">
            Información pública de Gánica.
            No se muestran datos personales del vecino.
        </p>
    </main>
</body>
</html>
""";

return Content(html, "text/html; charset=utf-8");
    }
}

// Datos permitidos para la consulta pública.
public class SolicitudPublicaDto
{
    public int NumeroSolicitud { get; set; }
    public string TipoResiduo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string? Servicio { get; set; }
}
