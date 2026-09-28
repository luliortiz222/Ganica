using System;

namespace GanicaApi.Models
{
    public class Notificacion
    {
        public long Id { get; set; }
        public long UsuarioId { get; set; } // A quién va dirigida (siempre filtrada por token)
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public TipoNotificacion Tipo { get; set; } = TipoNotificacion.Informacion;
        public DateTimeOffset? LeidaEn { get; set; }
        public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

        public Usuario? Usuario { get; set; }
    }

    public enum TipoNotificacion
    {
        Informacion,
        Alerta,
        Estado, 

        Entrega
    }
}