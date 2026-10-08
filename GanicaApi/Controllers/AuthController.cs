
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

    public AuthController(
        ApplicationDbContext db,
        IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // LOGIN

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (usuario is null ||
            !usuario.Activo ||
            !BCrypt.Net.BCrypt.Verify(
                dto.Password,
                usuario.PasswordHash))
        {
            return BadRequest(new
            {
                mensaje = "Credenciales inválidas o cuenta desactivada."
            });
        }

        var resultadoToken = GenerarAccessToken(usuario);

        var refreshToken =
            await GenerarRefreshToken(usuario.Id);

        return Ok(new
        {
            token = resultadoToken.token,

            expira_en = resultadoToken.expiraEn,

            refresh_token = refreshToken,

            usuario = new
            {
                usuario.Id,
                usuario.Email,

                // Ejemplo:
                // "vecino"
                rol = usuario.Rol?.Codigo,

                // Ejemplo:
                // "Vecino"
                nombre_rol = usuario.Rol?.Nombre
            }
        });
    }


    // REGISTRO

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Registro(
        [FromBody] RegistroDto dto)
    {
        // Validar email
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new
            {
                mensaje = "El correo es obligatorio."
            });
        }

        // Validar contraseña

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new
            {
                mensaje = "La contraseña es obligatoria."
            });
        }

        // Normalizar email

        var email = dto.Email
            .Trim()
            .ToLower();

        // Verificar si ya existe

        var usuarioExistente = await _db.Usuarios
            .AnyAsync(u =>
                u.Email.ToLower() == email);

        if (usuarioExistente)
        {
            return BadRequest(new
            {
                mensaje =
                    "Ya existe una cuenta registrada con ese correo."
            });
        }

        // CREAR USUARIO

        var usuario = new Usuario
        {
            Email = email,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.Password),

            RolId = 3,

            Activo = true
        };

        // Guardar usuario

        _db.Usuarios.Add(usuario);

        await _db.SaveChangesAsync();

        // Respuesta

        return Ok(new
        {
            mensaje = "Cuenta creada correctamente.",

            usuario = new
            {
                usuario.Id,
                usuario.Email,

                // Como acabamos de crear el usuario
                // sabemos que su rol es Vecino.
                rol = "vecino",

                nombre_rol = "Vecino"
            }
        });
    }

    // Obtener perfil del usuario

    [HttpGet("yo")]
    [Authorize]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var userIdClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier
            )?.Value;

        if (!long.TryParse(
                userIdClaim,
                out var userId))
        {
            return Unauthorized();
        }

        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(
                u => u.Id == userId);

        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            usuario.Id,

            usuario.Email,

            rol = usuario.Rol?.Codigo,

            nombre_rol = usuario.Rol?.Nombre
        });
    }

    // GENERAR ACCESS TOKEN

    private (
        string token,
        DateTime expiraEn
    ) GenerarAccessToken(
        Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Email,
                usuario.Email
            ),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N")
            )
        };

        // Agregar rol al token

        if (usuario.Rol is not null)
        {
            claims.Add(
                new Claim(
                    "rol_codigo",
                    usuario.Rol.Codigo
                )
            );

            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    usuario.Rol.Codigo
                )
            );
        }

        // CLAVE JWT

        var secretKey =
            _config["Jwt:SecretKey"]
            ?? "Ganica_Clave_Secreta_Super_Segura_2026_UnSL_TUDs!";

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)
            );

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

        var expiraEn =
            DateTime.UtcNow.AddHours(2);

        var token = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"],
        audience: _config["Jwt:Audience"],
        claims: claims,
        expires: expiraEn,
        signingCredentials: credentials
);

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return (
            tokenString,
            expiraEn
        );
    }

    private async Task<string> GenerarRefreshToken(
        long usuarioId)
    {
        var randomBytes = new byte[32];

        using var rng =
            RandomNumberGenerator.Create();

        rng.GetBytes(randomBytes);

        var rawToken =
            Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UsuarioId = usuarioId,

            TokenHash =
                BCrypt.Net.BCrypt.HashPassword(
                    rawToken),

            RevocadoEn = null
        };

        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync();

        return rawToken;
    }
}


public record LoginDto(
    string Email,
    string Password
);

public record RegistroDto(
    string Email,
    string Password
);
