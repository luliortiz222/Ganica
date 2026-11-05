
using GanicaApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GanicaApi.Services;

public class NotificacionPushInterceptor : SaveChangesInterceptor
{
    private readonly FirebasePushService _firebase;
    private readonly ILogger<NotificacionPushInterceptor> _logger;

    private readonly List<NotificacionPendiente> _pendientes = new();

    public NotificacionPushInterceptor(
        FirebasePushService firebase,
        ILogger<NotificacionPushInterceptor> logger)
    {
        _firebase = firebase;
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _pendientes.Clear();

        var contexto = eventData.Context;

        if (contexto != null)
        {
            var notificaciones = contexto.ChangeTracker
                .Entries<Notificacion>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var notificacion in notificaciones)
            {
                _pendientes.Add(new NotificacionPendiente
                {
                    UsuarioId = notificacion.UsuarioId,
                    Titulo = notificacion.Titulo,
                    Mensaje = notificacion.Mensaje
                });
            }
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contexto = eventData.Context;

            if (contexto == null || _pendientes.Count == 0)
                return await base.SavedChangesAsync(
                    eventData, result, cancellationToken);

            foreach (var pendiente in _pendientes)
            {
                var tokens = await contexto.Set<DispositivoPush>()
                    .AsNoTracking()
                    .Where(d =>
                        d.UsuarioId == pendiente.UsuarioId &&
                        d.Activo)
                    .Select(d => d.Token)
                    .ToListAsync(cancellationToken);

                if (tokens.Count == 0)
                    continue;

                await _firebase.EnviarNotificacionAsync(
                    tokens,
                    pendiente.Titulo,
                    pendiente.Mensaje);
            }
        }
        catch (Exception ex)
        {
            // Un error de Firebase nunca debe impedir
            // que la solicitud o notificación quede guardada.
            _logger.LogError(
                ex,
                "Error al procesar las notificaciones push.");
        }
        finally
        {
            _pendientes.Clear();
        }

        return await base.SavedChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pendientes.Clear();

        return base.SaveChangesFailedAsync(
            eventData,
            cancellationToken);
    }

    private class NotificacionPendiente
    {
        public long UsuarioId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}
