import axios from 'axios';
import { authService } from '@/servicies/auth_service';

export async function obtener_bandeja_notificaciones(usuarioId: number) {
    try {
        const token = authService.obtenerToken();

        console.log('Consultando notificaciones del usuario:', usuarioId);
        console.log('Token existe:', !!token);

        const response = await axios.get(
            `http://localhost:5193/api/Notificaciones/usuario/${usuarioId}`,
            {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            }
        );

        console.log('Notificaciones recibidas:', response.data);

        return {
            ok: true,
            notificaciones: response.data,
            mensaje: null
        };

    } catch (error: any) {
        console.error('Error obteniendo notificaciones:', error);
        console.error('Status:', error?.response?.status);
        console.error('Respuesta:', error?.response?.data);

        return {
            ok: false,
            notificaciones: [],
            mensaje: 'Error al cargar la bandeja.'
        };
    }
}

export async function marcar_notificacion_leida(id: number) {
    try {
        const token = authService.obtenerToken();

        await axios.patch(
            `http://localhost:5193/api/Notificaciones/${id}/leer`,
            {},
            {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            }
        );

        return { ok: true };

    } catch (error) {
        console.error('Error marcando notificación:', error);
        return { ok: false };
    }
}