import { defineStore } from 'pinia';
import axios from 'axios';

export const useEntregasStore = defineStore('entregas', {
    state: () => ({
        entregas: [],
        cargando: false
    }),
    actions: {
        async cargarEntregas() {
            this.cargando = true;
            try {
                const response = await axios.get('/api/entregas');
                this.entregas = response.data;
            } catch (error) {
                console.error('Error al cargar entregas/recolecciones:', error);
            } finally {
                this.cargando = false;
            }
        },
        async asignarEntrega(id, repartidorId) {
    try {
        await axios.patch(`/api/entregas/${id}/asignar`, {
            repartidorId: repartidorId
        });

        await this.cargarEntregas();

        return { ok: true };
    } catch (error) {
        console.error('Error al asignar entrega:', error);

        return {
            ok: false,
            mensaje: 'No se pudo asignar la recolección.'
        };
    }
},
        async cambiarEstado(id, nuevoEstado) {
            try {
                await axios.patch(`/api/entregas/${id}/estado`, JSON.stringify(nuevoEstado), {
                    headers: { 'Content-Type': 'application/json' }
                });
                await this.cargarEntregas();
                return { ok: true };
            } catch (error) {
                return { ok: false, mensaje: 'Error al actualizar el estado.' };
            }
        }
    }
});