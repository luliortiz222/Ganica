
using System.Security.Claims;
using GanicaApi.Data;
using GanicaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DispositivosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DispositivosController(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool ObtenerUsuarioId(out long usuarioId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out usuarioId);
    }

    // Registrar o actualizar el dispositivo del usuario autenticado.
    [HttpPut]
    public async Task<IActionResult> RegistrarDispositivo(
        [FromBody] RegistrarDispositivoRequest request)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { mensaje = "El token es obligatorio." });

        var token = request.Token.Trim();

        if (token.Length > 255)
            return BadRequest(new { mensaje = "El token no es válido." });

        var plataforma = string.IsNullOrWhiteSpace(request.Plataforma)
            ? "android"
            : request.Plataforma.Trim().ToLowerInvariant();

        var ahora = DateTimeOffset.UtcNow;

        var dispositivo = await _context.DispositivosPush
            .FirstOrDefaultAsync(d => d.Token == token);

        if (dispositivo == null)
        {
            dispositivo = new DispositivoPush
            {
                UsuarioId = usuarioId,
                Token = token,
                Plataforma = plataforma,
                CreadoEn = ahora,
                UltimaActualizacion = ahora,
                Activo = true
            };

            _context.DispositivosPush.Add(dispositivo);
        }
        else
        {
            dispositivo.UsuarioId = usuarioId;
            dispositivo.Plataforma = plataforma;
            dispositivo.UltimaActualizacion = ahora;
            dispositivo.Activo = true;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlConnector.MySqlException mysqlEx
                  && mysqlEx.Number == 1062)
        {
            // Otro pedido pudo registrar el mismo token
            // al mismo tiempo. Descartamos el alta fallida.
            if (_context.Entry(dispositivo).State == EntityState.Added)
            {
                _context.Entry(dispositivo).State = EntityState.Detached;
            }

            // Buscamos el registro que creó la otra petición.
            var existente = await _context.DispositivosPush
                .FirstOrDefaultAsync(d => d.Token == token);

            if (existente == null)
                throw;

            // Reasignamos el dispositivo al usuario actual.
            existente.UsuarioId = usuarioId;
            existente.Plataforma = plataforma;
            existente.UltimaActualizacion = DateTimeOffset.UtcNow;
            existente.Activo = true;

            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            mensaje = "Dispositivo registrado correctamente."
        });
    }

    // Desactivar el dispositivo al cerrar sesión.
    [HttpDelete]
    public async Task<IActionResult> DesactivarDispositivo(
        [FromBody] RegistrarDispositivoRequest request)
    {
        if (!ObtenerUsuarioId(out var usuarioId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { mensaje = "El token es obligatorio." });

        var token = request.Token.Trim();

        var dispositivo = await _context.DispositivosPush
            .FirstOrDefaultAsync(d =>
                d.Token == token && d.UsuarioId == usuarioId);

        if (dispositivo == null)
            return NotFound(new { mensaje = "El dispositivo no existe." });

        dispositivo.Activo = false;
        dispositivo.UltimaActualizacion = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Dispositivo desactivado correctamente."
        });
    }
}

public class RegistrarDispositivoRequest
{
    public string Token { get; set; } = string.Empty;

    public string Plataforma { get; set; } = "android";
}
