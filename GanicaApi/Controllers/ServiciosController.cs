using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GanicaApi.Data;
using Microsoft.AspNetCore.Authorization;
using GanicaApi.Models; // Asegúrate de que coincida con el namespace de tu modelo ServicioRecoleccion
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QRCoder;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiciosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServiciosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous] // <-- Permitimos ver la lista sin estar logueado para destrabar la vista
    public async Task<IActionResult> GetServicios(CancellationToken ct)
    {
        var servicios = await _context.ServiciosRecoleccion
            .Select(s => new
            {
                id = s.Id,
                nombre = s.Nombre,
                descripcion = s.Descripcion,
                dias = s.Dias,
                horario = s.Horario,
                requiere_solicitud = s.RequiereSolicitud
            })
            .ToListAsync(ct);

        return Ok(servicios);
    }

    [HttpGet("{id}")]
[AllowAnonymous]
public async Task<IActionResult> GetServicio(int id, CancellationToken ct)
{
    var servicio = await _context.ServiciosRecoleccion
        .Where(s => s.Id == id)
        .Select(s => new
        {
            id = s.Id,
            nombre = s.Nombre,
            descripcion = s.Descripcion,
            dias = s.Dias,
            horario = s.Horario,
            requiere_solicitud = s.RequiereSolicitud
        })
        .FirstOrDefaultAsync(ct);

    if (servicio == null) return NotFound();

    return Ok(servicio);
}

    [HttpPost]
    public async Task<IActionResult> CrearServicio([FromForm] ServicioRecoleccion dto, IFormFile? foto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new { mensaje = "El nombre del servicio es obligatorio." });
        }

        bool existe = await _context.ServiciosRecoleccion
            .AnyAsync(s => s.Nombre.ToLower() == dto.Nombre.ToLower(), ct);

        if (existe)
        {
            return BadRequest(new { mensaje = "Ya existe un servicio registrado con ese nombre." });
        }

        // Opcional: Si querés guardar el archivo físico en el servidor y guardar la ruta en la BD:
        if (foto != null && foto.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(foto.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await foto.CopyToAsync(stream, ct);
            }

            // Si tu entidad tiene un campo para la foto (ej: FotoUrl), se lo asignás:
            // dto.FotoUrl = $"/uploads/{uniqueFileName}";
        }

        _context.ServiciosRecoleccion.Add(dto);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetServicio), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarServicio(int id, [FromForm] ServicioRecoleccion dto, IFormFile? foto, CancellationToken ct)
    {
        var servicio = await _context.ServiciosRecoleccion.FindAsync(new object[] { id }, ct);

        if (servicio == null)
        {
            return NotFound(new { mensaje = "El servicio solicitado no existe." });
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new { mensaje = "El nombre del servicio no puede quedar vacío." });
        }

        servicio.Nombre = dto.Nombre;
        servicio.Descripcion = dto.Descripcion;
        servicio.Dias = dto.Dias;
        servicio.Horario = dto.Horario;
        servicio.RequiereSolicitud = dto.RequiereSolicitud;

        // Opcional: Manejo de actualización de foto si se envía una nueva
        if (foto != null && foto.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(foto.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await foto.CopyToAsync(stream, ct);
            }
            // servicio.FotoUrl = $"/uploads/{uniqueFileName}";
        }

        await _context.SaveChangesAsync(ct);

        return Ok(servicio);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarServicio(int id, CancellationToken ct)
    {
        var servicio = await _context.ServiciosRecoleccion.FindAsync(new object[] { id }, ct);

        if (servicio == null)
        {
            return NotFound(new { mensaje = "El servicio no existe." });
        }

        _context.ServiciosRecoleccion.Remove(servicio);
        await _context.SaveChangesAsync(ct);

        return Ok(new { mensaje = "Servicio eliminado correctamente." });
    }
[HttpGet("{id}/pdf")]
public async Task<IActionResult> DescargarPdf(int id, CancellationToken ct)
{
    var servicio = await _context.ServiciosRecoleccion
        .FindAsync(new object[] { id }, ct);

    if (servicio == null)
    {
        return NotFound(new
        {
            mensaje = "El servicio solicitado no existe."
        });
    }

    bool estaConfirmado = true;

    if (!estaConfirmado)
    {
        return Conflict(new
        {
            mensaje = "No se puede generar el PDF de un registro en borrador."
        });
    }

    // URL que va a contener el QR.
    // IMPORTANTE: esta IP debe ser accesible desde el otro celular.
    var urlRegistro = $"http://192.168.100.239:8100/servicios/{id}";

    // Generar QR
    using var qrGenerator = new QRCodeGenerator();

    using var qrData = qrGenerator.CreateQrCode(
        urlRegistro,
        QRCodeGenerator.ECCLevel.Q
    );

    var qrCode = new PngByteQRCode(qrData);
    byte[] qrBytes = qrCode.GetGraphic(20);

    // Generar PDF
    var documento = QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(30);

            page.Header()
                .Text("Comprobante de Servicio - GANICA")
                .Bold()
                .FontSize(18);

            page.Content()
                .PaddingVertical(10)
                .Column(col =>
                {
                    col.Spacing(10);

                    col.Item()
                        .Text($"Servicio: {servicio.Nombre}")
                        .FontSize(14);

                    col.Item()
                        .Text($"Descripción: {servicio.Descripcion}");

                    col.Item()
                        .Text(
                            $"Días y Horarios: {servicio.Dias} - {servicio.Horario}"
                        );

                    col.Item()
                        .PaddingTop(15)
                        .AlignCenter()
                        .Text("Escaneá este código para consultar el servicio");

                    col.Item()
                        .AlignCenter()
                        .Width(180)
                        .Image(qrBytes);
                });

            page.Footer()
                .AlignCenter()
                .Text("Sistema de Recolección de Basura");
        });
    });

    // ESTE ERA EL RETURN QUE FALTABA
    byte[] pdfBytes = documento.GeneratePdf();

    return File(
        pdfBytes,
        "application/pdf",
        $"comprobante_{id}.pdf"
    );
}
}