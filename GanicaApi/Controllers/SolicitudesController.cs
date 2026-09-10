using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GanicaApi.Data;
using GanicaApi.Models;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitudesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SolicitudesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudes()
    {
        return await _context.Solicitudes
            .Include(s => s.ServicioRecoleccion)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Solicitud>> PostSolicitud(Solicitud solicitud)
    {
        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync();
        return CreatedAtRoute(new { id = solicitud.Id }, solicitud);
    }
}