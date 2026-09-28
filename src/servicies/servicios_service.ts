import { ajax_request } from "./ajax_service";
import type { Servicio } from "@/stores/servicios_store";

export async function obtener_servicios_api() {
  try {
    const respuesta = await ajax_request("servicios");
    return respuesta;
  } catch (error) {
    console.error("Error devuelto por ajax_request en servicios_service:", error);
    throw error;
  }
}

// POST: Alta de servicio (Permite enviar FormData con archivos o JSON)
export async function crear_servicio_api(datos: FormData | Partial<Servicio>) {
  try {
    const esFormData = datos instanceof FormData;
    return await ajax_request<Servicio>("servicios", {
      method: "POST",
      body: esFormData ? datos : JSON.stringify(datos)
    });
  } catch (error) {
    console.error("Error al crear servicio:", error);
    throw error;
  }
}

// PUT: Modificación de servicio
export async function actualizar_servicio_api(id: string | number, datos: FormData | Partial<Servicio>) {
  try {
    const esFormData = datos instanceof FormData;
    return await ajax_request<Servicio>(`servicios/${id}`, {
      method: "PUT",
      body: esFormData ? datos : JSON.stringify(datos)
    });
  } catch (error) {
    console.error("Error al actualizar servicio:", error);
    throw error;
  }
}

// DELETE: Baja de servicio
export async function eliminar_servicio_api(id: string | number) {
  try {
    return await ajax_request<void>(`servicios/${id}`, {
      method: "DELETE"
    });
  } catch (error) {
    console.error("Error al eliminar servicio:", error);
    throw error;
  }
}