using System;

namespace GanicaApi.Models
{
    public class Entrega
    {
        public long Id { get; set; }
        public long PedidoId { get; set; } // O id del reporte/solicitud de recolección
        public long? RepartidorId { get; set; } // El recolector asignado
        public string DireccionLinea { get; set; } = string.Empty;
        public string? DireccionReferencia { get; set; }
        public decimal? DireccionLatitud { get; set; }
        public decimal? DireccionLongitud { get; set; }
        public EstadoEntrega Estado { get; set; } = EstadoEntrega.Pendiente;
        public DateTimeOffset? AsignadaEn { get; set; }
        public DateTimeOffset? EntregadaEn { get; set; }
        public string? Observaciones { get; set; }
    }

    public enum EstadoEntrega
    {
        Pendiente,
        Asignada,
        EnCamino,
        Entregada,   // O Recolectada
        Cancelada
    }
}

