using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GanicaApi.Data;
using GanicaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;

namespace GanicaApi.Controllers;

[ApiController]
[Route("api/sesion")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario is null || !usuario.Activo || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return BadRequest(new { mensaje = "Credenciales inválidas o cuenta desactivada." });
        }

        var (accessToken, expiraEn) = GenerarAccessToken(usuario);
        var refreshToken = await GenerarRefreshToken(usuario.Id);

        return Ok(new
        {
            token = accessToken,
            expira_en = expiraEn,
            refresh_token = refreshToken,
            usuario = new
            {
                usuario.Id,
                usuario.Email,
                rol = usuario.Rol?.Codigo
            }
        });
    }

    [HttpGet("yo")]
    [Authorize]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (usuario is null) return Unauthorized();

        return Ok(new
        {
            usuario.Id,
            usuario.Email,
            rol = usuario.Rol?.Codigo,
            nombre_rol = usuario.Rol?.Nombre
        });
    }

    private (string token, DateTime expiraEn) GenerarAccessToken(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (usuario.Rol is not null)
        {
            claims.Add(new Claim("rol_codigo", usuario.Rol.Codigo));
            claims.Add(new Claim(ClaimTypes.Role, usuario.Rol.Codigo));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"] ?? "Ganica_Clave_Secreta_Super_Segura_2026_UnSL_TUDs!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiraEn = DateTime.UtcNow.AddHours(2);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "GanicaApi",
            audience: _config["Jwt:Audience"] ?? "GanicaApp",
            claims: claims,
            expires: expiraEn,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }

    private async Task<string> GenerarRefreshToken(long usuarioId)
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var rawToken = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UsuarioId = usuarioId,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
            RevocadoEn = null
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return rawToken;
    }
}

public record LoginDto(string Email, string Password);