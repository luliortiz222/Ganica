// @ts-ignore
import { Capacitor } from '@capacitor/core';
// @ts-ignore
import { Geolocation } from '@capacitor/geolocation';

export async function obtener_ubicacion_actual() {
    if (!Capacitor.isPluginAvailable('Geolocation')) {
        return { ok: false, ubicacion: null, mensaje: 'La geolocalización no está disponible.' };
    }
    try {
        const permisos = await Geolocation.requestPermissions();
        if (permisos.location != 'granted' && permisos.coarseLocation != 'granted') {
            return { ok: false, ubicacion: null, mensaje: 'Sin permiso de ubicación. Habilitalo en los ajustes.' };
        }
        const posicion = await Geolocation.getCurrentPosition({
            enableHighAccuracy: true, timeout: 12000, maximumAge: 60000
        });
        return {
            ok: true,
            ubicacion: {
                latitud: posicion.coords.latitude,
                longitud: posicion.coords.longitude,
                precision: posicion.coords.accuracy
            },
            mensaje: null
        };
    } catch (error: any) {
        return { ok: false, ubicacion: null, mensaje: (error && error.message) || 'No se pudo obtener la ubicación.' };
    }
}

export function url_mapa(latitud: number | null, longitud: number | null, direccion: string = '') {
    if (latitud != null && longitud != null) {
        return `https://www.google.com/maps/search/?api=1&query=${latitud},${longitud}`;
    }
    return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(direccion)}`;
}