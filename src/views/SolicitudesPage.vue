<template>
  <ion-page>
    <!-- Encabezado -->
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>
        <ion-title>Solicitudes de Retiro</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <!-- Actualizar historial -->
      <ion-refresher
        slot="fixed"
        @ionRefresh="hacerRecarga($event)"
      >
        <ion-refresher-content
          pulling-text="Deslizá para actualizar..."
          refreshing-spinner="circles"
          refreshing-text="Cargando solicitudes..."
        />
      </ion-refresher>

      <!-- Formulario de solicitud -->
      <h2 class="app-seccion-titulo">
        Nueva Solicitud
      </h2>

      <ion-card class="app-card">
        <ion-card-content>
          <form
            class="app-formulario"
            @submit.prevent="enviarSolicitud"
          >
            <!-- Tipo de residuo -->
            <ion-item lines="full">
              <ion-select
                v-model="form.tipo"
                label="Tipo de Residuo Especial"
                label-placement="stacked"
                placeholder="Seleccione una opción"
                required
              >
                <ion-select-option value="Poda y Escombros">
                  Poda y Escombros
                </ion-select-option>

                <ion-select-option value="Electrodomésticos / Voluminosos">
                  Electrodomésticos / Voluminosos
                </ion-select-option>

                <ion-select-option value="Aceite Vegetal Usado">
                  Aceite Vegetal Usado
                </ion-select-option>
              </ion-select>
            </ion-item>

            <!-- Dirección de retiro -->
            <ion-item lines="full">
              <ion-input
                v-model="form.direccion"
                label="Dirección de Retiro"
                label-placement="stacked"
                placeholder="Ej: Av. Mitre 1234"
                required
                @ionInput="limpiarCoordenadas"
              />
            </ion-item>

            <!-- Abrir mapa -->
            <ion-button
              expand="block"
              fill="outline"
              class="app-boton ion-margin-top"
              type="button"
              @click="modalUbicacionAbierto = true"
            >
              <ion-icon
                :icon="locationOutline"
                slot="start"
              />
              Seleccionar ubicación en el mapa
            </ion-button>

            <!-- Mostrar coordenadas elegidas -->
            <ion-note
              v-if="form.latitud !== null && form.longitud !== null"
              color="success"
            >
              <p class="coordenadas-seleccionadas">
                <ion-icon :icon="checkmarkCircleOutline" />
                Ubicación seleccionada
              </p>

              <p class="coordenadas-seleccionadas">
                Latitud: {{ form.latitud.toFixed(6) }}
              </p>

              <p class="coordenadas-seleccionadas">
                Longitud: {{ form.longitud.toFixed(6) }}
              </p>
            </ion-note>

            <!-- Observaciones -->
            <ion-item lines="full">
              <ion-textarea
                v-model="form.observaciones"
                label="Observaciones / Detalles"
                label-placement="stacked"
                placeholder="Ej: 2 bolsas grandes de ramas frente al garage"
                :rows="3"
              />
            </ion-item>

            <!-- Dictar observaciones por voz -->
            <ion-button
              expand="block"
              fill="outline"
              type="button"
              class="app-boton ion-margin-top"
              :disabled="escuchandoVoz"
              @click="dictarObservaciones"
            >
              <ion-icon
                :icon="micOutline"
                slot="start"
              />

              {{
                escuchandoVoz
                  ? 'Escuchando...'
                  : 'Dictar observaciones'
              }}
            </ion-button>

            <!-- Enviar solicitud -->
            <ion-button
              expand="block"
              type="submit"
              color="primary"
              class="app-boton ion-margin-top"
            >
              <ion-icon
                :icon="sendOutline"
                slot="start"
              />
              Enviar Solicitud
            </ion-button>
          </form>
        </ion-card-content>
      </ion-card>

      <!-- Historial de solicitudes -->
      <h3 class="app-seccion-titulo">
        Mis Solicitudes
      </h3>

      <!-- Estado vacío -->
      <div v-if="solicitudes.length === 0">
        <ion-note class="app-mensaje-vacio">
          No tenés solicitudes registradas aún.
        </ion-note>
      </div>

      <!-- Lista de solicitudes -->
      <ion-list v-else lines="full">
        <ion-item
          v-for="item in solicitudes"
          :key="item.id"
          class="app-lista-item"
        >
          <ion-label>
            <h2>
              <strong>{{ item.tipo }}</strong>
            </h2>

            <p>📍 {{ item.direccion }}</p>

            <p v-if="item.observaciones">
              💬 {{ item.observaciones }}
            </p>

            <p
              v-if="item.latitud != null && item.longitud != null"
            >
              Coordenadas: {{ item.latitud.toFixed(6) }},
              {{ item.longitud.toFixed(6) }}
            </p>

            <ion-note color="medium">
              Fecha: {{ item.fecha }}
            </ion-note>
          </ion-label>

          <!-- Estado de envío -->
          <ion-badge
            slot="end"
            :color="obtenerEstado(item.estado).color"
            class="app-estado-badge"
          >
            <ion-icon
              :icon="obtenerEstado(item.estado).icono"
            />

            {{ obtenerEstado(item.estado).texto }}
          </ion-badge>
        </ion-item>
      </ion-list>
    </ion-content>

    <!-- Selector de ubicación -->
    <SelectorUbicacion
      :abierto="modalUbicacionAbierto"
      :direccion-inicial="form.direccion"
      :latitud-inicial="form.latitud"
      :longitud-inicial="form.longitud"
      @confirmar="aplicarUbicacion"
      @cerrar="modalUbicacionAbierto = false"
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
  IonBadge,
  IonNote,
  IonRefresher,
  IonRefresherContent,
  toastController
} from '@ionic/vue';

import {
  sendOutline,
  checkmarkCircleOutline,
  locationOutline,
  micOutline
} from 'ionicons/icons';

import SelectorUbicacion from '@/components/SelectorUbicacion.vue';

import {
  crear_solicitud_api
} from '@/servicies/solicitudes_service';

import {
  dictar_texto
} from '@/servicies/voz_service';

import {
  ESTADOS_SOLICITUD
} from '@/utils/estados';

interface Solicitud {
  id: number;
  tipo: string;
  direccion: string;
  observaciones: string;
  fecha: string;
  estado: 'enviada' | 'sin_enviar';
  latitud?: number | null;
  longitud?: number | null;
}

interface Ubicacion {
  direccion: string;
  latitud: number;
  longitud: number;
}

const SOLICITUDES_KEY = 'ganica_solicitudes';

const form = ref({
  tipo: '',
  direccion: '',
  observaciones: '',
  latitud: null as number | null,
  longitud: null as number | null
});

const solicitudes = ref<Solicitud[]>([]);
const modalUbicacionAbierto = ref(false);

// Estado del reconocimiento de voz.
const escuchandoVoz = ref(false);

const ESTADO_POR_DEFECTO = {
  color: 'medium',
  texto: 'En revisión',
  icono: checkmarkCircleOutline
};

const obtenerEstado = (estado: string) => {
  return (
    ESTADOS_SOLICITUD[
      estado as keyof typeof ESTADOS_SOLICITUD
    ] ?? ESTADO_POR_DEFECTO
  );
};

// Aplicar la ubicación confirmada en el mapa.
const aplicarUbicacion = (ubicacion: Ubicacion) => {
  form.value.direccion = ubicacion.direccion;
  form.value.latitud = ubicacion.latitud;
  form.value.longitud = ubicacion.longitud;
};

// Si el usuario edita manualmente la dirección,
// se eliminan las coordenadas para evitar datos incorrectos.
const limpiarCoordenadas = () => {
  form.value.latitud = null;
  form.value.longitud = null;
};

// Dictar y agregar texto a las observaciones.
const dictarObservaciones = async () => {
  if (escuchandoVoz.value) {
    return;
  }

  escuchandoVoz.value = true;

  try {
    const textoReconocido = await dictar_texto();

    if (textoReconocido.trim()) {
      const textoActual = form.value.observaciones.trim();
      const textoNuevo = textoReconocido.trim();

      form.value.observaciones = textoActual
        ? `${textoActual} ${textoNuevo}`
        : textoNuevo;

      await mostrarToast(
        'Texto agregado a las observaciones.',
        'success'
      );
    }
  } catch (error: unknown) {
    console.error(
      'Error en el reconocimiento de voz:',
      error
    );

    const mensaje =
      error instanceof Error
        ? error.message
        : 'No se pudo utilizar el reconocimiento de voz.';

    await mostrarToast(mensaje, 'warning');
  } finally {
    escuchandoVoz.value = false;
  }
};

// Cargar solicitudes guardadas localmente.
const cargarSolicitudes = () => {
  const guardadas = localStorage.getItem(SOLICITUDES_KEY);

  if (guardadas) {
    try {
      solicitudes.value = JSON.parse(guardadas);
    } catch (error) {
      console.error(
        'Error al cargar las solicitudes guardadas:',
        error
      );

      solicitudes.value = [];
    }
  } else {
    solicitudes.value = [];
  }
};

// Actualizar historial después de la sincronización.
const actualizarDesdeSincronizacion = () => {
  cargarSolicitudes();
};

// Recargar historial mediante el gesto de Ionic.
const hacerRecarga = async (event: CustomEvent) => {
  cargarSolicitudes();

  setTimeout(() => {
    event.detail.complete();
  }, 500);
};

// Registrar eventos de sincronización.
onMounted(() => {
  cargarSolicitudes();

  window.addEventListener(
    'solicitudes-sincronizadas',
    actualizarDesdeSincronizacion
  );
});

// Eliminar evento al salir de la vista.
onUnmounted(() => {
  window.removeEventListener(
    'solicitudes-sincronizadas',
    actualizarDesdeSincronizacion
  );
});

// Limpiar formulario.
const limpiarFormulario = () => {
  form.value = {
    tipo: '',
    direccion: '',
    observaciones: '',
    latitud: null,
    longitud: null
  };
};

// Mostrar notificación.
const mostrarToast = async (
  mensaje: string,
  color: 'success' | 'warning' | 'danger'
) => {
  const toast = await toastController.create({
    message: mensaje,
    duration: 2500,
    color,
    position: 'bottom'
  });

  await toast.present();
};

// Enviar solicitud.
const enviarSolicitud = async () => {
  if (
    !form.value.tipo ||
    !form.value.direccion.trim()
  ) {
    await mostrarToast(
      'Completá el tipo de residuo y la dirección.',
      'warning'
    );

    return;
  }

  // Copiamos los datos antes de enviar para mantener
  // la misma información en caso de un error de conexión.
  const datosLocales = {
    tipo: form.value.tipo,
    direccion: form.value.direccion.trim(),
    observaciones: form.value.observaciones,
    latitud: form.value.latitud,
    longitud: form.value.longitud
  };

  try {
    const datosSolicitud = {
      servicioRecoleccionId: 152,
      tipoResiduo: datosLocales.tipo,
      direccion: datosLocales.direccion,
      observaciones: datosLocales.observaciones,
      latitud: datosLocales.latitud,
      longitud: datosLocales.longitud
    };

    console.log(
      'Enviando solicitud:',
      datosSolicitud
    );

    const response = await crear_solicitud_api(
      datosSolicitud
    );

    console.log(
      'Solicitud creada:',
      response
    );

    const nuevaSolicitud: Solicitud = {
      id: response.id,
      ...datosLocales,
      fecha: new Date().toLocaleDateString('es-AR'),
      estado: 'enviada'
    };

    solicitudes.value.unshift(nuevaSolicitud);

    localStorage.setItem(
      SOLICITUDES_KEY,
      JSON.stringify(solicitudes.value)
    );

    limpiarFormulario();

    await mostrarToast(
      '¡Solicitud registrada y enviada a los recolectores!',
      'success'
    );
  } catch (error: unknown) {
    console.error(
      'Error al enviar solicitud:',
      error
    );

    const mensajeError =
      error instanceof Error
        ? error.message
        : '';

    if (
      mensajeError.includes('Sin conexión') ||
      mensajeError.includes('Tiempo de espera')
    ) {
      const solicitudPendiente: Solicitud = {
        id: Date.now(),
        ...datosLocales,
        fecha: new Date().toLocaleDateString('es-AR'),
        estado: 'sin_enviar'
      };

      solicitudes.value.unshift(solicitudPendiente);

      localStorage.setItem(
        SOLICITUDES_KEY,
        JSON.stringify(solicitudes.value)
      );

      limpiarFormulario();

      await mostrarToast(
        'Sin conexión. Solicitud guardada como "Sin enviar".',
        'warning'
      );

      return;
    }

    await mostrarToast(
      'No se pudo registrar la solicitud.',
      'danger'
    );
  }
};
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

ion-badge {
  flex-shrink: 0;
  margin-inline-start: 8px;
}

.app-estado-badge ion-icon {
  font-size: 15px;
}

.coordenadas-seleccionadas {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 6px 0;
  font-size: 13px;
}
</style>
