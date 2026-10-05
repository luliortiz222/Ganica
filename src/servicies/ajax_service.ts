import { obtener_api_url } from "@/config/debug";
import { offline_store } from "@/stores/offline_store";
import { offline_queue } from "@/stores/offline_queue";

function construir_url(endpoint: string): string {
  const api_url = obtener_api_url();

  if (!api_url) {
    throw new Error("No se configuró la URL de la API.");
  }

  return `${api_url}/${String(endpoint).replace(/^\/+/, "")}`;
}

export async function ajax_request<T = any>(
  endpoint: string,
  options: RequestInit & {
    guardable?: boolean;
    encolable?: boolean;
  } = {}
): Promise<T> {

  const {
    guardable = false,
    encolable = false,
    ...fetchOptions
  } = options;

  const url = construir_url(endpoint);

  const controller = new AbortController();

  const timeoutId = setTimeout(() => {
    controller.abort();
  }, 5000);

  const token = localStorage.getItem("token");

  const esFormData = fetchOptions.body instanceof FormData;

  const headers: Record<string, string> = {};

  if (!esFormData) {
    headers["Content-Type"] = "application/json";
  }

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  try {

    // Si estamos offline, guardamos directamente la operación.
    if (encolable && !navigator.onLine) {

      const operacionId = offline_queue.agregar_operacion(
        endpoint,
        fetchOptions
      );

      console.log(
        `Sin conexión. Operación guardada: ${endpoint}`
      );

      throw new Error(
        `Sin conexión. Solicitud guardada como pendiente. ID: ${operacionId}`
      );
    }

    const response = await fetch(url, {
      ...fetchOptions,
      signal: controller.signal,
      headers: {
        ...headers,
        ...fetchOptions.headers,
      },
    });

    if (!response.ok) {

      let mensajeError = `Error HTTP: ${response.status}`;

      try {

        const errorData = await response.json();

        if (errorData?.mensaje) {
          mensajeError = errorData.mensaje;
        } else if (errorData?.message) {
          mensajeError = errorData.message;
        }

      } catch {
      }

      throw new Error(mensajeError);
    }

    const datos = await response.json();

    if (guardable) {
      offline_store.guardar_copia(endpoint, datos);
    }

    return datos;

  } catch (error: any) {

    /*
     * Si la petición se puede encolar y falló por
     * falta de conexión, la guardamos para enviarla después.
     */
    if (
      encolable &&
      (
        error?.name === "AbortError" ||
        error instanceof TypeError
      )
    ) {

      const operacionId = offline_queue.agregar_operacion(
        endpoint,
        fetchOptions
      );

      console.log(
        `Operación guardada para enviar después: ${endpoint}`
      );

      throw new Error(
        `Sin conexión. Solicitud guardada como pendiente. ID: ${operacionId}`
      );
    }

    /*
     * Si es una lectura guardable y la API no responde,
     * usamos la última copia disponible.
     */
    if (guardable) {

      const copia = offline_store.obtener_copia<T>(endpoint);

      if (copia !== null) {

        console.log(
          `API no disponible. Usando copia offline de: ${endpoint}`
        );

        return copia;
      }
    }

    if (error?.name === "AbortError") {

      throw new Error(
        "Tiempo de espera agotado. El servidor no responde."
      );
    }

    throw error;

  } finally {

    clearTimeout(timeoutId);

  }
}