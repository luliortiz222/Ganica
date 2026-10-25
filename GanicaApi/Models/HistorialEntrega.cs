
namespace GanicaApi.Models;

public class HistorialEntrega
{
    public long Id { get; set; }

    public long EntregaId { get; set; }

    public long UsuarioId { get; set; }

    public string EstadoAnterior { get; set; } = string.Empty;

    public string EstadoNuevo { get; set; } = string.Empty;

    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.UtcNow;

    public string? Observacion { get; set; }

    public Entrega? Entrega { get; set; }

    public Usuario? Usuario { get; set; }
}

