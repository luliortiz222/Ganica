<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>
        <ion-title>Nueva solicitud</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <h2 class="app-seccion-titulo">Solicitar recolección</h2>

      <p>
        Completá los datos del residuo que necesitás retirar.
        El equipo de recolección recibirá tu solicitud.
      </p>

      <ion-card class="app-card">
        <ion-card-content>
          <form class="app-formulario" @submit.prevent="enviarSolicitud">

            <!-- SERVICIOS DISPONIBLES -->
            <ion-item lines="full">
              <ion-select
                v-model="form.servicioRecoleccionId"
                label="Servicio de recolección"
                label-placement="stacked"
                placeholder="Seleccioná un servicio"
                :disabled="
                  serviciosCargando ||
                  serviciosDisponibles.length === 0 ||
                  enviando
                "
                required
              >
                <ion-select-option
                  v-for="servicio in serviciosDisponibles"
                  :key="servicio.id"
                  :value="Number(servicio.id)"
                >
                  {{ servicio.nombre || servicio.titulo || 'Servicio sin nombre' }}
                </ion-select-option>
              </ion-select>
            </ion-item>

            <ion-note v-if="serviciosCargando" color="medium">
              Cargando servicios disponibles...
            </ion-note>

            <ion-note
              v-else-if="serviciosDisponibles.length === 0"
              color="warning"
            >
              No hay servicios habilitados para solicitudes en este momento.
            </ion-note>

            <!-- INFORMACIÓN DEL SERVICIO -->
            <ion-item
              v-if="servicioSeleccionado"
              lines="none"
            >
              <ion-label class="ion-text-wrap">
                <p v-if="servicioSeleccionado.descripcion">
                  {{ servicioSeleccionado.descripcion }}
                </p>

                <p v-if="servicioSeleccionado.dias">
                  Días: {{ servicioSeleccionado.dias }}
                </p>

                <p v-if="servicioSeleccionado.horario">
                  Horario: {{ servicioSeleccionado.horario }}
                </p>
              </ion-label>
            </ion-item>

            <!-- DIRECCIÓN -->
            <ion-item lines="full">
              <ion-input
                v-model="form.direccion"
                label="Dirección de retiro"
                label-placement="stacked"
                placeholder="Ej.: Av. Mitre 1234"
                required
                @ionInput="limpiarCoordenadas"
              />
            </ion-item>

            <!-- UBICACIÓN EN EL MAPA -->
            <ion-button
              expand="block"
              fill="outline"
              class="ion-margin-top"
              type="button"
              @click="modalUbicacionAbierto = true"
            >
              <ion-icon :icon="locationOutline" slot="start" />
              Seleccionar ubicación en el mapa
            </ion-button>

            <ion-note
              v-if="form.latitud !== null && form.longitud !== null"
              color="success"
            >
              <p class="coordenadas">
                <ion-icon :icon="checkmarkCircleOutline" />
                Ubicación seleccionada
              </p>

              <p class="coordenadas">
                Latitud: {{ form.latitud.toFixed(6) }}
              </p>

              <p class="coordenadas">
                Longitud: {{ form.longitud.toFixed(6) }}
              </p>
            </ion-note>

            <!-- OBSERVACIONES -->
            <ion-item lines="full">
              <ion-textarea
                v-model="form.observaciones"
                label="Observaciones y detalles"
                label-placement="stacked"
                placeholder="Ej.: ramas en bolsas frente al garage"
                :rows="3"
              />
            </ion-item>

            <!-- DICTADO POR VOZ -->
            <ion-button
              expand="block"
              fill="outline"
              type="button"
              class="ion-margin-top"
              :disabled="escuchandoVoz || enviando"
              @click="dictarObservaciones"
            >
              <ion-icon :icon="micOutline" slot="start" />
              {{ escuchandoVoz ? 'Escuchando...' : 'Dictar observaciones' }}
            </ion-button>

            <!-- ENVIAR -->
            <ion-button
              expand="block"
              type="submit"
              class="ion-margin-top"
              :disabled="
                enviando ||
                serviciosCargando ||
                serviciosDisponibles.length === 0
              "
            >
              <ion-icon :icon="sendOutline" slot="start" />
              {{ enviando ? 'Enviando...' : 'Enviar solicitud' }}
            </ion-button>

          </form>
        </ion-card-content>
      </ion-card>

      <!-- MIS SOLICITUDES -->
      <h2 class="app-seccion-titulo">Mis solicitudes</h2>

      <ion-note v-if="cargando" color="medium">
        Cargando tus solicitudes...
      </ion-note>

      <ion-note
        v-else-if="misSolicitudes.length === 0"
        color="medium"
      >
        Todavía no tenés solicitudes registradas.
      </ion-note>

      <ion-list v-else lines="full">
        <ion-item
          v-for="item in misSolicitudes"
          :key="item.id"
          class="app-lista-item"
        >
          <ion-label class="ion-text-wrap">
            <h2>
              <strong>{{ item.tipoResiduo }}</strong>
            </h2>

            <p>📍 {{ item.direccion }}</p>

            <p v-if="item.observaciones">
              💬 {{ item.observaciones }}
            </p>

            <p>
              Fecha:
              {{ formatearFecha(item.fechaCreacion) }}
            </p>

            <p>
              Estado:
              <strong>{{ item.estado }}</strong>
            </p>

            <p
              v-if="tieneFirmaRegistrada(item)"
              class="estado-firma registrada"
            >
              <ion-icon :icon="checkmarkCircleOutline" />
              Conformidad firmada y registrada
            </p>

            <p
              v-else-if="estaEnCamino(item)"
              class="estado-firma pendiente"
            >
              <ion-icon :icon="timeOutline" />
              Conformidad pendiente de firma
            </p>

            <ion-button
              v-if="puedeFirmar(item)"
              expand="block"
              class="ion-margin-top"
              @click="abrirFirma(item)"
            >
              <ion-icon :icon="createOutline" slot="start" />
              Firmar conformidad
            </ion-button>
          </ion-label>
        </ion-item>
      </ion-list>
    </ion-content>

    <!-- MODAL DE UBICACIÓN -->
    <SelectorUbicacion
      :abierto="modalUbicacionAbierto"
      :direccion-inicial="form.direccion"
      :latitud-inicial="form.latitud"
      :longitud-inicial="form.longitud"
      @confirmar="aplicarUbicacion"
      @cerrar="modalUbicacionAbierto = false"
    />

    <!-- MODAL DE FIRMA -->
    <FirmaConformidad
      v-if="solicitudParaFirma"
      :abierto="modalFirmaAbierto"
      :solicitud-id="solicitudParaFirma.id"
      :direccion="solicitudParaFirma.direccion"
      :tipo-residuo="solicitudParaFirma.tipoResiduo"
      @cerrar="cerrarFirma"
      @finalizada="finalizarFirma"
    />
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';

import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonButtons,
  IonMenuButton,
  IonCard,
  IonCardContent,
  IonItem,
  IonLabel,
  IonInput,
  IonTextarea,
  IonSelect,
  IonSelectOption,
  IonButton,
  IonIcon,
  IonList,
  IonNote,
  toastController
} from '@ionic/vue';

import {
  sendOutline,
  checkmarkCircleOutline,
  locationOutline,
  micOutline,
  createOutline,
  timeOutline
} from 'ionicons/icons';

import SelectorUbicacion from '@/components/SelectorUbicacion.vue';
import FirmaConformidad from '@/components/FirmaConformidad.vue';

import {
  crear_solicitud_api,
  listar_solicitudes_api
} from '@/servicies/solicitudes_service';

import type {
  SolicitudApi
} from '@/servicies/solicitudes_service';

import {
  obtener_servicios_solicitables_api
} from '@/servicies/servicios_service';

import type {
  Servicio
} from '@/stores/servicios_store';

import {
  dictar_texto
} from '@/servicies/voz_service';


interface Ubicacion {
  direccion: string;
  latitud: number;
  longitud: number;
}

interface SolicitudLocal {
  id: number;
  servicioRecoleccionId: number;
  tipoResiduo: string;
  direccion: string;
  observaciones: string;
  fechaCreacion: string;
  estado: string;
  latitud: number | null;
  longitud: number | null;
  firmaConformidadArchivo?: string | null;
  firmaConformidadFecha?: string | null;
}

interface SolicitudConFirma extends SolicitudApi {
  firmaConformidadArchivo?: string | null;
  firmaConformidadFecha?: string | null;
}


const CLAVE_LOCAL = 'ganica_solicitudes_vecino';


// FORMULARIO
const form = ref({
  servicioRecoleccionId: 0,
  direccion: '',
  observaciones: '',
  latitud: null as number | null,
  longitud: null as number | null
});


// SERVICIOS
const serviciosDisponibles = ref<Servicio[]>([]);
const serviciosCargando = ref(false);

const servicioSeleccionado = computed(() => {
  return serviciosDisponibles.value.find(
    servicio =>
      Number(servicio.id) ===
      Number(form.value.servicioRecoleccionId)
  ) ?? null;
});


// SOLICITUDES
const misSolicitudes = ref<SolicitudApi[]>([]);
const cargando = ref(false);
const enviando = ref(false);
const escuchandoVoz = ref(false);


// MODALES
const modalUbicacionAbierto = ref(false);
const solicitudParaFirma = ref<SolicitudApi | null>(null);
const modalFirmaAbierto = ref(false);


// TOAST
const mostrarToast = async (
  mensaje: string,
  color: 'success' | 'warning' | 'danger'
) => {
  const toast = await toastController.create({
    message: mensaje,
    duration: 2800,
    color,
    position: 'bottom'
  });

  await toast.present();
};


// CARGAR SERVICIOS
const cargarServicios = async () => {
  serviciosCargando.value = true;

  try {
    const servicios = await obtener_servicios_solicitables_api();

    serviciosDisponibles.value = Array.isArray(servicios)
      ? servicios.filter(servicio =>
          servicio.requiere_solicitud === true
        )
      : [];

    // Verificar que la selección siga disponible.
    const existeSeleccion = serviciosDisponibles.value.some(
      servicio =>
        Number(servicio.id) ===
        Number(form.value.servicioRecoleccionId)
    );

    if (!existeSeleccion) {
      form.value.servicioRecoleccionId = 0;
    }

  } catch (error) {
    console.error('Error al cargar servicios:', error);

    serviciosDisponibles.value = [];

    await mostrarToast(
      'No se pudieron cargar los servicios de recolección.',
      'warning'
    );

  } finally {
    serviciosCargando.value = false;
  }
};


// LEER SOLICITUDES LOCALES
const leerSolicitudesLocales = (): SolicitudLocal[] => {
  try {
    const datos = localStorage.getItem(CLAVE_LOCAL);

    if (!datos) return [];

    const solicitudes = JSON.parse(datos);

    return Array.isArray(solicitudes)
      ? solicitudes
      : [];

  } catch (error) {
    console.error(
      'Error al leer solicitudes locales:',
      error
    );

    return [];
  }
};


// GUARDAR SOLICITUDES LOCALES
const guardarSolicitudesLocales = (
  solicitudes: SolicitudLocal[]
) => {
  localStorage.setItem(
    CLAVE_LOCAL,
    JSON.stringify(solicitudes)
  );
};


// GUARDAR SOLICITUD PENDIENTE
const guardarPendienteLocal = (
  solicitud: SolicitudLocal
) => {
  const guardadas = leerSolicitudesLocales();

  guardadas.unshift(solicitud);

  guardarSolicitudesLocales(guardadas);
};


// APLICAR UBICACIÓN DEL MAPA
const aplicarUbicacion = (ubicacion: Ubicacion) => {
  form.value.direccion = ubicacion.direccion;
  form.value.latitud = ubicacion.latitud;
  form.value.longitud = ubicacion.longitud;
};


// LIMPIAR COORDENADAS
const limpiarCoordenadas = () => {
  form.value.latitud = null;
  form.value.longitud = null;
};


// DICTADO POR VOZ
const dictarObservaciones = async () => {
  if (escuchandoVoz.value || enviando.value) return;

  escuchandoVoz.value = true;

  try {
    const texto = await dictar_texto();

    if (texto.trim()) {
      const actual = form.value.observaciones.trim();

      form.value.observaciones = actual
        ? `${actual} ${texto.trim()}`
        : texto.trim();

      await mostrarToast(
        'Observaciones agregadas.',
        'success'
      );
    }

  } catch (error) {
    console.error(
      'Error al dictar observaciones:',
      error
    );

    await mostrarToast(
      error instanceof Error
        ? error.message
        : 'No se pudo utilizar el reconocimiento de voz.',
      'warning'
    );

  } finally {
    escuchandoVoz.value = false;
  }
};


// CONVERTIR SOLICITUD LOCAL A FORMATO API
const convertirSolicitudLocal = (
  solicitud: SolicitudLocal
): SolicitudApi => {
  return {
    id: solicitud.id,
    servicioRecoleccionId: solicitud.servicioRecoleccionId,
    tipoResiduo: solicitud.tipoResiduo,
    direccion: solicitud.direccion,
    observaciones: solicitud.observaciones,
    fechaCreacion: solicitud.fechaCreacion,
    estado: solicitud.estado,
    latitud: solicitud.latitud,
    longitud: solicitud.longitud,
    firmaConformidadArchivo:
      solicitud.firmaConformidadArchivo ?? null,
    firmaConformidadFecha:
      solicitud.firmaConformidadFecha ?? null
  };
};


// CARGAR MIS SOLICITUDES
const cargarMisSolicitudes = async () => {
  cargando.value = true;

  const locales = leerSolicitudesLocales();

  try {
    const remotas = await listar_solicitudes_api();

    const idsRemotos = new Set(
      remotas.map(solicitud => solicitud.id)
    );

    const localesNoDuplicadas = locales.filter(
      solicitud => !idsRemotos.has(solicitud.id)
    );

    misSolicitudes.value = [
      ...localesNoDuplicadas.map(convertirSolicitudLocal),
      ...remotas
    ];

  } catch (error) {
    console.error(
      'Error al cargar solicitudes:',
      error
    );

    misSolicitudes.value = locales.map(
      convertirSolicitudLocal
    );

  } finally {
    cargando.value = false;
  }
};


// LIMPIAR FORMULARIO
const limpiarFormulario = () => {
  form.value = {
    servicioRecoleccionId: 0,
    direccion: '',
    observaciones: '',
    latitud: null,
    longitud: null
  };
};


// ENVIAR SOLICITUD
const enviarSolicitud = async () => {
  if (enviando.value) return;

  const servicio = servicioSeleccionado.value;

  // Validar servicio.
  if (!servicio) {
    await mostrarToast(
      'Seleccioná un servicio de recolección.',
      'warning'
    );

    return;
  }

  const servicioId = Number(servicio.id);

  if (!Number.isInteger(servicioId) || servicioId <= 0) {
    await mostrarToast(
      'El servicio seleccionado no tiene un identificador válido.',
      'warning'
    );

    return;
  }

  // Validar dirección.
  if (!form.value.direccion.trim()) {
    await mostrarToast(
      'Debés indicar la dirección de recolección.',
      'warning'
    );

    return;
  }

  // Obtener nombre del servicio.
  const nombreServicio = String(
    servicio.nombre ?? servicio.titulo ?? ''
  ).trim();

  if (!nombreServicio) {
    await mostrarToast(
      'El servicio seleccionado no tiene un nombre válido.',
      'warning'
    );

    return;
  }

  enviando.value = true;

  // Estos datos se usan tanto para la API
  // como para guardar la solicitud sin conexión.
  const datos = {
    servicioRecoleccionId: servicioId,
    tipoResiduo: nombreServicio,
    direccion: form.value.direccion.trim(),
    observaciones: form.value.observaciones.trim(),
    latitud: form.value.latitud,
    longitud: form.value.longitud
  };

  try {
    await crear_solicitud_api({
      servicioRecoleccionId: datos.servicioRecoleccionId,
      tipoResiduo: datos.tipoResiduo,
      direccion: datos.direccion,
      observaciones: datos.observaciones,
      latitud: datos.latitud,
      longitud: datos.longitud
    });

    limpiarFormulario();

    await cargarMisSolicitudes();

    await mostrarToast(
      'Solicitud registrada correctamente.',
      'success'
    );

  } catch (error) {
    console.error(
      'Error al crear solicitud:',
      error
    );

    const mensaje = error instanceof Error
      ? error.message
      : '';

    const fueEncolada =
      mensaje.includes('Sin conexión') ||
      mensaje.includes('Tiempo de espera');

    if (fueEncolada) {

      // Guardar también el ID del servicio elegido.
      guardarPendienteLocal({
        id: -Date.now(),
        servicioRecoleccionId: datos.servicioRecoleccionId,
        tipoResiduo: datos.tipoResiduo,
        direccion: datos.direccion,
        observaciones: datos.observaciones,
        fechaCreacion: new Date().toISOString(),
        estado: 'Sin enviar',
        latitud: datos.latitud,
        longitud: datos.longitud
      });

      limpiarFormulario();

      await cargarMisSolicitudes();

      await mostrarToast(
        'Sin conexión. La solicitud quedó guardada y se enviará cuando vuelva internet.',
        'warning'
      );

    } else {
      await mostrarToast(
        mensaje || 'No se pudo registrar la solicitud.',
        'danger'
      );
    }

  } finally {
    enviando.value = false;
  }
};


// NORMALIZAR ESTADO
const normalizarEstado = (
  estado: string | null | undefined
) => {
  return (estado || '')
    .trim()
    .toLocaleLowerCase('es-AR');
};


// VERIFICAR SI ESTÁ EN CAMINO
const estaEnCamino = (item: SolicitudApi) => {
  return normalizarEstado(item.estado) === 'en camino';
};


// VERIFICAR FIRMA REGISTRADA
const tieneFirmaRegistrada = (item: SolicitudApi) => {
  const solicitud = item as SolicitudConFirma;

  return Boolean(
    solicitud.firmaConformidadArchivo ||
    solicitud.firmaConformidadFecha
  );
};


// VERIFICAR SI PUEDE FIRMAR
const puedeFirmar = (item: SolicitudApi) => {
  return (
    estaEnCamino(item) &&
    !tieneFirmaRegistrada(item) &&
    item.servicioRecoleccionId !== 0 &&
    item.id > 0
  );
};


// ABRIR FIRMA
const abrirFirma = (item: SolicitudApi) => {
  if (!puedeFirmar(item)) {
    void mostrarToast(
      'Solo podés firmar una solicitud en camino que todavía no tenga conformidad registrada.',
      'warning'
    );

    return;
  }

  solicitudParaFirma.value = item;
  modalFirmaAbierto.value = true;
};


// CERRAR FIRMA
const cerrarFirma = () => {
  modalFirmaAbierto.value = false;
  solicitudParaFirma.value = null;
};


// FINALIZAR FIRMA
const finalizarFirma = async () => {
  modalFirmaAbierto.value = false;
  solicitudParaFirma.value = null;

  await cargarMisSolicitudes();

  await mostrarToast(
    'Conformidad firmada y registrada correctamente.',
    'success'
  );
};


// FORMATEAR FECHA
const formatearFecha = (fecha: string) => {
  if (!fecha) return 'Sin fecha';

  const valor = new Date(fecha);

  if (Number.isNaN(valor.getTime())) return fecha;

  return valor.toLocaleDateString('es-AR');
};


// ACTUALIZAR DESPUÉS DE SINCRONIZAR
const actualizarTrasSincronizacion = () => {
  void cargarMisSolicitudes();
};


// ACTUALIZAR AL RECUPERAR INTERNET
const actualizarAlVolverInternet = () => {
  void cargarMisSolicitudes();
};


// INICIO
onMounted(() => {
  void cargarServicios();
  void cargarMisSolicitudes();

  window.addEventListener(
    'solicitudes-sincronizadas',
    actualizarTrasSincronizacion
  );

  window.addEventListener(
    'online',
    actualizarAlVolverInternet
  );
});


// LIMPIEZA
onUnmounted(() => {
  window.removeEventListener(
    'solicitudes-sincronizadas',
    actualizarTrasSincronizacion
  );

  window.removeEventListener(
    'online',
    actualizarAlVolverInternet
  );
});
</script>

<style scoped>
ion-card-content {
  padding: 16px;
}

ion-item h2 {
  font-size: 1rem;
  margin-bottom: 8px;
}

ion-item p {
  white-space: normal;
  overflow-wrap: anywhere;
  margin: 6px 0;
}

.coordenadas {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 6px 0;
  font-size: 13px;
}

.estado-firma {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.9rem;
}

.estado-firma.registrada {
  color: var(--ion-color-success);
}

.estado-firma.pendiente {
  color: var(--ion-color-warning-shade);
}
</style>
