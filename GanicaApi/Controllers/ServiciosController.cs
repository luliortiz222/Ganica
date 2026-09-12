 using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GanicaApi.Data;
using Microsoft.AspNetCore.Authorization;
using GanicaApi.Models; // Asegúrate de que coincida con el namespace de tu modelo ServicioRecoleccion

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
    public async Task<IActionResult> CrearServicio([FromBody] ServicioRecoleccion dto, CancellationToken ct)
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

        _context.ServiciosRecoleccion.Add(dto);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetServicio), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarServicio(int id, [FromBody] ServicioRecoleccion dto, CancellationToken ct)
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
}