<template>
  <ion-app>
    <!-- Menú Lateral -->
    <ion-menu content-id="main-content">
      <ion-header>
        <ion-toolbar color="primary">
          <ion-title>Gánica</ion-title>
        </ion-toolbar>
      </ion-header>
      <ion-content>
        <ion-list-header>Menú Principal</ion-list-header>
        <ion-menu-toggle :auto-hide="false" v-for="ruta in rutas" :key="ruta.name">
          <ion-item :router-link="ruta.path" lines="none">
            <ion-icon slot="start" :icon="ruta.icono" />
            <ion-label>{{ ruta.titulo }}</ion-label>
          </ion-item>
        </ion-menu-toggle>
      </ion-content>
    </ion-menu>

    <!-- Contenido y Pestañas -->
    <ion-tabs id="main-content">
      <ion-router-outlet />
      
      <ion-tab-bar slot="bottom">
        <ion-tab-button 
          v-for="ruta in rutas" 
          :key="ruta.name" 
          :tab="ruta.name" 
          :href="ruta.path"
        >
          <ion-icon :icon="ruta.icono" />
          <ion-label>{{ ruta.titulo }}</ion-label>
        </ion-tab-button>
      </ion-tab-bar>
    </ion-tabs>
  </ion-app>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { 
  IonApp, 
  IonRouterOutlet, 
  IonTabs, 
  IonTabBar, 
  IonTabButton, 
  IonIcon, 
  IonLabel,
  IonMenu,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonListHeader,
  IonItem,
  IonMenuToggle
} from '@ionic/vue';
import { rutasNavegacion } from './navegacion';
import { inicializarTema } from './config/tema';

const rutas = rutasNavegacion;

onMounted(() => {
  inicializarTema();
});
</script>

<style>

ion-tab-bar {
  --color-selected: var(--ion-color-primary);
}

ion-toolbar[color="primary"] {
  --background: var(--ion-color-primary);
  --color: var(--ion-color-primary-contrast);
}
</style>