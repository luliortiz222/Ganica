
import { ajax_request } from "./ajax_service";
import { Capacitor } from "@capacitor/core";
import { LocalNotifications } from "@capacitor/local-notifications";

export async function obtener_bandeja_notificaciones(usuarioId: number) {
    try {
        const notificaciones = await ajax_request<any[]>(
            `Notificaciones/usuario/${usuarioId}`,
            {
                method: "GET"
            }
        );

        return {
            ok: true,
            notificaciones,
            mensaje: null
        };

    } catch (error: any) {
        console.error("Error obteniendo notificaciones:", error);

        return {
            ok: false,
            notificaciones: [],
            mensaje: "Error al cargar la bandeja."
        };
    }
}

export async function marcar_notificacion_leida(id: number) {
    try {
        await ajax_request(
            `Notificaciones/${id}/leer`,
            {
                method: "PATCH",
                body: JSON.stringify({})
            }
        );

        return { ok: true };

    } catch (error) {
        console.error("Error marcando notificación:", error);
        return { ok: false };
    }
}

export async function notificar_evento_local(
    titulo: string,
    mensaje: string
) {
    if (!Capacitor.isPluginAvailable("LocalNotifications")) {
        return {
            ok: false,
            mensaje: "Notificaciones locales no disponibles."
        };
    }

    const permisos = await LocalNotifications.checkPermissions();

    if (permisos.display !== "granted") {
        const solicitados = await LocalNotifications.requestPermissions();

        if (solicitados.display !== "granted") {
            return {
                ok: false,
                mensaje: "Sin permiso de notificaciones."
            };
        }
    }

    await LocalNotifications.schedule({
        notifications: [{
            id: Date.now() % 2147483647,
            title: titulo,
            body: mensaje,
            schedule: {
                at: new Date(Date.now() + 250)
            }
        }]
    });

    return { ok: true, mensaje: null };
}
