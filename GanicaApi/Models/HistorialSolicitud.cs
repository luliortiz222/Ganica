
namespace GanicaApi.Models;

public class HistorialSolicitud
{
    public long Id { get; set; }

    public int SolicitudId { get; set; }

    public long UsuarioId { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string? EstadoAnterior { get; set; }

    public string? EstadoNuevo { get; set; }

    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;

    public string? Observacion { get; set; }

    public Solicitud? Solicitud { get; set; }

    public Usuario? Usuario { get; set; }
}
