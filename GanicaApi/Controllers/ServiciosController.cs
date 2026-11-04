
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
    private readonly IWebHostEnvironment _environment;

    private const long TamanoMaximoFoto = 5 * 1024 * 1024; // 5 MB

    public ServiciosController(
        ApplicationDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ==========================================
    // CARPETA Y GUARDADO DE FOTOGRAFÍAS
    // ==========================================

    private string ObtenerCarpetaUploads()
    {
        var webRoot = _environment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot"
            );
        }

        return Path.Combine(webRoot, "uploads");
    }

    private async Task<string> GuardarFotoAsync(
        IFormFile foto,
        CancellationToken ct)
    {
        if (foto.Length > TamanoMaximoFoto)
        {
            throw new InvalidOperationException(
                "La fotografía no puede superar los 5 MB."
            );
        }

        var contentType = foto.ContentType?.ToLowerInvariant();

        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };

        if (extension == null)
        {
            throw new InvalidOperationException(
                "Formato de fotografía no válido. Usá JPG, PNG o WEBP."
            );
        }

        var carpeta = ObtenerCarpetaUploads();

        Directory.CreateDirectory(carpeta);

        var nombreArchivo = $"{Guid.NewGuid()}{extension}";
        var rutaArchivo = Path.Combine(carpeta, nombreArchivo);

        await using var stream = new FileStream(
            rutaArchivo,
            FileMode.CreateNew
        );

        await foto.CopyToAsync(stream, ct);

        return $"/uploads/{nombreArchivo}";
    }

    private void EliminarFotoAnterior(string? fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl))
            return;

        // Solo se eliminan archivos de nuestra carpeta uploads.
        var nombreArchivo = Path.GetFileName(fotoUrl);

        if (string.IsNullOrWhiteSpace(nombreArchivo))
            return;

        var rutaArchivo = Path.Combine(
            ObtenerCarpetaUploads(),
            nombreArchivo
        );

        if (System.IO.File.Exists(rutaArchivo))
        {
            System.IO.File.Delete(rutaArchivo);
        }
    }

    private static bool TieneFoto(IFormFile? foto)
    {
        return foto != null && foto.Length > 0;
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
                requiere_solicitud = s.RequiereSolicitud,
                foto_url = s.FotoUrl
            })
            .ToListAsync(ct);

        return Ok(servicios);
    }

    // ==========================================
    // GET: Obtener un servicio por ID
    // ==========================================

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetServicio(
        int id,
        CancellationToken ct)
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
                requiere_solicitud = s.RequiereSolicitud,
                foto_url = s.FotoUrl
            })
            .FirstOrDefaultAsync(ct);

        if (servicio == null)
        {
            return NotFound(new
            {
                mensaje = "El servicio solicitado no existe."
            });
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

        dto.Nombre = dto.Nombre.Trim();

        if (TieneFoto(foto))
        {
            try
            {
                dto.FotoUrl = await GuardarFotoAsync(foto!, ct);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        _context.ServiciosRecoleccion.Add(dto);

        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetServicio),
            new { id = dto.Id },
            new
            {
                id = dto.Id,
                nombre = dto.Nombre,
                descripcion = dto.Descripcion,
                dias = dto.Dias,
                horario = dto.Horario,
                requiere_solicitud = dto.RequiereSolicitud,
                foto_url = dto.FotoUrl
            }
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

        // Conservar la fotografía anterior si no se recibió otra.
        if (TieneFoto(foto))
        {
            var fotoAnterior = servicio.FotoUrl;

            try
            {
                servicio.FotoUrl = await GuardarFotoAsync(foto!, ct);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }

            await _context.SaveChangesAsync(ct);

            // Eliminar la foto anterior solo después de guardar correctamente.
            EliminarFotoAnterior(fotoAnterior);
        }
        else
        {
            await _context.SaveChangesAsync(ct);
        }

        return Ok(new
        {
            id = servicio.Id,
            nombre = servicio.Nombre,
            descripcion = servicio.Descripcion,
            dias = servicio.Dias,
            horario = servicio.Horario,
            requiere_solicitud = servicio.RequiereSolicitud,
            foto_url = servicio.FotoUrl
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
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (servicio == null)
        {
            return NotFound(new
            {
                mensaje = "El servicio no existe."
            });
        }

        var fotoAnterior = servicio.FotoUrl;

        _context.ServiciosRecoleccion.Remove(servicio);

        await _context.SaveChangesAsync(ct);

        EliminarFotoAnterior(fotoAnterior);

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
