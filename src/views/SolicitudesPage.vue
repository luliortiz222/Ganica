<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>
        <ion-title>Solicitudes de Retiro</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <ion-refresher slot="fixed" @ionRefresh="hacerRecarga($event)">
        <ion-refresher-content 
          pulling-text="Deslizá para actualizar..." 
          refreshing-spinner="circles" 
          refreshing-text="Cargando solicitudes...">
        </ion-refresher-content>
      </ion-refresher>

      <h2 class="app-titulo-ganica">Nueva Solicitud</h2>

      <!-- Formulario de Solicitud -->
      <ion-card>
        <ion-card-content>
          <form @submit.prevent="enviarSolicitud">
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

            <ion-button expand="block" type="submit" color="primary" class="ion-margin-top">
              <ion-icon :icon="sendOutline" slot="start" />
              Enviar Solicitud
            </ion-button>
          </form>
        </ion-card-content>
      </ion-card>

      <!-- Historial de Solicitudes -->
      <h3 class="ion-margin-top">Mis Solicitudes</h3>

      <div v-if="solicitudes.length === 0">
        <ion-note class="ion-padding-start">No tenés solicitudes registradas aún.</ion-note>
      </div>

      <ion-list v-else lines="full">
        <ion-item v-for="item in solicitudes" :key="item.id">
          <ion-label>
            <h2><strong>{{ item.tipo }}</strong></h2>
            <p>📍 {{ item.direccion }}</p>
            <p v-if="item.observaciones">💬 {{ item.observaciones }}</p>
            <ion-note color="medium">Fecha: {{ item.fecha }}</ion-note>
          </ion-label>
          <ion-badge slot="end" color="warning">Pendiente</ion-badge>
        </ion-item>
      </ion-list>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { 
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, 
  IonButtons, IonMenuButton, IonCard, IonCardContent, 
  IonItem, IonLabel, IonInput, IonTextarea, IonSelect, 
  IonSelectOption, IonButton, IonIcon, IonList, IonBadge, 
  IonNote, IonRefresher, IonRefresherContent, toastController,
  alertController // <-- 1. Importado para manejar el 403
} from '@ionic/vue';
import { sendOutline } from 'ionicons/icons';
import axios from 'axios';
import { authService } from '@/servicies/auth_service';
import { obtener_api_url } from '@/config/debug';

interface Solicitud {
  id: number;
  tipo: string;
  direccion: string;
  observaciones: string;
  fecha: string;
}

const SOLICITUDES_KEY = 'ganica_solicitudes';

const form = ref({
  servicioId: 152,
  tipo: '',
  direccion: '',
  observaciones: ''
});

const solicitudes = ref<Solicitud[]>([]);

const cargarSolicitudes = () => {
  const guardadas = localStorage.getItem(SOLICITUDES_KEY);
  if (guardadas) {
    solicitudes.value = JSON.parse(guardadas);
  }
};

const hacerRecarga = async (event: CustomEvent) => {
  cargarSolicitudes();
  setTimeout(() => {
    event.detail.complete();
  }, 500);
};

onMounted(() => {
  cargarSolicitudes();
});


const enviarSolicitud = async () => {

  if (!form.value.tipo || !form.value.direccion) {
    const toast = await toastController.create({
      message: 'Completá el tipo de residuo y la dirección.',
      duration: 2500,
      color: 'warning',
      position: 'bottom'
    });

    await toast.present();
    return;
  }

  try {
    const token = authService.obtenerToken();

    if (!token) {
      const toast = await toastController.create({
        message: 'Tu sesión expiró. Volvé a iniciar sesión.',
        duration: 2500,
        color: 'danger',
        position: 'bottom'
      });

      await toast.present();
      return;
    }

    console.log('Enviando solicitud:', {
      servicioRecoleccionId: form.value.servicioId,
      direccion: form.value.direccion,
      observaciones: form.value.observaciones
    });

    const response = await axios.post(
      `${obtener_api_url()}/api/Solicitudes`,
      {
        servicioRecoleccionId: form.value.tipo,
        direccion: form.value.direccion,
        observaciones: form.value.observaciones
      },
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      }
    );

    console.log('Solicitud creada:', response.data);

    const nuevaSolicitud: Solicitud = {
      id: response.data.id,
      tipo: 'Residuos Voluminosos y Poda',
      direccion: form.value.direccion,
      observaciones: form.value.observaciones,
      fecha: new Date().toLocaleDateString('es-AR')
    };

    solicitudes.value.unshift(nuevaSolicitud);

    localStorage.setItem(
      SOLICITUDES_KEY,
      JSON.stringify(solicitudes.value)
    );

   form.value = {
  servicioId: 152,
  tipo: '',
  direccion: '',
  observaciones: ''
};
    const toast = await toastController.create({
      message: '¡Solicitud registrada y enviada a los recolectores!',
      duration: 2500,
      color: 'success',
      position: 'bottom'
    });

    await toast.present();

  } catch (error: any) {
    console.error('Error al enviar solicitud:', error);
    console.error('Status:', error?.response?.status);
    console.error('Respuesta del servidor:', error?.response?.data);

    const toast = await toastController.create({
      message: 'No se pudo registrar la solicitud.',
      duration: 2500,
      color: 'danger',
      position: 'bottom'
    });

    await toast.present();
  }
};
</script>