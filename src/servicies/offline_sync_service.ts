
import { offline_queue } from "@/stores/offline_queue";
import { obtener_api_url } from "@/config/debug";

const CLAVE_SOLICITUDES = "ganica_solicitudes_vecino";

// Construye la URL de la API.
function construir_url(endpoint: string): string {
  const api_url = obtener_api_url();

  if (!api_url) {
    throw new Error("No se configuró la URL de la API.");
  }

  return `${api_url.replace(/\/+$/, "")}/${String(endpoint).replace(/^\/+/, "")}`;
}

// Comprueba si la operación corresponde a una solicitud.
function esSolicitud(endpoint: string): boolean {
  return endpoint
    .replace(/^\/+/, "")
    .split("?")[0]
    .toLocaleLowerCase("es-AR") === "solicitudes";
}

// Actualiza la solicitud guardada localmente después de enviarla.
function actualizarSolicitudLocal(
  datosEnviados: any,
  respuesta: any
): void {
  try {
    const contenido = localStorage.getItem(CLAVE_SOLICITUDES);

    if (!contenido) return;

    const solicitudes = JSON.parse(contenido);

    if (!Array.isArray(solicitudes)) return;

    let actualizada = false;

    const solicitudesActualizadas = solicitudes.map(
      (solicitud: any) => {
        if (actualizada) return solicitud;

        const estado = String(solicitud.estado || "")
          .trim()
          .toLocaleLowerCase("es-AR");

        const coincide =
          (estado === "sin enviar" || estado === "sin_enviar") &&

          solicitud.tipoResiduo === datosEnviados.tipoResiduo &&

          solicitud.direccion === datosEnviados.direccion &&

          (solicitud.observaciones || "") ===
            (datosEnviados.observaciones || "") &&

          (solicitud.latitud ?? null) ===
            (datosEnviados.latitud ?? null) &&

          (solicitud.longitud ?? null) ===
            (datosEnviados.longitud ?? null) &&

          Number(solicitud.servicioRecoleccionId ?? 0) ===
            Number(datosEnviados.servicioRecoleccionId ?? 0);

        if (!coincide) return solicitud;

        actualizada = true;

        return {
          ...solicitud,

          // Reemplaza el ID temporal por el ID de la API.
          id: respuesta?.id ?? solicitud.id,

          // Conserva el servicio seleccionado.
          servicioRecoleccionId:
            respuesta?.servicioRecoleccionId ??
            solicitud.servicioRecoleccionId ??
            datosEnviados.servicioRecoleccionId ??
            0,

          estado: respuesta?.estado || "Pendiente",

          fechaCreacion:
            respuesta?.fechaCreacion || solicitud.fechaCreacion,

          firmaConformidadArchivo:
            respuesta?.firmaConformidadArchivo ?? null,

          firmaConformidadFecha:
            respuesta?.firmaConformidadFecha ?? null
        };
      }
    );

    localStorage.setItem(
      CLAVE_SOLICITUDES,
      JSON.stringify(solicitudesActualizadas)
    );

    if (!actualizada) {
      console.warn(
        "No se encontró una solicitud local pendiente que coincida con los datos enviados."
      );
    }

  } catch (error) {
    console.error(
      "No se pudieron actualizar las solicitudes locales:",
      error
    );
  }
}

// Ejecuta la sincronización de la cola.
async function ejecutar_sincronizacion_cola(): Promise<void> {
  if (!navigator.onLine) {
    console.log("Sin conexión. Se pospone la sincronización.");
    return;
  }

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

      const encabezados = new Headers(
        operacion.options.headers
      );

      const esFormData =
        operacion.options.body instanceof FormData;

      if (!esFormData && !encabezados.has("Content-Type")) {
        encabezados.set("Content-Type", "application/json");
      }

      if (token) {
        encabezados.set("Authorization", `Bearer ${token}`);
      }

      const response = await fetch(url, {
        ...operacion.options,
        headers: encabezados
      });

      if (!response.ok) {
        const detalle = await response.text();

        throw new Error(
          `Error HTTP al sincronizar: ${response.status}. ${detalle}`
        );
      }

      const textoRespuesta = await response.text();

      let respuesta: any = {};

      if (textoRespuesta.trim()) {
        try {
          respuesta = JSON.parse(textoRespuesta);
        } catch {
          respuesta = {};
        }
      }

      if (esSolicitud(operacion.endpoint)) {
        let datosEnviados: any = {};

        if (typeof operacion.options.body === "string") {
          try {
            datosEnviados = JSON.parse(
              operacion.options.body
            );
          } catch {
            datosEnviados = {};
          }
        }

        actualizarSolicitudLocal(
          datosEnviados,
          respuesta
        );
      }

      // Se elimina de la cola solamente si se envió correctamente.
      offline_queue.eliminar_operacion(operacion.id);

      console.log(
        `Operación sincronizada correctamente: ${operacion.endpoint}`
      );

      window.dispatchEvent(
        new CustomEvent("solicitudes-sincronizadas")
      );

    } catch (error) {
      console.error(
        `No se pudo sincronizar: ${operacion.endpoint}`,
        error
      );

      // La operación permanece pendiente para otro intento.
      continue;
    }
  }
}

// Evita ejecutar varias sincronizaciones simultáneamente.
let sincronizacionEnCurso = false;

export async function sincronizar_cola(): Promise<void> {
  if (sincronizacionEnCurso) return;

  sincronizacionEnCurso = true;

  try {
    await ejecutar_sincronizacion_cola();
  } finally {
    sincronizacionEnCurso = false;
  }
}

// Inicia la sincronización automática cuando vuelve internet.
export function iniciar_sincronizacion_automatica(): () => void {
  const sincronizarAlConectar = () => {
    console.log(
      "Conexión recuperada. Sincronizando solicitudes..."
    );

    void sincronizar_cola();
  };

  // Si la aplicación se abre con internet, sincroniza inmediatamente.
  if (navigator.onLine) {
    void sincronizar_cola();
  }

  window.addEventListener("online", sincronizarAlConectar);

  // Permite retirar el evento cuando se desmonta la aplicación.
  return () => {
    window.removeEventListener("online", sincronizarAlConectar);
  };
}
