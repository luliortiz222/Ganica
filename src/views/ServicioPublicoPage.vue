<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-title>Servicio</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <div v-if="cargando">
        Cargando servicio...
      </div>

      <div v-else-if="error">
        {{ error }}
      </div>

      <div v-else-if="servicio">
        <h2>{{ servicio.nombre }}</h2>

        <p>{{ servicio.descripcion }}</p>

        <p>
          <strong>Días:</strong>
          {{ servicio.dias }}
        </p>

        <p>
          <strong>Horario:</strong>
          {{ servicio.horario }}
        </p>

        <p>
          <strong>Requiere solicitud:</strong>
          {{ servicio.requiere_solicitud ? 'Sí' : 'No' }}
        </p>
      </div>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent
} from '@ionic/vue';
import { useRoute } from 'vue-router';
import { obtener_api_url } from '@/config/debug';

interface Servicio {
  id: number;
  nombre: string;
  descripcion: string;
  dias: string;
  horario: string;
  requiere_solicitud: boolean;
}

const route = useRoute();

const servicio = ref<Servicio | null>(null);
const cargando = ref(true);
const error = ref('');

onMounted(async () => {
  try {
    const id = route.params.id;

    const response = await fetch(
      `${obtener_api_url()}/servicios/${id}`
    );

    if (!response.ok) {
      throw new Error(`Error HTTP: ${response.status}`);
    }

    servicio.value = await response.json();
  } catch (err: any) {
    console.error('Error al obtener servicio:', err);
    error.value = err?.message || 'No se pudo obtener el servicio.';
  } finally {
    cargando.value = false;
  }
});
</script>