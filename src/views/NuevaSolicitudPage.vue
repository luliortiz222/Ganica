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
            <ion-item lines="full">
              <ion-select
                v-model="form.tipo"
                label="Tipo de residuo especial"
                label-placement="stacked"
                placeholder="Seleccioná una opción"
                required
              >
                <ion-select-option value="Poda y Escombros">
                  Poda y Escombros
                </ion-select-option>

                <ion-select-option value="Electrodomésticos-Voluminosos">
                  Electrodomésticos y voluminosos
                </ion-select-option>

                <ion-select-option value="Aceite Vegetal Usado">
                  Aceite vegetal usado
                </ion-select-option>
              </ion-select>
            </ion-item>

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

            <ion-item lines="full">
              <ion-textarea
                v-model="form.observaciones"
                label="Observaciones y detalles"
                label-placement="stacked"
                placeholder="Ej.: ramas en bolsas frente al garage"
                :rows="3"
              />
            </ion-item>

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

            <ion-button
              expand="block"
              type="submit"
              class="ion-margin-top"
              :disabled="enviando"
            >
              <ion-icon :icon="sendOutline" slot="start" />
              {{ enviando ? 'Enviando...' : 'Enviar solicitud' }}
            </ion-button>
          </form>
        </ion-card-content>
      </ion-card>

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
            <h2><strong>{{ item.tipoResiduo }}</strong></h2>

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

            <!-- Estado de la conformidad -->
            <p v-if="tieneFirmaRegistrada(item)" class="estado-firma registrada">
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

            <!-- Solo solicitudes reales del servidor pueden firmarse.
                 Las solicitudes guardadas localmente aún no se pueden firmar. -->
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

    <SelectorUbicacion
      :abierto="modalUbicacionAbierto"
      :direccion-inicial="form.direccion"
      :latitud-inicial="form.latitud"
      :longitud-inicial="form.longitud"
      @confirmar="aplicarUbicacion"
      @cerrar="modalUbicacionAbierto = false"
    />

    <!-- Firma del vecino -->
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
import { ref, onMounted, onUnmounted } from 'vue';

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

import { dictar_texto } from '@/servicies/voz_service';

interface Ubicacion {
  direccion: string;
  latitud: number;
  longitud: number;
}

interface SolicitudLocal {
  id: number;
  tipoResiduo: string;
  direccion: string;
  observaciones: string;
  fechaCreacion: string;
  estado: string;
}

interface SolicitudConFirma extends SolicitudApi {
  firmaConformidadArchivo?: string | null;
  firmaConformidadFecha?: string | null;
}

const form = ref({
  tipo: '',
  direccion: '',
  observaciones: '',
  latitud: null as number | null,
  longitud: null as number | null
});

const misSolicitudes = ref<SolicitudApi[]>([]);
const cargando = ref(false);
const enviando = ref(false);
const escuchandoVoz = ref(false);
const modalUbicacionAbierto = ref(false);

const solicitudParaFirma = ref<SolicitudApi | null>(null);
const modalFirmaAbierto = ref(false);

const CLAVE_LOCAL = 'ganica_solicitudes_vecino';

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

const aplicarUbicacion = (ubicacion: Ubicacion) => {
  form.value.direccion = ubicacion.direccion;
  form.value.latitud = ubicacion.latitud;
  form.value.longitud = ubicacion.longitud;
};

const limpiarCoordenadas = () => {
  form.value.latitud = null;
  form.value.longitud = null;
};

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
    console.error('Error al dictar observaciones:', error);

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

const guardarPendienteLocal = (solicitud: SolicitudLocal) => {
  let guardadas: SolicitudLocal[] = [];

  try {
    guardadas = JSON.parse(
      localStorage.getItem(CLAVE_LOCAL) || '[]'
    );
  } catch {
    guardadas = [];
  }

  guardadas.unshift(solicitud);

  localStorage.setItem(
    CLAVE_LOCAL,
    JSON.stringify(guardadas)
  );
};

const cargarMisSolicitudes = async () => {
  cargando.value = true;

  try {
    const remotas = await listar_solicitudes_api();

    let locales: SolicitudLocal[] = [];

    try {
      locales = JSON.parse(
        localStorage.getItem(CLAVE_LOCAL) || '[]'
      );
    } catch {
      locales = [];
    }

    const idsRemotos = new Set(remotas.map(s => s.id));

    misSolicitudes.value = [
      ...locales
        .filter(s => !idsRemotos.has(s.id))
        .map(s => ({
          id: s.id,
          servicioRecoleccionId: 0,
          tipoResiduo: s.tipoResiduo,
          direccion: s.direccion,
          observaciones: s.observaciones,
          fechaCreacion: s.fechaCreacion,
          estado: s.estado
        }) as SolicitudApi),
      ...remotas
    ];
  } catch (error) {
    console.error('Error al cargar solicitudes:', error);

    try {
      const locales: SolicitudLocal[] = JSON.parse(
        localStorage.getItem(CLAVE_LOCAL) || '[]'
      );

      misSolicitudes.value = locales.map(s => ({
        id: s.id,
        servicioRecoleccionId: 0,
        tipoResiduo: s.tipoResiduo,
        direccion: s.direccion,
        observaciones: s.observaciones,
        fechaCreacion: s.fechaCreacion,
        estado: s.estado
      })) as SolicitudApi[];
    } catch {
      misSolicitudes.value = [];
    }
  } finally {
    cargando.value = false;
  }
};

const limpiarFormulario = () => {
  form.value = {
    tipo: '',
    direccion: '',
    observaciones: '',
    latitud: null,
    longitud: null
  };
};

const enviarSolicitud = async () => {
  if (enviando.value) return;

  if (!form.value.tipo || !form.value.direccion.trim()) {
    await mostrarToast(
      'Completá el tipo de residuo y la dirección.',
      'warning'
    );
    return;
  }

  enviando.value = true;

  const datos = {
    tipo: form.value.tipo,
    direccion: form.value.direccion.trim(),
    observaciones: form.value.observaciones.trim(),
    latitud: form.value.latitud,
    longitud: form.value.longitud
  };

  try {
    const respuesta = await crear_solicitud_api({
      servicioRecoleccionId: 0,
      tipoResiduo: datos.tipo,
      direccion: datos.direccion,
      observaciones: datos.observaciones,
      latitud: datos.latitud,
      longitud: datos.longitud
    });

    // La respuesta se obtiene del servidor; recargamos la lista.
    void respuesta;

    limpiarFormulario();

    await cargarMisSolicitudes();

    await mostrarToast(
      'Solicitud registrada correctamente.',
      'success'
    );
  } catch (error) {
    console.error('Error al crear solicitud:', error);

    const mensaje = error instanceof Error
      ? error.message
      : '';

    if (
      mensaje.includes('Sin conexión') ||
      mensaje.includes('Tiempo de espera')
    ) {
      guardarPendienteLocal({
        id: Date.now(),
        tipoResiduo: datos.tipo,
        direccion: datos.direccion,
        observaciones: datos.observaciones,
        fechaCreacion: new Date().toISOString(),
        estado: 'Sin enviar'
      });

      limpiarFormulario();

      await mostrarToast(
        'Sin conexión. La solicitud quedó guardada localmente.',
        'warning'
      );
    } else {
      await mostrarToast(
        'No se pudo registrar la solicitud. Revisá tu conexión e intentá nuevamente.',
        'danger'
      );
    }

    await cargarMisSolicitudes();
  } finally {
    enviando.value = false;
  }
};

const normalizarEstado = (estado: string | null | undefined) => {
  return (estado || '').trim().toLocaleLowerCase('es-AR');
};

const estaEnCamino = (item: SolicitudApi) => {
  return normalizarEstado(item.estado) === 'en camino';
};

const tieneFirmaRegistrada = (item: SolicitudApi) => {
  const solicitud = item as SolicitudConFirma;

  return Boolean(
    solicitud.firmaConformidadArchivo ||
    solicitud.firmaConformidadFecha
  );
};

const puedeFirmar = (item: SolicitudApi) => {
  return (
    estaEnCamino(item) &&
    !tieneFirmaRegistrada(item) &&
    item.servicioRecoleccionId !== 0
  );
};

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

const cerrarFirma = () => {
  modalFirmaAbierto.value = false;
  solicitudParaFirma.value = null;
};

const finalizarFirma = async () => {
  modalFirmaAbierto.value = false;
  solicitudParaFirma.value = null;

  await cargarMisSolicitudes();

  await mostrarToast(
    'Conformidad firmada y registrada correctamente.',
    'success'
  );
};

const formatearFecha = (fecha: string) => {
  if (!fecha) return 'Sin fecha';

  const valor = new Date(fecha);

  if (Number.isNaN(valor.getTime())) return fecha;

  return valor.toLocaleDateString('es-AR');
};

const actualizarTrasSincronizacion = () => {
  void cargarMisSolicitudes();
};

onMounted(() => {
  void cargarMisSolicitudes();

  window.addEventListener(
    'solicitudes-sincronizadas',
    actualizarTrasSincronizacion
  );
});

onUnmounted(() => {
  window.removeEventListener(
    'solicitudes-sincronizadas',
    actualizarTrasSincronizacion
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