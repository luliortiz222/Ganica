
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GanicaApi.Data;
using GanicaApi.Models;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PuntosRetiroController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PuntosRetiroController(ApplicationDbContext context)
    {
        _context = context;
    }

    // CONSULTAR PUNTOS
    // Todos los roles pueden consultar los puntos activos.
    // El administrador también puede ver los desactivados.

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PuntoRetiro>>> GetPuntosRetiro()
    {
        var consulta = _context.PuntosRetiro.AsQueryable();

        if (!User.IsInRole("administrador"))
        {
            consulta = consulta.Where(p => p.Activo);
        }

        var puntos = await consulta
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return Ok(puntos);
    }

    // CONSULTAR UN PUNTO POR ID

    [HttpGet("{id}")]
    public async Task<ActionResult<PuntoRetiro>> GetPuntoRetiro(int id)
    {
        var punto = await _context.PuntosRetiro
            .FirstOrDefaultAsync(p => p.Id == id);

        if (punto is null ||
            (!punto.Activo && !User.IsInRole("administrador")))
        {
            return NotFound(new
            {
                mensaje = "No se encontró el punto de retiro."
            });
        }

        return Ok(punto);
    }

    // CREAR PUNTO
    // Exclusivo del administrador.

    [HttpPost]
    [Authorize(Roles = "administrador")]
    public async Task<ActionResult<PuntoRetiro>> PostPuntoRetiro(
        [FromBody] PuntoRetiro punto)
    {
        if (string.IsNullOrWhiteSpace(punto.Nombre) ||
            string.IsNullOrWhiteSpace(punto.Barrio) ||
            string.IsNullOrWhiteSpace(punto.Direccion) ||
            string.IsNullOrWhiteSpace(punto.HorarioAtencion) ||
            string.IsNullOrWhiteSpace(punto.Materiales))
        {
            return BadRequest(new
            {
                mensaje = "Completá todos los campos obligatorios."
            });
        }

        punto.Id = 0;
        punto.Nombre = punto.Nombre.Trim();
        punto.Barrio = punto.Barrio.Trim();
        punto.Direccion = punto.Direccion.Trim();
        punto.HorarioAtencion = punto.HorarioAtencion.Trim();
        punto.Materiales = punto.Materiales.Trim();
        punto.Activo = true;

        _context.PuntosRetiro.Add(punto);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPuntoRetiro),
            new { id = punto.Id },
            punto
        );
    }

    // MODIFICAR PUNTO
    // Exclusivo del administrador.

    [HttpPut("{id}")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> PutPuntoRetiro(
        int id,
        [FromBody] PuntoRetiro datos)
    {
        var punto = await _context.PuntosRetiro
            .FirstOrDefaultAsync(p => p.Id == id);

        if (punto is null)
        {
            return NotFound(new
            {
                mensaje = "No se encontró el punto de retiro."
            });
        }

        if (string.IsNullOrWhiteSpace(datos.Nombre) ||
            string.IsNullOrWhiteSpace(datos.Barrio) ||
            string.IsNullOrWhiteSpace(datos.Direccion) ||
            string.IsNullOrWhiteSpace(datos.HorarioAtencion) ||
            string.IsNullOrWhiteSpace(datos.Materiales))
        {
            return BadRequest(new
            {
                mensaje = "Completá todos los campos obligatorios."
            });
        }

        punto.Nombre = datos.Nombre.Trim();
        punto.Barrio = datos.Barrio.Trim();
        punto.Direccion = datos.Direccion.Trim();
        punto.HorarioAtencion = datos.HorarioAtencion.Trim();
        punto.Materiales = datos.Materiales.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Punto de retiro actualizado correctamente.",
            punto
        });
    }

    // BAJA LÓGICA
    // Exclusivo del administrador.
    // No elimina el registro de la base de datos.

    [HttpDelete("{id}")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> DeletePuntoRetiro(int id)
    {
        var punto = await _context.PuntosRetiro
            .FirstOrDefaultAsync(p => p.Id == id);

        if (punto is null)
        {
            return NotFound(new
            {
                mensaje = "No se encontró el punto de retiro."
            });
        }

        if (!punto.Activo)
        {
            return BadRequest(new
            {
                mensaje = "El punto de retiro ya está desactivado."
            });
        }

        punto.Activo = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Punto de retiro desactivado correctamente."
        });
    }
}
