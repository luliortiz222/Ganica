namespace GanicaApi.Models;
public class RefreshToken
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset? RevocadoEn { get; set; }
}