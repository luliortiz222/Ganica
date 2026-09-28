<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-title>Puntos de Recolección</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <ion-refresher slot="fixed" @ionRefresh="refrescarEntregas($event)">
        <ion-refresher-content></ion-refresher-content>
      </ion-refresher>

      <ion-list>
        <ion-item v-for="entrega in entregasStore.entregas" :key="entrega.id">
          <ion-label>
            <h2>Pedido ID: {{ entrega.pedidoId }}</h2>
            <p>{{ entrega.direccionLinea }}</p>
            <p><strong>Estado:</strong> {{ entrega.estado }}</p>
          </ion-label>
          <div slot="end" class="acciones-recoleccion">
            <ion-button v-if="entrega.estado === 'Pendiente'" size="small" @click="tomarEntrega(entrega.id)">
              Tomar
            </ion-button>
            <ion-button size="small" fill="outline" @click="verEnMapa(entrega.direccionLatitud, entrega.direccionLongitud, entrega.direccionLinea)">
              Mapa
            </ion-button>
          </div>
        </ion-item>
      </ion-list>
    </ion-content>
  </ion-page>
</template>

<script>
import { defineComponent, onMounted } from 'vue';
import { IonPage, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, IonLabel, IonButton, IonRefresher, IonRefresherContent } from '@ionic/vue';
import { useEntregasStore } from '@/stores/entregas_store';
import { url_mapa } from '@/services/geolocalizacion_service';

export default defineComponent({
  name: 'EntregasPage',
  components: { IonPage, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonItem, IonLabel, IonButton, IonRefresher, IonRefresherContent },
  setup() {
    const entregasStore = useEntregasStore();

    onMounted(() => {
      entregasStore.cargarEntregas();
    });

    const refrescarEntregas = async (event) => {
      await entregasStore.cargarEntregas();
      event.target.complete();
    };

    const tomarEntrega = async (id) => {
      await entregasStore.asignarEntrega(id);
    };

    const verEnMapa = (lat, lng, dir) => {
      const url = url_mapa(lat, lng, dir);
      window.open(url, '_blank');
    };

    return {
      entregasStore,
      refrescarEntregas,
      tomarEntrega,
      verEnMapa
    };
  }
});
</script>