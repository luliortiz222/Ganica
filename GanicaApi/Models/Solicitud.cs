namespace GanicaApi.Models;

public class Solicitud
{
    public int Id { get; set; }
    public int ServicioRecoleccionId { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public string Estado { get; set; } = "Pendiente";

    public ServicioRecoleccion? ServicioRecoleccion { get; set; }
}