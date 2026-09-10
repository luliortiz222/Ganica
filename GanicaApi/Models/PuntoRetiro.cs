namespace GanicaApi.Models;

public class PuntoRetiro
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string HorarioAtencion { get; set; } = string.Empty;
}