
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

      const esFormData = operacion.options.body instanceof FormData;

      const headers: Record<string, string> = {};

      if (!esFormData) {
        headers["Content-Type"] = "application/json";
      }

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

      const respuesta = await response.json();

      offline_queue.eliminar_operacion(operacion.id);

      console.log(
        `Operación sincronizada correctamente: ${operacion.endpoint}`
      );

      if (operacion.endpoint === "Solicitudes") {
        const claveSolicitudes = "ganica_solicitudes";
        const datosSolicitudes = localStorage.getItem(claveSolicitudes);

        if (datosSolicitudes) {
          try {
            const solicitudes = JSON.parse(datosSolicitudes);

            // Intentamos relacionar la operación enviada con
            // la solicitud local mediante sus datos originales.
            let datosEnviados: any = {};

            try {
              if (typeof operacion.options.body === "string") {
                datosEnviados = JSON.parse(operacion.options.body);
              }
            } catch {
              datosEnviados = {};
            }

            let actualizada = false;

            const solicitudesActualizadas = solicitudes.map(
              (solicitud: any) => {
                const coincide =
                  !actualizada &&
                  solicitud.estado === "sin_enviar" &&
                  solicitud.tipo === datosEnviados.tipoResiduo &&
                  solicitud.direccion === datosEnviados.direccion &&
                  solicitud.observaciones === datosEnviados.observaciones &&
                  (solicitud.latitud ?? null) ===
                    (datosEnviados.latitud ?? null) &&
                  (solicitud.longitud ?? null) ===
                    (datosEnviados.longitud ?? null);

                if (coincide) {
                  actualizada = true;

                  return {
                    ...solicitud,
                    estado: "enviada",
                    id: respuesta?.id ?? solicitud.id
                  };
                }

                return solicitud;
              }
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

      // Si falla, la operación permanece en la cola
      // para volver a intentarlo posteriormente.
      continue;
    }
  }
}
