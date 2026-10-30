
import { ajax_request } from '@/servicies/ajax_service';

export interface PuntoRetiro {
  id: number;
  nombre: string;
  barrio: string;
  direccion: string;
  horarioAtencion: string;
  materiales: string;
  activo: boolean;
}

export interface PuntoRetiroRequest {
  nombre: string;
  barrio: string;
  direccion: string;
  horarioAtencion: string;
  materiales: string;
}

const ENDPOINT = 'PuntosRetiro';

export const puntosRetiroService = {
  // Consultar puntos de retiro.
  async listar(): Promise<PuntoRetiro[]> {
    return await ajax_request<PuntoRetiro[]>(
      ENDPOINT,
      { method: 'GET', guardable: true }
    );
  },

  // Crear un punto de retiro.
  async crear(datos: PuntoRetiroRequest): Promise<PuntoRetiro> {
    return await ajax_request<PuntoRetiro>(
      ENDPOINT,
      {
        method: 'POST',
        body: JSON.stringify(datos),
        encolable: false
      }
    );
  },

  // Modificar un punto de retiro.
  async modificar(
    id: number,
    datos: PuntoRetiroRequest
  ) {
    return await ajax_request(
      `${ENDPOINT}/${id}`,
      {
        method: 'PUT',
        body: JSON.stringify(datos),
        encolable: false
      }
    );
  },

  // Desactivar un punto de retiro (baja lógica).
  async desactivar(id: number) {
    return await ajax_request(
      `${ENDPOINT}/${id}`,
      {
        method: 'DELETE',
        encolable: false
      }
    );
  }
};
