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
  IonToggle
} from '@ionic/vue';
import { servicios_store, Servicio } from '@/stores/servicios_store';
import { tomarFotoParaServicio } from '@/servicies/camara_service';
import { descargarYCompartirPdf } from '@/servicies/compartir_service';
import { vibrar_error } from '@/servicies/vibracion_service';
import { toastController, IonIcon } from '@ionic/vue';
import { shareSocialOutline } from 'ionicons/icons';

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

// Estados para la foto de la Unidad 6
const fotoUrl = ref<string | null>(null);
const fotoArchivo = ref<File | null>(null);

const errorServidor = ref<string | null>(null);
const guardando = ref(false);

// Sincroniza los campos cuando se abre para editar o crear
watch(
  () => props.servicioEditar,
  (nuevo) => {
    if (nuevo) {
      nombre.value = nuevo.nombre || nuevo.titulo || '';
      descripcion.value = nuevo.descripcion || nuevo.detalle || '';
      dias.value = nuevo.dias || '';
      horario.value = nuevo.horario || '';
      requiereSolicitud.value = nuevo.requiere_solicitud || false;
      // Si ya tiene foto guardada en el servidor
      fotoUrl.value = (nuevo as any).foto_url || (nuevo as any).foto || null;
    } else {
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
  { immediate: true }
);

// Función para disparar la cámara y manejar permisos nativos
const capturarFoto = async () => {
  const rutaWeb = await tomarFotoParaServicio();
  if (rutaWeb) {
    fotoUrl.value = rutaWeb;
    try {
      const response = await fetch(rutaWeb);
      const blob = await response.blob();
      fotoArchivo.value = new File([blob], `servicio_${Date.now()}.jpg`, { type: 'image/jpeg' });
    } catch (err) {
      console.error('Error al procesar la foto de la cámara:', err);
    }
  }
};

const guardar = async () => {
  guardando.value = true;
  errorServidor.value = null;

  try {
    // Usamos FormData para empaquetar textos y el archivo binario de la foto
    const formData = new FormData();
    formData.append('Nombre', nombre.value);
    formData.append('Descripcion', descripcion.value);
    formData.append('Dias', dias.value);
    formData.append('Horario', horario.value);
    formData.append('RequiereSolicitud', String(requiereSolicitud.value));

    if (fotoArchivo.value) {
      formData.append('foto', fotoArchivo.value);
    }

    if (props.servicioEditar?.id) {
      await servicios_store.editar_servicio(props.servicioEditar.id, formData as any);
    } else {
      await servicios_store.crear_servicio(formData as any);
    }

    // Si el servidor responde OK, cerramos el modal
    emit('cerrar');
  } catch (err: any) {
    errorServidor.value = err?.message || 'Error de validación en el servidor.';
  } finally {
    guardando.value = false;
  }
};

const manejarCompartirPdf = async (id: number | undefined, nombreServicio: string) => {
  if (!id) return;
  try {
    await descargarYCompartirPdf(id, nombreServicio);
  } catch (err: any) {
    if (err?.response?.status === 409 || err?.status === 409) {
      const toast = await toastController.create({
        message: 'No se puede generar el PDF: el registro se encuentra en borrador.',
        duration: 3000,
        color: 'warning',
        position: 'bottom'
      });
      await toast.present();
    } else {
      const toast = await toastController.create({
        message: 'Ocurrió un error al intentar generar o compartir el PDF.',
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
  <ion-modal :is-open="abierto" @didDismiss="emit('cerrar')">
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>{{ servicioEditar ? 'Editar' : 'Nuevo' }} Servicio</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="emit('cerrar')">CERRAR</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <ion-item>
        <ion-label position="stacked">Nombre del Servicio *</ion-label>
        <ion-input v-model="nombre" placeholder="Ej. Recolección de Voluminosos"></ion-input>
      </ion-item>

      <ion-item>
        <ion-label position="stacked">Descripción</ion-label>
        <ion-input v-model="descripcion" placeholder="Ej. Retiro de escombros y restos de poda"></ion-input>
      </ion-item>

      <ion-item>
        <ion-label position="stacked">Días</ion-label>
        <ion-input v-model="dias" placeholder="Ej. Lunes y Miércoles"></ion-input>
      </ion-item>

      <ion-item>
        <ion-label position="stacked">Horario</ion-label>
        <ion-input v-model="horario" placeholder="Ej. 08:00 a 12:00 hs"></ion-input>
      </ion-item>

      <ion-item style="margin-top: 10px;">
        <ion-label>Requiere solicitud previa</ion-label>
        <ion-toggle v-model="requiereSolicitud" slot="end"></ion-toggle>
      </ion-item>

      <!-- SECCIÓN CÁMARA (Unidad 6) -->
      <ion-item lines="none" style="margin-top: 15px;">
        <ion-label position="stacked">Evidencia fotográfica</ion-label>
      </ion-item>
      
      <div class="ion-padding-horizontal">
        <ion-button expand="block" color="secondary" @click="capturarFoto">
          📸 Tomar Foto / Abrir Cámara
        </ion-button>

        <!-- Vista previa de la foto capturada -->
        <div v-if="fotoUrl" class="contenedor-foto">
          <img :src="fotoUrl" alt="Vista previa de foto" />
        </div>
      </div>

      <ion-button 
        v-if="servicioEditar?.id" 
        expand="block" 
        color="tertiary" 
        class="ion-margin-top" 
        @click="manejarCompartirPdf(Number(servicioEditar.id), servicioEditar.nombre || nombre)">
        <ion-icon slot="start" :icon="shareSocialOutline"></ion-icon>
        Generar y Compartir Comprobante PDF
      </ion-button>

      <!-- Cartel de alerta del servidor -->
      <div v-if="errorServidor" class="alerta-error">
        {{ errorServidor }}
      </div>

      <ion-button expand="block" class="ion-margin-top" :disabled="guardando" @click="guardar">
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

.contenedor-foto img {
  max-height: 160px;
  border-radius: 8px;
  object-fit: cover;
  border: 1px solid var(--ion-color-step-300, #ccc);
}
</style>