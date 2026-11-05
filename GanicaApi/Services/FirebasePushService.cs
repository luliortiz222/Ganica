using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace GanicaApi.Services;

public class FirebasePushService
{
    private readonly ILogger<FirebasePushService> _logger;

    public FirebasePushService(
        IConfiguration configuration,
        ILogger<FirebasePushService> logger)
    {
        _logger = logger;

        var credentialsPath = configuration["Firebase:CredentialsPath"];

        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            throw new InvalidOperationException(
                "No se configuró Firebase:CredentialsPath.");
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credentialsPath)
            });
        }
    }

    public async Task EnviarNotificacionAsync(
        IEnumerable<string> tokens,
        string titulo,
        string mensaje)
    {
        var listaTokens = tokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        if (listaTokens.Count == 0)
            return;

        try
        {
            var notificacion = new MulticastMessage
            {
                Tokens = listaTokens,

                Notification = new Notification
                {
                    Title = titulo,
                    Body = mensaje
                },

                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = "avisos"
                    }
                }
            };

            var resultado = await FirebaseMessaging.DefaultInstance
                .SendEachForMulticastAsync(notificacion);

            _logger.LogInformation(
                "Firebase: {Exitosas} notificaciones enviadas y {Fallidas} fallidas.",
                resultado.SuccessCount,
                resultado.FailureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar notificaciones mediante Firebase.");
        }
    }
}