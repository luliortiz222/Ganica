import { ajax_request } from "./ajax_service";
import type { Servicio } from "@/stores/servicios_store";

// GET: Obtener todos los servicios
export async function obtener_servicios_api(): Promise<Servicio[]> {
  try {
    return await ajax_request<Servicio[]>("servicios");
  } catch (error) {
    console.error("Error al obtener servicios:", error);
    throw error;
  }
}

// POST: Alta de servicio
export async function crear_servicio_api(
  datos: FormData | Partial<Servicio>
): Promise<Servicio> {
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
export async function actualizar_servicio_api(
  id: string | number,
  datos: FormData | Partial<Servicio>
): Promise<Servicio> {
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
export async function eliminar_servicio_api(
  id: string | number
): Promise<void> {
  try {
    return await ajax_request<void>(`servicios/${id}`, {
      method: "DELETE"
    });
  } catch (error) {
    console.error("Error al eliminar servicio:", error);
    throw error;
  }
}