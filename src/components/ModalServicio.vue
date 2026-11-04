
<script setup lang="ts">
import { ref, watch } from 'vue';

import {
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonItem,
  IonLabel,
  IonInput,
  IonButton,
  IonButtons,
  IonToggle,
  IonIcon,
  toastController
} from '@ionic/vue';

import { servicios_store, Servicio } from '@/stores/servicios_store';
import { tomarFotoParaServicio } from '@/servicies/camara_service';
import { descargarYCompartirPdf } from '@/servicies/compartir_service';
import { vibrar_error } from '@/servicies/vibracion_service';
import { obtener_api_url } from '@/config/debug';

import {
  shareSocialOutline,
  imagesOutline
} from 'ionicons/icons';

const props = defineProps<{
  abierto: boolean;
  servicioEditar?: Servicio | null;
}>();

const emit = defineEmits(['cerrar']);

const nombre = ref('');
const descripcion = ref('');
const dias = ref('');
const horario = ref('');
const requiereSolicitud = ref(false);

// Estados de la fotografía
const fotoUrl = ref<string | null>(null);
const fotoArchivo = ref<File | null>(null);
const archivoFotoInput = ref<HTMLInputElement | null>(null);

const errorServidor = ref<string | null>(null);
const guardando = ref(false);

// ==========================================
// CONVERTIR RUTA DE FOTO EN URL COMPLETA
// ==========================================

const obtenerUrlFoto = (ruta: string | null | undefined): string | null => {
  if (!ruta) return null;

  // Si ya es una URL completa, se conserva.
  if (
    ruta.startsWith('http://') ||
    ruta.startsWith('https://') ||
    ruta.startsWith('blob:') ||
    ruta.startsWith('data:')
  ) {
    return ruta;
  }

  // Si es una ruta relativa, se agrega la dirección de la API.
  const baseApi = obtener_api_url().replace(/\/api\/?$/i, '');

  return `${baseApi}${ruta.startsWith('/') ? '' : '/'}${ruta}`;
};

// ==========================================
// CARGAR DATOS AL ABRIR EL MODAL
// ==========================================

watch(
  () => [props.abierto, props.servicioEditar] as const,
  ([abierto, nuevo]) => {
    // Esperar hasta que el modal esté abierto.
    if (!abierto) return;

    if (nuevo) {
      nombre.value = nuevo.nombre || nuevo.titulo || '';
      descripcion.value = nuevo.descripcion || nuevo.detalle || '';
      dias.value = nuevo.dias || '';
      horario.value = nuevo.horario || '';
      requiereSolicitud.value = nuevo.requiere_solicitud || false;

      // Recuperar la fotografía existente.
      const servicio = nuevo as any;

      const rutaFoto =
        servicio.foto_url ||
        servicio.fotoUrl ||
        servicio.foto ||
        null;

      fotoUrl.value = obtenerUrlFoto(rutaFoto);

      // No reemplazar la fotografía si no se seleccionó otra.
      fotoArchivo.value = null;

    } else {
      // Formulario nuevo.
      nombre.value = '';
      descripcion.value = '';
      dias.value = '';
      horario.value = '';
      requiereSolicitud.value = false;
      fotoUrl.value = null;
      fotoArchivo.value = null;
    }

    errorServidor.value = null;
  },
  {
    immediate: true,
    deep: true
  }
);

// ==========================================
// TOMAR FOTO CON LA CÁMARA
// ==========================================

const capturarFoto = async () => {
  errorServidor.value = null;

  try {
    const rutaWeb = await tomarFotoParaServicio();

    if (rutaWeb) {
      fotoUrl.value = rutaWeb;

      const response = await fetch(rutaWeb);
      const blob = await response.blob();

      fotoArchivo.value = new File(
        [blob],
        `servicio_${Date.now()}.jpg`,
        { type: blob.type || 'image/jpeg' }
      );
    }
  } catch (err) {
    console.error('Error al procesar la foto de la cámara:', err);

    errorServidor.value =
      'No se pudo obtener la fotografía de la cámara.';
  }
};

// ==========================================
// SELECCIONAR FOTO DESDE PC O DISPOSITIVO
// ==========================================

const abrirSelectorFotos = () => {
  archivoFotoInput.value?.click();
};

const seleccionarFoto = (evento: Event) => {
  const input = evento.target as HTMLInputElement;
  const archivo = input.files?.[0];

  if (!archivo) return;

  errorServidor.value = null;

  const formatosPermitidos = [
    'image/jpeg',
    'image/png',
    'image/webp'
  ];

  if (!formatosPermitidos.includes(archivo.type)) {
    errorServidor.value =
      'Formato no válido. Seleccioná una imagen JPG, PNG o WEBP.';

    input.value = '';
    return;
  }

  if (archivo.size > 5 * 1024 * 1024) {
    errorServidor.value =
      'La fotografía no puede superar los 5 MB.';

    input.value = '';
    return;
  }

  fotoArchivo.value = archivo;

  // Vista previa de la nueva fotografía.
  fotoUrl.value = URL.createObjectURL(archivo);

  input.value = '';
};

// ==========================================
// GUARDAR SERVICIO
// ==========================================

const guardar = async () => {
  guardando.value = true;
  errorServidor.value = null;

  try {
    const formData = new FormData();

    formData.append('Nombre', nombre.value);
    formData.append('Descripcion', descripcion.value);
    formData.append('Dias', dias.value);
    formData.append('Horario', horario.value);

    formData.append(
      'RequiereSolicitud',
      String(requiereSolicitud.value)
    );

    // Solo enviar foto si se seleccionó una nueva.
    // Si no se selecciona otra, el backend conserva la existente.
    if (fotoArchivo.value) {
      formData.append('foto', fotoArchivo.value);
    }

    if (props.servicioEditar?.id) {
      await servicios_store.editar_servicio(
        props.servicioEditar.id,
        formData as any
      );
    } else {
      await servicios_store.crear_servicio(formData as any);
    }

    emit('cerrar');

  } catch (err: any) {
    errorServidor.value =
      err?.message ||
      'Error de validación en el servidor.';

  } finally {
    guardando.value = false;
  }
};

// ==========================================
// COMPARTIR PDF
// ==========================================

const manejarCompartirPdf = async (
  id: number | undefined,
  nombreServicio: string
) => {
  if (!id) return;

  try {
    await descargarYCompartirPdf(id, nombreServicio);

  } catch (err: any) {
    if (err?.response?.status === 409 || err?.status === 409) {
      const toast = await toastController.create({
        message:
          'No se puede generar el PDF: el registro se encuentra en borrador.',
        duration: 3000,
        color: 'warning',
        position: 'bottom'
      });

      await toast.present();

    } else {
      const toast = await toastController.create({
        message:
          'Ocurrió un error al intentar generar o compartir el PDF.',
        duration: 3000,
        color: 'danger',
        position: 'bottom'
      });

      await toast.present();
    }

    vibrar_error();
  }
};
</script>

<template>
  <ion-modal
    :is-open="abierto"
    @didDismiss="emit('cerrar')"
  >
    <ion-header>
      <ion-toolbar color="primary">

        <ion-title>
          {{ servicioEditar ? 'Editar' : 'Nuevo' }} Servicio
        </ion-title>

        <ion-buttons slot="end">
          <ion-button @click="emit('cerrar')">
            CERRAR
          </ion-button>
        </ion-buttons>

      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">

      <ion-item>
        <ion-label position="stacked">
          Nombre del Servicio *
        </ion-label>

        <ion-input
          v-model="nombre"
          placeholder="Ej. Recolección de Voluminosos"
        />
      </ion-item>

      <ion-item>
        <ion-label position="stacked">
          Descripción
        </ion-label>

        <ion-input
          v-model="descripcion"
          placeholder="Ej. Retiro de escombros y restos de poda"
        />
      </ion-item>

      <ion-item>
        <ion-label position="stacked">
          Días
        </ion-label>

        <ion-input
          v-model="dias"
          placeholder="Ej. Lunes y Miércoles"
        />
      </ion-item>

      <ion-item>
        <ion-label position="stacked">
          Horario
        </ion-label>

        <ion-input
          v-model="horario"
          placeholder="Ej. 08:00 a 12:00 hs"
        />
      </ion-item>

      <ion-item style="margin-top: 10px;">
        <ion-label>
          Requiere solicitud previa
        </ion-label>

        <ion-toggle
          v-model="requiereSolicitud"
          slot="end"
        />
      </ion-item>

      <!-- SECCIÓN FOTOGRÁFICA -->

      <ion-item
        lines="none"
        style="margin-top: 15px;"
      >
        <ion-label position="stacked">
          Evidencia fotográfica
        </ion-label>
      </ion-item>

      <div class="ion-padding-horizontal">

        <ion-button
          expand="block"
          color="secondary"
          @click="abrirSelectorFotos"
        >
          <ion-icon
            slot="start"
            :icon="imagesOutline"
          />

          Seleccionar fotografía
        </ion-button>

        <input
          ref="archivoFotoInput"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          hidden
          @change="seleccionarFoto"
        />

        <ion-button
          expand="block"
          color="primary"
          @click="capturarFoto"
        >
          📸 Tomar Foto / Abrir Cámara
        </ion-button>

        <!-- VISTA PREVIA DE LA FOTO EXISTENTE O NUEVA -->

        <div
          v-if="fotoUrl"
          class="contenedor-foto"
        >
          <p>Vista previa de la fotografía:</p>

          <img
            :src="fotoUrl"
            alt="Fotografía del servicio"
            @error="console.error('No se pudo cargar la fotografía:', fotoUrl)"
          />

          <ion-button
            size="small"
            fill="clear"
            color="danger"
            @click="fotoUrl = null; fotoArchivo = null"
          >
            Quitar fotografía seleccionada
          </ion-button>
        </div>

      </div>

      <!-- COMPARTIR PDF -->

      <ion-button
        v-if="servicioEditar?.id"
        expand="block"
        color="tertiary"
        class="ion-margin-top"
        @click="manejarCompartirPdf(
          Number(servicioEditar.id),
          servicioEditar.nombre || nombre
        )"
      >
        <ion-icon
          slot="start"
          :icon="shareSocialOutline"
        />

        Generar y Compartir Comprobante PDF
      </ion-button>

      <!-- ERROR -->

      <div
        v-if="errorServidor"
        class="alerta-error"
      >
        {{ errorServidor }}
      </div>

      <!-- GUARDAR -->

      <ion-button
        expand="block"
        class="ion-margin-top"
        :disabled="guardando"
        @click="guardar"
      >
        {{ guardando ? 'GUARDANDO...' : 'GUARDAR' }}
      </ion-button>

    </ion-content>
  </ion-modal>
</template>

<style scoped>
.alerta-error {
  margin-top: 16px;
  padding: 12px;
  background-color: rgba(235, 68, 90, 0.12);
  border-left: 4px solid var(--ion-color-danger, #eb445a);
  color: var(--ion-color-danger, #eb445a);
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
}

.contenedor-foto {
  margin-top: 12px;
  text-align: center;
}

.contenedor-foto p {
  font-size: 14px;
  font-weight: 500;
}

.contenedor-foto img {
  max-height: 200px;
  max-width: 100%;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid var(--ion-color-step-300, #ccc);
}
</style>
