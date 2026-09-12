import { obtener_api_url } from "@/config/debug";



function construir_url(endpoint: string): string {
  const api_url = obtener_api_url();

  if (!api_url) {
    throw new Error("No se configuró la URL de la API.");
  }

  return `${api_url}/${String(endpoint).replace(/^\/+/, "")}`;
}

export async function ajax_request<T = any>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {

  const url = construir_url(endpoint);

  const controller = new AbortController();

  const timeoutId = setTimeout(() => {
    controller.abort();
  }, 5000);

  // Recuperamos el token del almacenamiento local (ajustá la clave si usas otra, ej: 'token')
  const token = localStorage.getItem('token');
  console.log("TOKEN ENVIADO:", token); // <-- Agregá esto para ver si lo lee

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal,
      headers: {
        ...headers,
        ...options.headers,
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

    return await response.json();

  } catch (error: any) {

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