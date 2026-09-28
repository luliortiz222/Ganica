// @ts-ignore
import { Capacitor } from '@capacitor/core';
// @ts-ignore
import { LocalNotifications } from '@capacitor/local-notifications';

export async function notificar_evento_local(titulo: string, mensaje: string) {
    if (!Capacitor.isPluginAvailable('LocalNotifications')) {
        return { ok: false, mensaje: 'Notificaciones locales no disponibles.' };
    }
    
    const permisos = await LocalNotifications.checkPermissions();
    if (permisos.display !== 'granted') {
        const solicitados = await LocalNotifications.requestPermissions();
        if (solicitados.display !== 'granted') {
            return { ok: false, mensaje: 'Sin permiso de notificaciones.' };
        }
    }

    await LocalNotifications.schedule({
        notifications: [{
            id: Date.now() % 2147483647,
            title: titulo,
            body: mensaje,
            schedule: { at: new Date(Date.now() + 250) }
        }]
    });

    return { ok: true, mensaje: null };
}