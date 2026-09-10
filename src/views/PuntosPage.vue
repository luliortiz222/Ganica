<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>
        <ion-title>Puntos de Retiro</ion-title>
      </ion-toolbar>
      <ion-toolbar color="primary">
        <ion-searchbar 
          v-model="busqueda" 
          placeholder="Buscar por barrio o material..." 
          animated 
        />
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <ion-refresher slot="fixed" @ionRefresh="hacerRecarga($event)">
        <ion-refresher-content 
          pulling-text="Deslizá para actualizar..." 
          refreshing-spinner="circles" 
          refreshing-text="Cargando puntos...">
        </ion-refresher-content>
      </ion-refresher>

      <h2 class="app-titulo-ganica">Puntos Verdes y Reciclaje</h2>

      <div v-if="puntosFiltrados.length === 0" class="ion-text-center ion-padding">
        <ion-note>No se encontraron puntos de retiro que coincidan con la búsqueda.</ion-note>
      </div>

      <ion-card v-for="punto in puntosFiltrados" :key="punto.id">
        <ion-card-header>
          <ion-card-title>{{ punto.nombre }}</ion-card-title>
          <ion-card-subtitle>📍 {{ punto.direccion }} ({{ punto.barrio }})</ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <p style="margin-bottom: 8px;">
            <ion-icon :icon="timeOutline" style="vertical-align: middle; margin-right: 4px;" />
            <strong>Horario:</strong> {{ punto.horario }}
          </p>
          
          <div class="chips-container">
            <ion-chip v-for="material in punto.materiales" :key="material" color="primary">
              <ion-label>{{ material }}</ion-label>
            </ion-chip>
          </div>
        </ion-card-content>
      </ion-card>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { 
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, 
  IonButtons, IonMenuButton, IonSearchbar, IonCard, 
  IonCardHeader, IonCardTitle, IonCardSubtitle, IonCardContent, 
  IonChip, IonLabel, IonNote, IonIcon, IonRefresher, IonRefresherContent 
} from '@ionic/vue';
import { timeOutline } from 'ionicons/icons';

interface PuntoRetiro {
  id: number;
  nombre: string;
  barrio: string;
  direccion: string;
  horario: string;
  materiales: string[];
}

const busqueda = ref('');

const puntos = ref<PuntoRetiro[]>([
  {
    id: 1,
    nombre: 'Punto Verde Plaza Central',
    barrio: 'Centro',
    direccion: 'Av. San Martín y Belgrano',
    horario: 'Lunes a Sábados 08:00 a 20:00 hs',
    materiales: ['Plástico', 'Cartón', 'Vidrio', 'Metal']
  },
  {
    id: 2,
    nombre: 'EcoPunto Parque Norte',
    barrio: 'Norte',
    direccion: 'Calle Las Heras 850',
    horario: '24 horas (Contenedores automáticos)',
    materiales: ['Plástico', 'Papel', 'Pilas / Baterías']
  },
  {
    id: 3,
    nombre: 'Estación de Reciclaje Sur',
    barrio: 'Sur',
    direccion: 'Av. Mitre 2100',
    horario: 'Lunes a Viernes 09:00 a 18:00 hs',
    materiales: ['RAEEs (Electrónicos)', 'Aceite Vegetal', 'Cartón', 'Metal']
  }
]);

const hacerRecarga = (event: CustomEvent) => {
  busqueda.value = '';
  setTimeout(() => {
    event.detail.complete();
  }, 500);
};

const puntosFiltrados = computed(() => {
  if (!busqueda.value.trim()) return puntos.value;
  const q = busqueda.value.toLowerCase();
  return puntos.value.filter(p => 
    p.nombre.toLowerCase().includes(q) ||
    p.barrio.toLowerCase().includes(q) ||
    p.direccion.toLowerCase().includes(q) ||
    p.materiales.some(m => m.toLowerCase().includes(q))
  );
});
</script>

<style scoped>
.chips-container {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-top: 8px;
}
</style>