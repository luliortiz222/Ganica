import { ajax_request } from "./ajax_service";
import { obtener_api_url } from "@/config/debug";

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

  // Datos de la firma de conformidad.
  firmaConformidadArchivo?: string | null;
  firmaConformidadFecha?: string | null;
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

export interface ActualizarEstadoSolicitud {
  estado: string;
  observacion?: string;
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

export async function crear_solicitud_api(
  datos: CrearSolicitud
): Promise<SolicitudApi> {
  return await ajax_request<SolicitudApi>("Solicitudes", {
    method: "POST",
    body: JSON.stringify(datos),
    encolable: true
  });
}

export async function listar_solicitudes_api():
  Promise<SolicitudApi[]> {
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

// OBTENER QR PÚBLICO DE UNA SOLICITUD
export async function obtener_qr_solicitud_api(
  id: number
): Promise<Blob> {

  const api_url = obtener_api_url();

  if (!api_url) {
    throw new Error("No se configuró la URL de la API.");
  }

  const token = localStorage.getItem("token");

  if (!token) {
    throw new Error("No hay una sesión iniciada.");
  }

  const url =
    `${api_url.replace(/\/+$/, "")}/Solicitudes/${id}/qr`;

  const response = await fetch(url, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`
    }
  });

  if (!response.ok) {
    throw new Error(
      `No se pudo generar el QR. Error HTTP: ${response.status}`
    );
  }

  return await response.blob();
}

// OBTENER COMPROBANTE PDF DE UNA SOLICITUD
export async function obtener_comprobante_pdf_solicitud_api(
  id: number
): Promise<Blob> {

  const api_url = obtener_api_url();

  if (!api_url) {
    throw new Error("No se configuró la URL de la API.");
  }

  const token = localStorage.getItem("token");

  if (!token) {
    throw new Error("No hay una sesión iniciada.");
  }

  const url =
    `${api_url.replace(/\/+$/, "")}/Solicitudes/${id}/pdf`;

  const response = await fetch(url, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`
    }
  });

  if (!response.ok) {
    throw new Error(
      `No se pudo descargar el comprobante. Error HTTP: ${response.status}`
    );
  }

  return await response.blob();
}

