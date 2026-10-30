
<template>
  <ion-app>
    <!-- Menú lateral -->
    <ion-menu content-id="main-content">
      <ion-header>
        <ion-toolbar color="primary">
          <ion-title>Gánica</ion-title>
        </ion-toolbar>
      </ion-header>

      <ion-content>
        <ion-list-header>Menú Principal</ion-list-header>

        <ion-menu-toggle
          :auto-hide="false"
          v-for="ruta in rutasVisibles"
          :key="ruta.name"
        >
          <ion-item :router-link="ruta.path" lines="none">
            <ion-icon slot="start" :icon="ruta.icono" />
            <ion-label>{{ ruta.titulo }}</ion-label>
          </ion-item>
        </ion-menu-toggle>
      </ion-content>
    </ion-menu>

    <!-- Contenido y pestañas -->
    <ion-tabs id="main-content">
      <ion-router-outlet />

      <ion-tab-bar slot="bottom">
        <ion-tab-button
          v-for="ruta in rutasVisibles"
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
import {
  computed,
  onMounted,
  onUnmounted,
  ref
} from 'vue';

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
  IonMenuToggle,
} from '@ionic/vue';

import { rutasNavegacion } from './navegacion';
import { inicializarTema } from './config/tema';

import {
  iniciar_sincronizacion_automatica
} from '@/servicies/offline_sync_service';

const rutas = rutasNavegacion;

function obtenerRolActual(): string {
  try {
    const usuarioGuardado = localStorage.getItem('usuario');

    if (!usuarioGuardado) return '';

    const usuario = JSON.parse(usuarioGuardado);
    return String(usuario?.rol ?? '').trim().toLowerCase();
  } catch {
    return '';
  }
}

// El rol ahora es reactivo.
const rolActual = ref(obtenerRolActual());

const actualizarSesion = () => {
  rolActual.value = obtenerRolActual();
};

const rutasVisibles = computed(() => {
  const rol = rolActual.value;

  return rutas.filter((ruta) => {
    const rolesPermitidos = ruta.meta?.roles ?? [];

    return (
      rolesPermitidos.includes('TODOS') ||
      rolesPermitidos.includes(rol)
    );
  });
});

// Referencia para detener el evento al desmontar la aplicación.
let detenerSincronizacion: (() => void) | null = null;

onMounted(() => {
  inicializarTema();

  // Sincronizar la sesión al abrir la aplicación.
  actualizarSesion();

  window.addEventListener(
    'sesion-actualizada',
    actualizarSesion
  );

  // Iniciar sincronización automática de operaciones pendientes.
  detenerSincronizacion = iniciar_sincronizacion_automatica();
});

onUnmounted(() => {
  window.removeEventListener(
    'sesion-actualizada',
    actualizarSesion
  );

  detenerSincronizacion?.();
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
