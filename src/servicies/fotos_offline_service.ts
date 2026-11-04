
import { obtener_api_url } from "@/config/debug";

interface FotoGuardada {
  id: string;
  fotoUrl: string;
  archivo: Blob;
}

const NOMBRE_BD = "ganica_fotos_offline";
const NOMBRE_STORE = "fotos_servicios";
const VERSION_BD = 1;

const urlsObject = new Map<string, string>();

function abrirBaseDatos(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    if (!("indexedDB" in window)) {
      reject(new Error("IndexedDB no está disponible."));
      return;
    }

    const solicitud = indexedDB.open(NOMBRE_BD, VERSION_BD);

    solicitud.onupgradeneeded = () => {
      const db = solicitud.result;

      if (!db.objectStoreNames.contains(NOMBRE_STORE)) {
        db.createObjectStore(NOMBRE_STORE, {
          keyPath: "id"
        });
      }
    };

    solicitud.onsuccess = () => resolve(solicitud.result);
    solicitud.onerror = () => reject(solicitud.error);
  });
}

async function guardarFoto(
  foto: FotoGuardada
): Promise<void> {
  const db = await abrirBaseDatos();

  return new Promise((resolve, reject) => {
    const transaccion = db.transaction(
      NOMBRE_STORE,
      "readwrite"
    );

    transaccion.objectStore(NOMBRE_STORE).put(foto);

    transaccion.oncomplete = () => {
      db.close();
      resolve();
    };

    transaccion.onerror = () => {
      db.close();
      reject(transaccion.error);
    };

    transaccion.onabort = () => {
      db.close();
      reject(transaccion.error);
    };
  });
}

async function obtenerFotoGuardada(
  id: string
): Promise<FotoGuardada | null> {
  const db = await abrirBaseDatos();

  return new Promise((resolve, reject) => {
    const transaccion = db.transaction(
      NOMBRE_STORE,
      "readonly"
    );

    const solicitud = transaccion
      .objectStore(NOMBRE_STORE)
      .get(id);

    solicitud.onsuccess = () => {
      db.close();
      resolve(solicitud.result ?? null);
    };

    solicitud.onerror = () => {
      db.close();
      reject(solicitud.error);
    };
  });
}

function construirUrlFoto(ruta: string): string {
  if (/^https?:\/\//i.test(ruta)) {
    return ruta;
  }

  const baseApi = obtener_api_url().replace(
    /\/api\/?$/i,
    ""
  );

  return `${baseApi}${ruta.startsWith("/") ? "" : "/"}${ruta}`;
}

function crearUrlLocal(
  id: string,
  archivo: Blob
): string {
  const anterior = urlsObject.get(id);

  if (anterior) {
    URL.revokeObjectURL(anterior);
  }

  const nuevaUrl = URL.createObjectURL(archivo);
  urlsObject.set(id, nuevaUrl);

  return nuevaUrl;
}

/**
 * Descarga y guarda la fotografía de un servicio.
 * Si no hay conexión o falla la descarga, utiliza
 * la última fotografía guardada en el dispositivo.
 */
export async function obtenerFotoOffline(
  idServicio: string | number,
  fotoUrl: string | null | undefined
): Promise<string | null> {
  if (!fotoUrl || typeof fotoUrl !== "string") {
    return null;
  }

  const id = String(idServicio);
  const urlCompleta = construirUrlFoto(fotoUrl);

  if (navigator.onLine) {
    try {
      const controller = new AbortController();

      const timeout = window.setTimeout(
        () => controller.abort(),
        8000
      );

      let respuesta: Response;

      try {
        respuesta = await fetch(urlCompleta, {
          signal: controller.signal
        });
      } finally {
        window.clearTimeout(timeout);
      }

      if (!respuesta.ok) {
        throw new Error(
          `No se pudo descargar la fotografía: ${respuesta.status}`
        );
      }

      const archivo = await respuesta.blob();

      if (!archivo.type.startsWith("image/")) {
        throw new Error("El archivo recibido no es una imagen.");
      }

      // Primero guardamos la nueva foto.
      // Si falla el almacenamiento, conservamos la anterior.
      await guardarFoto({
        id,
        fotoUrl,
        archivo
      });

      return crearUrlLocal(id, archivo);

    } catch (error) {
      console.warn(
        `No se pudo actualizar la foto del servicio ${id}. Se buscará la copia local.`,
        error
      );
    }
  }

  // Sin conexión o si falló la descarga:
  // recuperamos la última fotografía guardada.
  try {
    const guardada = await obtenerFotoGuardada(id);

    if (guardada?.archivo) {
      return crearUrlLocal(id, guardada.archivo);
    }

  } catch (error) {
    console.error(
      `No se pudo recuperar la foto local del servicio ${id}:`,
      error
    );
  }

  return null;
}
