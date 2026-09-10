using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PuntoRetiro>>> GetPuntosRetiro()
    {
        return await _context.PuntosRetiro.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<PuntoRetiro>> PostPuntoRetiro(PuntoRetiro punto)
    {
        _context.PuntosRetiro.Add(punto);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPuntosRetiro), new { id = punto.Id }, punto);
    }
}