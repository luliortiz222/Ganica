import { offline_queue } from "@/stores/offline_queue";
import { obtener_api_url } from "@/config/debug";

function construir_url(endpoint: string): string {
  const api_url = obtener_api_url();

  if (!api_url) {
    throw new Error("No se configuró la URL de la API.");
  }

  return `${api_url}/${String(endpoint).replace(/^\/+/, "")}`;
}

export async function sincronizar_cola(): Promise<void> {

  const cola = offline_queue.obtener_cola();

  if (cola.length === 0) {
    console.log("No hay operaciones pendientes para sincronizar.");
    return;
  }

  console.log(
    `Sincronizando ${cola.length} operación(es) pendiente(s)...`
  );

  const token = localStorage.getItem("token");

  for (const operacion of cola) {

    try {

      const url = construir_url(operacion.endpoint);

      const headers: Record<string, string> = {
        "Content-Type": "application/json"
      };

      if (token) {
        headers["Authorization"] = `Bearer ${token}`;
      }

      const response = await fetch(url, {
        ...operacion.options,
        headers: {
          ...headers,
          ...operacion.options.headers
        }
      });

      if (!response.ok) {
        throw new Error(
          `Error HTTP al sincronizar: ${response.status}`
        );
      }

      await response.json();

      offline_queue.eliminar_operacion(operacion.id);

      console.log(
        `Operación sincronizada correctamente: ${operacion.endpoint}`
      );

      // Si era una solicitud, la marcamos como enviada
      // en el historial local.
      if (operacion.endpoint === "Solicitudes") {

        const claveSolicitudes = "ganica_solicitudes";
        const datosSolicitudes = localStorage.getItem(claveSolicitudes);

        if (datosSolicitudes) {

          try {

            const solicitudes = JSON.parse(datosSolicitudes);

            const solicitudesActualizadas = solicitudes.map(
              (solicitud: any) => ({
                ...solicitud,
                estado:
                  solicitud.estado === "sin_enviar"
                    ? "enviada"
                    : solicitud.estado
              })
            );

            localStorage.setItem(
              claveSolicitudes,
              JSON.stringify(solicitudesActualizadas)
            );
            window.dispatchEvent(
  new CustomEvent("solicitudes-sincronizadas")
);

          } catch (error) {

            console.error(
              "No se pudieron actualizar las solicitudes locales:",
              error
            );
          }
        }
      }

    } catch (error) {

      console.error(
        `No se pudo sincronizar: ${operacion.endpoint}`,
        error
      );

      // Dejamos la operación en la cola para
      // volver a intentarla posteriormente.
      break;
    }
  }
}