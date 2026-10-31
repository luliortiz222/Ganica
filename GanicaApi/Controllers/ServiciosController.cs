
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

using GanicaApi.Data;
using GanicaApi.Models;

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

    // ==========================================
    // GET: Obtener TODOS los servicios
    // ==========================================
   [HttpGet]
[AllowAnonymous]
public async Task<IActionResult> GetServicios(CancellationToken ct)
{
    var servicios = await _context.ServiciosRecoleccion
        .AsNoTracking()
        .OrderBy(s => s.Nombre)
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

    // ==========================================
    // GET: Obtener un servicio por ID
    // ==========================================
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServicio(int id, CancellationToken ct)
    {
        var servicio = await _context.ServiciosRecoleccion
            .AsNoTracking()
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

        if (servicio == null)
        {
            return NotFound(new { mensaje = "El servicio solicitado no existe." });
        }

        return Ok(servicio);
    }

    // ==========================================
    // POST: Crear un servicio
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> CrearServicio(
        [FromForm] ServicioRecoleccion dto,
        IFormFile? foto,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre del servicio es obligatorio."
            });
        }

        bool existe = await _context.ServiciosRecoleccion
            .AnyAsync(
                s => s.Nombre.ToLower() == dto.Nombre.Trim().ToLower(),
                ct
            );

        if (existe)
        {
            return BadRequest(new
            {
                mensaje = "Ya existe un servicio registrado con ese nombre."
            });
        }

        // Guardar fotografía si se recibió una.
        if (foto != null && foto.Length > 0)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads"
            );

            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName =
                $"{Guid.NewGuid()}_{Path.GetFileName(foto.FileName)}";

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);

            await foto.CopyToAsync(stream, ct);

            // Si el modelo incorpora FotoUrl, guardar aquí la ruta.
            // dto.FotoUrl = $"/uploads/{uniqueFileName}";
        }

        dto.Nombre = dto.Nombre.Trim();

        _context.ServiciosRecoleccion.Add(dto);

        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetServicio),
            new { id = dto.Id },
            dto
        );
    }

    // ==========================================
    // PUT: Actualizar un servicio
    // ==========================================
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarServicio(
        int id,
        [FromForm] ServicioRecoleccion dto,
        IFormFile? foto,
        CancellationToken ct)
    {
        var servicio = await _context.ServiciosRecoleccion
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (servicio == null)
        {
            return NotFound(new
            {
                mensaje = "El servicio solicitado no existe."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre del servicio no puede quedar vacío."
            });
        }

        bool nombreDuplicado = await _context.ServiciosRecoleccion
            .AnyAsync(
                s => s.Id != id &&
                     s.Nombre.ToLower() == dto.Nombre.Trim().ToLower(),
                ct
            );

        if (nombreDuplicado)
        {
            return BadRequest(new
            {
                mensaje = "Ya existe otro servicio registrado con ese nombre."
            });
        }

        servicio.Nombre = dto.Nombre.Trim();
        servicio.Descripcion = dto.Descripcion;
        servicio.Dias = dto.Dias;
        servicio.Horario = dto.Horario;
        servicio.RequiereSolicitud = dto.RequiereSolicitud;

        // Actualizar fotografía si se recibió una nueva.
        if (foto != null && foto.Length > 0)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads"
            );

            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName =
                $"{Guid.NewGuid()}_{Path.GetFileName(foto.FileName)}";

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);

            await foto.CopyToAsync(stream, ct);

            // Si el modelo incorpora FotoUrl, guardar aquí la ruta.
            // servicio.FotoUrl = $"/uploads/{uniqueFileName}";
        }

        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            id = servicio.Id,
            nombre = servicio.Nombre,
            descripcion = servicio.Descripcion,
            dias = servicio.Dias,
            horario = servicio.Horario,
            requiere_solicitud = servicio.RequiereSolicitud
        });
    }

    // ==========================================
    // DELETE: Eliminar un servicio
    // ==========================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarServicio(
        int id,
        CancellationToken ct)
    {
        var servicio = await _context.ServiciosRecoleccion
            .FindAsync(new object[] { id }, ct);

        if (servicio == null)
        {
            return NotFound(new
            {
                mensaje = "El servicio no existe."
            });
        }

        _context.ServiciosRecoleccion.Remove(servicio);

        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            mensaje = "Servicio eliminado correctamente."
        });
    }

    // ==========================================
    // GET: Generar comprobante PDF con QR
    // ==========================================
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DescargarPdf(
        int id,
        CancellationToken ct)
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

        // URL accesible desde el otro celular.
        var urlRegistro = $"http://192.168.100.239:8100/servicios/{id}";

        using var qrGenerator = new QRCodeGenerator();

        using var qrData = qrGenerator.CreateQrCode(
            urlRegistro,
            QRCodeGenerator.ECCLevel.Q
        );

        var qrCode = new PngByteQRCode(qrData);
        byte[] qrBytes = qrCode.GetGraphic(20);

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

        byte[] pdfBytes = documento.GeneratePdf();

        return File(
            pdfBytes,
            "application/pdf",
            $"comprobante_{id}.pdf"
        );
    }
}
