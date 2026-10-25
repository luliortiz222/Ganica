
namespace GanicaApi.Models;

public class Solicitud
{
    public int Id { get; set; }

    public int ServicioRecoleccionId { get; set; }

    // Usuario que creó la solicitud.
    // Nullable para mantener las solicitudes anteriores.
    public long? UsuarioId { get; set; }

    public string Direccion { get; set; } = string.Empty;

    public string Observaciones { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public string Estado { get; set; } = "Pendiente";

    public string TipoResiduo { get; set; } = string.Empty;

    // Coordenadas opcionales de la ubicación marcada en el mapa.
    public double? Latitud { get; set; }

    public double? Longitud { get; set; }

    public ServicioRecoleccion? ServicioRecoleccion { get; set; }

    // Relación con el usuario creador.
    public Usuario? Usuario { get; set; }
}
