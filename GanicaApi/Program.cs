using System.Text;
using GanicaApi.Data;
using GanicaApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Configurar conexión a MySQL
var connectionString = builder.Configuration.GetConnectionString("Default");

// Registrar interceptor de notificaciones push
builder.Services.AddScoped<NotificacionPushInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));

    options.AddInterceptors(
        serviceProvider.GetRequiredService<NotificacionPushInterceptor>());
});

// Registrar servicio de notificaciones Firebase
builder.Services.AddSingleton<FirebasePushService>();

// 1. Configurar Autenticación con JWT
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "Ganica_Clave_Secreta_Super_Segura_2026_UnSL_TUDs!";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GanicaApi",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GanicaApp",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Confirmar contra la BD que el usuario siga activo
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            try
            {
                var db = context.HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();

                var userIdClaim = context.Principal?
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) ||
                    !long.TryParse(userIdClaim, out var userId))
                {
                    Console.WriteLine("DEBUG JWT: Claims de usuario vacíos o inválidos.");
                    context.Fail("Token inválido.");
                    return;
                }

                var usuario = await db.Usuarios.AsNoTracking()
                    .Where(x => x.Id == userId)
                    .Select(x => new { x.Activo, x.RolId })
                    .FirstOrDefaultAsync();

                if (usuario is null || !usuario.Activo)
                {
                    Console.WriteLine($"DEBUG JWT: Usuario ID {userId} no encontrado o inactivo en BD.");
                    context.Fail("El usuario ya no está activo.");
                    return;
                }

                Console.WriteLine($"DEBUG JWT: Usuario ID {userId} validado con éxito en BD.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG JWT EXCEPTION: {ex.Message}");
                context.Fail("Error interno validando usuario.");
            }
        }
    };
});

// 2. API cerrada por defecto
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(
        new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
});

builder.Services.AddOpenApi();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ejecutar Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbInitializer.Seed(context);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseStaticFiles();

// Middlewares de Autenticación y Autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.MapControllers();

app.Run();
