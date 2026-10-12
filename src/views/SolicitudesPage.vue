
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

            <ion-item lines="full">
              <ion-input
                v-model="form.direccion"
                label="Dirección de Retiro"
                label-placement="stacked"
                placeholder="Ej: Av. Mitre 1234"
                required
              />
            </ion-item>

            <ion-item lines="full">
              <ion-textarea
                v-model="form.observaciones"
                label="Observaciones / Detalles"
                label-placement="stacked"
                placeholder="Ej: 2 bolsas grandes de ramas frente al garage"
                :rows="3"
              />
            </ion-item>

            <ion-button
              expand="block"
              type="submit"
              color="primary"
              class="app-boton"
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

import { sendOutline, checkmarkCircleOutline  } from 'ionicons/icons';

import {
  crear_solicitud_api
} from '@/servicies/solicitudes_service';

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
}

const SOLICITUDES_KEY = 'ganica_solicitudes';

const form = ref({
  tipo: '',
  direccion: '',
  observaciones: ''
});

const solicitudes = ref<Solicitud[]>([]);

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

// Cargar solicitudes guardadas localmente
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

// Actualizar historial después de la sincronización
const actualizarDesdeSincronizacion = () => {
  cargarSolicitudes();
};

// Recargar historial mediante el gesto de Ionic
const hacerRecarga = async (event: CustomEvent) => {
  cargarSolicitudes();

  setTimeout(() => {
    event.detail.complete();
  }, 500);
};

// Registrar eventos de sincronización
onMounted(() => {
  cargarSolicitudes();

  window.addEventListener(
    'solicitudes-sincronizadas',
    actualizarDesdeSincronizacion
  );
});

// Eliminar evento al salir de la vista
onUnmounted(() => {
  window.removeEventListener(
    'solicitudes-sincronizadas',
    actualizarDesdeSincronizacion
  );
});

// Limpiar formulario
const limpiarFormulario = () => {
  form.value = {
    tipo: '',
    direccion: '',
    observaciones: ''
  };
};

// Mostrar notificación
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

// Enviar solicitud
const enviarSolicitud = async () => {
  if (!form.value.tipo || !form.value.direccion.trim()) {
    await mostrarToast(
      'Completá el tipo de residuo y la dirección.',
      'warning'
    );

    return;
  }

  try {
    const datosSolicitud = {
      servicioRecoleccionId: 152,
      tipoResiduo: form.value.tipo,
      direccion: form.value.direccion.trim(),
      observaciones: form.value.observaciones
    };

    console.log('Enviando solicitud:', datosSolicitud);

    const response = await crear_solicitud_api(datosSolicitud);

    console.log('Solicitud creada:', response);

    const nuevaSolicitud: Solicitud = {
      id: response.id,
      tipo: form.value.tipo,
      direccion: form.value.direccion,
      observaciones: form.value.observaciones,
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
    console.error('Error al enviar solicitud:', error);

    const mensajeError =
      error instanceof Error ? error.message : '';

    if (
      mensajeError.includes('Sin conexión') ||
      mensajeError.includes('Tiempo de espera')
    ) {
      const solicitudPendiente: Solicitud = {
        id: Date.now(),
        tipo: form.value.tipo,
        direccion: form.value.direccion,
        observaciones: form.value.observaciones,
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
/* Ajustes específicos de esta vista */

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
</style>
