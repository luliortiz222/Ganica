
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
  --background: #ffffff;
  --color: #929292;
  --color-selected: var(--ion-color-primary);
  height: 60px;
  padding: 4px 10px;
  border-top: 1px solid #eeeeee;
}

ion-tab-button {
  --background: transparent;
  --color: #929292;
  --color-selected: var(--ion-color-primary);
  --ripple-color: transparent;
  min-width: 0;
  flex: 1;
}

ion-tab-button ion-icon {
  font-size: 21px;
}

ion-tab-button ion-label {
  font-size: 10px;
  font-weight: 500;
  margin-top: 3px;
}

ion-tab-button.tab-selected ion-icon {
  font-size: 22px;
}

/* MODO OSCURO */
body.dark ion-tab-bar {
  --background: #000000;
  --color: #cccccc;
  --color-selected: var(--ion-color-primary);
  border-top: 1px solid #333333;
}

body.dark ion-tab-button {
  --background: transparent;
  --color: #cccccc;
  --color-selected: var(--ion-color-primary);
}

ion-toolbar[color="primary"] {
  --background: var(--ion-color-primary);
  --color: var(--ion-color-primary-contrast);
}
</style>
