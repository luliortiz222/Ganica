
namespace GanicaApi.Models;

public class HistorialNotificacion
{
    public long Id { get; set; }

    public long NotificacionId { get; set; }

    public long UsuarioId { get; set; }

    public string Accion { get; set; } = "Leida";

    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;

    public Notificacion? Notificacion { get; set; }

    public Usuario? Usuario { get; set; }
}
