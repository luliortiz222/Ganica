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
    } else {
      nombre.value = '';
      descripcion.value = '';
      dias.value = '';
      horario.value = '';
      requiereSolicitud.value = false;
    }
    errorServidor.value = null;
  },
  { immediate: true }
);

const guardar = async () => {
  guardando.value = true;
  errorServidor.value = null;

  try {
    const payload = {
      nombre: nombre.value,
      descripcion: descripcion.value,
      dias: dias.value,
      horario: horario.value,
      requiere_solicitud: requiereSolicitud.value
    };

    if (props.servicioEditar?.id) {
      await servicios_store.editar_servicio(props.servicioEditar.id, payload);
    } else {
      await servicios_store.crear_servicio(payload);
    }

    // Si el servidor responde OK (200/201), cerramos el modal
    emit('cerrar');
  } catch (err: any) {
    // Si el servidor responde 400 u otro error, el modal PERMANECE ABIERTO
    // y muestra el mensaje de error devuelto por el backend
    errorServidor.value = err?.message || 'Error de validación en el servidor.';
  } finally {
    guardando.value = false;
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

      <!-- Cartel de alerta del servidor (Validación Un. 4) -->
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
</style>