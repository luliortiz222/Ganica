namespace GanicaApi.Models;

public class DispositivoPush
{
    public long Id { get; set; }

    public long UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public string Token { get; set; } = string.Empty;

    public string Plataforma { get; set; } = "android";

    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UltimaActualizacion { get; set; }

    public bool Activo { get; set; } = true;
}