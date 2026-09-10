<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';

import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonButtons,
  IonButton,
  IonIcon,
  IonMenuButton,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonSkeletonText,
  IonFab,
  IonFabButton,
  onIonViewWillEnter
} from '@ionic/vue';

import {
  sunnyOutline,
  moonOutline,
  alertCircleOutline,
  trashOutline,
  addOutline,
  createOutline,
  trashBinOutline
} from 'ionicons/icons';

import { obtenerTemaGuardado, aplicarTema } from '@/config/tema';
import { servicios_store, Servicio } from '@/stores/servicios_store';
import ModalServicio from '@/components/ModalServicio.vue';

const esOscuro = ref(false);

// Control de apertura del Modal
const modalAbierto = ref(false);
const servicioAEditar = ref<Servicio | null>(null);

const cargando = computed(() => servicios_store.cargando);
const error = computed(() => servicios_store.error);
const hay_servicios = computed(() => servicios_store.hay_servicios);
const lista = computed(() => servicios_store.lista);

const reintentar = () => {
  servicios_store.cargar_servicios();
};

// Funciones para abrir el modal (Alta / Modificación)
const abrirModalNuevo = () => {
  servicioAEditar.value = null;
  modalAbierto.value = true;
};

const abrirModalEditar = (servicio: Servicio) => {
  servicioAEditar.value = servicio;
  modalAbierto.value = true;
};

// Función para eliminar (Baja)
const darDeBaja = async (id: string | number) => {
  if (confirm('¿Estás seguro de que querés dar de baja este servicio?')) {
    await servicios_store.eliminar_servicio(id);
  }
};

onMounted(() => {
  esOscuro.value = obtenerTemaGuardado();
});

onIonViewWillEnter(() => {
  servicios_store.cargar_servicios();
});

const alternarTema = () => {
  esOscuro.value = !esOscuro.value;
  aplicarTema(esOscuro.value);
};
</script>

<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>

        <ion-title>Gánica · Servicios</ion-title>

        <ion-buttons slot="end">
          <ion-button @click="alternarTema">
            <ion-icon
              :icon="esOscuro ? sunnyOutline : moonOutline"
              slot="icon-only"
            />
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <div class="app-portada">
        <ion-title size="large" class="ion-no-padding">
          Servicios de Recolección
        </ion-title>
      </div>

      <!-- 1. ESTADO CARGANDO -->
      <div v-if="cargando">
        <ion-card v-for="n in 3" :key="n">
          <ion-card-header>
            <ion-card-title>
              <ion-skeleton-text animated style="width: 60%; height: 20px;" />
            </ion-card-title>
          </ion-card-header>

          <ion-card-content>
            <ion-skeleton-text animated style="width: 90%;" />
            <ion-skeleton-text animated style="width: 45%; margin-top: 8px;" />
            <ion-skeleton-text animated style="width: 50%; margin-top: 4px;" />
          </ion-card-content>
        </ion-card>
      </div>

      <!-- 2. ESTADO ERROR -->
      <div v-else-if="error" class="estado-contenedor">
        <ion-icon :icon="alertCircleOutline" class="icono-error" />

        <h2>Error de conexión</h2>

        <p>{{ error }}</p>

        <ion-button color="primary" fill="solid" @click="reintentar">
          REINTENTAR
        </ion-button>
      </div>

      <!-- 3. ESTADO VACÍO -->
      <div v-else-if="!hay_servicios" class="estado-contenedor">
        <ion-icon :icon="trashOutline" class="icono-vacio" />

        <p>No hay servicios disponibles en este momento.</p>
      </div>

      <!-- 4. LISTA REAL CON ACCIONES ABM -->
      <div v-else>
        <ion-card v-for="s in lista" :key="s.id">
          <ion-card-header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <ion-card-title>
                {{ s.nombre || s.titulo }}
              </ion-card-title>
              <div>
                <ion-button fill="clear" size="small" @click="abrirModalEditar(s)">
                  <ion-icon slot="icon-only" :icon="createOutline" />
                </ion-button>
                <ion-button fill="clear" color="danger" size="small" @click="darDeBaja(s.id)">
                  <ion-icon slot="icon-only" :icon="trashBinOutline" />
                </ion-button>
              </div>
            </div>
          </ion-card-header>

          <ion-card-content>
            <p>{{ s.descripcion || s.detalle }}</p>

            <p v-if="s.dias" style="margin-top: 8px;">
              <strong>Días:</strong> {{ s.dias }}
            </p>

            <p v-if="s.horario">
              <strong>Horario:</strong> {{ s.horario }}
            </p>
          </ion-card-content>
        </ion-card>
      </div>

      <!-- BOTÓN FLOTANTE PARA AGREGAR NUEVO SERVICIO -->
      <ion-fab vertical="bottom" horizontal="end" slot="fixed">
        <ion-fab-button color="primary" @click="abrirModalNuevo">
          <ion-icon :icon="addOutline" />
        </ion-fab-button>
      </ion-fab>

      <!-- COMPONENTE MODAL DE EDICIÓN / ALTA -->
      <ModalServicio
        :abierto="modalAbierto"
        :servicioEditar="servicioAEditar"
        @cerrar="modalAbierto = false"
      />
    </ion-content>
  </ion-page>
</template>

<style scoped>
.app-portada {
  margin-bottom: 1rem;
}

.estado-contenedor {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  text-align: center;
  gap: 12px;
}

.icono-error {
  font-size: 56px;
  color: var(--ion-color-danger);
}

.icono-vacio {
  font-size: 56px;
  color: var(--ion-color-medium);
}
</style>