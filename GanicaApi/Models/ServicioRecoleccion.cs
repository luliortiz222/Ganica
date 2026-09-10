namespace GanicaApi.Models;

public class ServicioRecoleccion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Dias { get; set; } = string.Empty;
    public string Horario { get; set; } = string.Empty;
    public bool RequiereSolicitud { get; set; }
}