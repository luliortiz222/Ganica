import { ref, computed } from "vue";
import {
  obtener_servicios_api,
  crear_servicio_api,
  actualizar_servicio_api,
  eliminar_servicio_api
} from "@/servicies/servicios_service";

export interface Servicio {
  id: number | string;
  nombre?: string;
  titulo?: string;
  descripcion?: string;
  detalle?: string;
  dias?: string;
  horario?: string;
  requiere_solicitud?: boolean;
}

// Variables de estado globales
const servicios = ref<Servicio[]>([]);
const cargando = ref(false);
const error = ref<string | null>(null);

const hay_servicios = computed(() => servicios.value.length > 0);
const lista = computed(() => servicios.value);

const cargar_servicios = async () => {
  cargando.value = true;
  error.value = null;

  try {
    const data = await obtener_servicios_api();
    servicios.value = Array.isArray(data) ? data : [];
  } catch (err: any) {
    console.error("Error al cargar servicios:", err);
    error.value =
      err?.message ||
      "No se pudo conectar con la API. Revisá que la API esté corriendo y que la IP sea accesible.";
  } finally {
    cargando.value = false;
  }
};

const crear_servicio = async (nuevoServicio: Partial<Servicio>) => {
  await crear_servicio_api(nuevoServicio);
  await cargar_servicios();
};

const editar_servicio = async (id: string | number, datos: Partial<Servicio>) => {
  await actualizar_servicio_api(id, datos);
  await cargar_servicios();
};

const eliminar_servicio = async (id: string | number) => {
  await eliminar_servicio_api(id);
  await cargar_servicios();
};

export const servicios_store = {
  get servicios() { return servicios.value; },
  get cargando() { return cargando.value; },
  get error() { return error.value; },
  get hay_servicios() { return hay_servicios.value; },
  get lista() { return lista.value; },
  cargar_servicios,
  crear_servicio,
  editar_servicio,
  eliminar_servicio
};