import { ajax_request } from "./ajax_service";

export interface CrearSolicitud {
  servicioRecoleccionId: number;
  direccion: string;
  observaciones: string;
}

export interface SolicitudApi {
  id: number;
  servicioRecoleccionId: number;
  direccion: string;
  observaciones: string;
  fechaCreacion: string;
  estado: string;
}

export async function crear_solicitud_api(
  datos: CrearSolicitud
): Promise<SolicitudApi> {
  return await ajax_request<SolicitudApi>("Solicitudes", {
    method: "POST",
    body: JSON.stringify(datos),
    encolable: true
  });
}