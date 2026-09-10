namespace GanicaApi.Models;
public class Usuario
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public long? RolId { get; set; } // Opcional: si es null, entra sin rol
    public Rol? Rol { get; set; }
    public bool Activo { get; set; } = true;
}