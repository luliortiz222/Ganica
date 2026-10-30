
namespace GanicaApi.Models;

public class PuntoRetiro
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Barrio { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public string HorarioAtencion { get; set; } = string.Empty;

    // Materiales separados por comas.
    public string Materiales { get; set; } = string.Empty;

    // Permite dar de baja sin eliminar el registro.
    public bool Activo { get; set; } = true;
}
