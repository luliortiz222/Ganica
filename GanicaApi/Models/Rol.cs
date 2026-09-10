namespace GanicaApi.Models;
public class Rol
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty; // "administrador", "recolector", "vecino", "quizas sin rol"
    public bool Activo { get; set; } = true;
}