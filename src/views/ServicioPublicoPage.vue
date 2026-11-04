
<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Servicio</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <div v-if="cargando" class="estado">
        Cargando servicio...
      </div>

      <div v-else-if="error" class="estado error">
        {{ error }}
      </div>

      <div v-else-if="servicio" class="servicio-contenido">

        <div v-if="obtenerFotoServicio()" class="foto-servicio">
          <img
            :src="obtenerFotoServicio()!"
            :alt="`Fotografía de ${servicio.nombre}`"
          />
        </div>

        <h2>{{ servicio.nombre }}</h2>

        <p>{{ servicio.descripcion }}</p>

        <p v-if="servicio.dias">
          <strong>Días:</strong>
          {{ servicio.dias }}
        </p>

        <p v-if="servicio.horario">
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
  foto_url?: string | null;
}

const route = useRoute();

const servicio = ref<Servicio | null>(null);
const cargando = ref(true);
const error = ref('');

const obtenerFotoServicio = (): string | null => {
  const ruta = servicio.value?.foto_url;

  if (!ruta) {
    return null;
  }

  if (/^https?:\/\//i.test(ruta)) {
    return ruta;
  }

  const baseApi = obtener_api_url().replace(/\/api\/?$/i, '');

  return `${baseApi}${ruta.startsWith('/') ? '' : '/'}${ruta}`;
};

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

    error.value =
      err?.message || 'No se pudo obtener el servicio.';

  } finally {
    cargando.value = false;
  }
});
</script>

<style scoped>
.estado {
  padding: 24px 12px;
  text-align: center;
}

.error {
  color: var(--ion-color-danger);
}

.servicio-contenido h2 {
  margin-top: 18px;
  font-size: 1.5rem;
  font-weight: 700;
}

.servicio-contenido p {
  line-height: 1.6;
}

.foto-servicio {
  width: 100%;
  margin-bottom: 16px;
  overflow: hidden;
  border-radius: 14px;
}

.foto-servicio img {
  display: block;
  width: 100%;
  max-height: 280px;
  object-fit: cover;
  border-radius: 14px;
}
</style>
