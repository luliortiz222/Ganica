<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>
        <ion-title>Mi Cuenta</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <h2 class="app-titulo-ganica">Perfil de Usuario</h2>

      <!-- Tarjeta de Perfil -->
      <ion-card>
        <ion-card-header>
          <ion-card-title>Lourdes Ortiz</ion-card-title>
          <ion-card-subtitle>Vecina Registrada</ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <ion-list lines="full">
            <ion-item>
              <ion-input 
                v-model="perfil.nombre" 
                label="Nombre completo" 
                label-placement="stacked" 
              />
            </ion-item>

            <ion-item>
              <ion-input 
                v-model="perfil.direccion" 
                label="Dirección principal" 
                label-placement="stacked" 
              />
            </ion-item>

            <ion-item>
              <ion-toggle v-model="perfil.notificaciones">
                Recibir alertas de recolección
              </ion-toggle>
            </ion-item>

            <ion-item>
              <ion-toggle :checked="esOscuro" @ionChange="alternarTema">
                Modo Oscuro
              </ion-toggle>
            </ion-item>
          </ion-list>

          <ion-button expand="block" color="primary" class="ion-margin-top" @click="guardarPerfil">
            <ion-icon :icon="saveOutline" slot="start" />
            Guardar Cambios
          </ion-button>
        </ion-card-content>
      </ion-card>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { 
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, 
  IonButtons, IonMenuButton, IonCard, IonCardHeader, 
  IonCardTitle, IonCardSubtitle, IonCardContent, IonList, 
  IonItem, IonInput, IonToggle, IonButton, IonIcon, 
  toastController 
} from '@ionic/vue';
import { saveOutline } from 'ionicons/icons';
import { obtenerTemaGuardado, aplicarTema } from '@/config/tema';

const PERFIL_KEY = 'ganica_perfil_usuario';

const esOscuro = ref(false);

const perfil = ref({
  nombre: 'Lourdes Ortiz',
  direccion: 'Av. Mitre 1234',
  notificaciones: true
});

onMounted(() => {
  esOscuro.value = obtenerTemaGuardado();
  const guardado = localStorage.getItem(PERFIL_KEY);
  if (guardado) {
    perfil.value = JSON.parse(guardado);
  }
});

const alternarTema = (event: CustomEvent) => {
  esOscuro.value = event.detail.checked;
  aplicarTema(esOscuro.value);
};

const guardarPerfil = async () => {
  localStorage.setItem(PERFIL_KEY, JSON.stringify(perfil.value));
  
  const toast = await toastController.create({
    message: 'Perfil actualizado con éxito',
    duration: 2000,
    color: 'success',
    position: 'bottom'
  });
  await toast.present();
};
</script>