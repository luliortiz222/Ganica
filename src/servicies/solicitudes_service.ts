import { ajax_request } from "./ajax_service";

export interface CrearSolicitud {
  servicioRecoleccionId: number;
  tipoResiduo: string;
  direccion: string;
  observaciones: string;
  latitud?: number | null;
  longitud?: number | null;
}

export interface SolicitudApi {
  id: number;
  servicioRecoleccionId: number;
  tipoResiduo: string;
  direccion: string;
  observaciones: string;
  fechaCreacion: string;
  estado: string;
  latitud?: number | null;
  longitud?: number | null;
}


export interface HistorialSolicitudApi {
  id: number;
  solicitudId: number;
  usuarioId: number;
  usuarioEmail: string | null;
  accion: string;
  estadoAnterior: string | null;
  estadoNuevo: string | null;
  fecha: string;
  observacion: string | null;
  tipoResiduo: string | null;
  direccion: string | null;
}

export async function listar_historial_solicitudes_api():
  Promise<HistorialSolicitudApi[]> {
  return await ajax_request<HistorialSolicitudApi[]>(
    "Solicitudes/historial",
    {
      method: "GET"
    }
  );
}


export interface ActualizarEstadoSolicitud {
  estado: string;
  observacion?: string;
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

export async function listar_solicitudes_api(): Promise<SolicitudApi[]> {
  return await ajax_request<SolicitudApi[]>("Solicitudes", {
    method: "GET"
  });
}

export async function actualizar_estado_solicitud_api(
  id: number,
  datos: ActualizarEstadoSolicitud
): Promise<unknown> {
  return await ajax_request<unknown>(`Solicitudes/${id}/estado`, {
    method: "PATCH",
    body: JSON.stringify(datos)
  });
}

