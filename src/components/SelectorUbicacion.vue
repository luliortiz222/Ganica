
<script setup lang="ts">
import { ref, watch } from 'vue';
import {
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonButtons,
  IonButton,
  IonSearchbar,
  IonText,
  toastController
} from '@ionic/vue';

const props = defineProps<{
  abierto: boolean;
  direccionInicial?: string;
  latitudInicial?: number | null;
  longitudInicial?: number | null;
}>();

const emit = defineEmits<{
  cerrar: [];
  confirmar: [ubicacion: {
    direccion: string;
    latitud: number;
    longitud: number;
  }];
}>();

const direccion = ref('');
const busqueda = ref('');
const latitud = ref(props.latitudInicial ?? -33.6757);
const longitud = ref(props.longitudInicial ?? -65.4578);
const zoom = ref(15);
const cargando = ref(false);
const errorMapa = ref(false);
const resultadoElegido = ref(false);

const mapa = ref<HTMLDivElement | null>(null);
const imagenes = ref<HTMLImageElement[]>([]);
const desplazamiento = ref({ x: 0, y: 0 });
const tiles = ref<{ x: number; y: number; src: string }[]>([]);


function coordenadasATile(lat: number, lon: number, z: number) {
  const n = 2 ** z;
  const x = ((lon + 180) / 360) * n;
  const latRad = (lat * Math.PI) / 180;
  const y =
    ((1 -
      Math.log(Math.tan(latRad) + 1 / Math.cos(latRad)) /
        Math.PI) /
      2) *
    n;

  return { x, y };
}

function tileACoordenadas(x: number, y: number, z: number) {
  const n = 2 ** z;
  const lon = (x / n) * 360 - 180;
  const lat =
    (Math.atan(Math.sinh(Math.PI * (1 - (2 * y) / n))) * 180) /
    Math.PI;

  return { lat, lon };
}

function actualizarTiles() {
  if (!mapa.value) return;

  const rect = mapa.value.getBoundingClientRect();
  const centro = coordenadasATile(latitud.value, longitud.value, zoom.value);
  const mitadX = Math.ceil(rect.width / 256 / 2) + 1;
  const mitadY = Math.ceil(rect.height / 256 / 2) + 1;
  const baseX = Math.floor(centro.x);
  const baseY = Math.floor(centro.y);
  const nuevos = [];

  for (let dy = -mitadY; dy <= mitadY; dy++) {
    for (let dx = -mitadX; dx <= mitadX; dx++) {
      const x = baseX + dx;
      const y = baseY + dy;
      const n = 2 ** zoom.value;

      if (y < 0 || y >= n) continue;

      nuevos.push({
        x: dx,
        y: dy,
        src: `https://tile.openstreetmap.org/${zoom.value}/${((x % n) + n) % n}/${y}.png`
      });
    }
  }

  tiles.value = nuevos;
  desplazamiento.value = {
    x: (centro.x - baseX) * 256,
    y: (centro.y - baseY) * 256
  };
}

function moverMapa(dx: number, dy: number) {
  const centro = coordenadasATile(latitud.value, longitud.value, zoom.value);

  const nueva = tileACoordenadas(
    centro.x - dx / 256,
    centro.y - dy / 256,
    zoom.value
  );

  latitud.value = nueva.lat;
  longitud.value = nueva.lon;
  actualizarTiles();
}

function acercar(cambio: number) {
  zoom.value = Math.max(3, Math.min(19, zoom.value + cambio));
  actualizarTiles();
}

let inicioX = 0;
let inicioY = 0;
let ultimoX = 0;
let ultimoY = 0;
let arrastrando = false;

function iniciarMovimiento(event: PointerEvent) {
  if ((event.target as HTMLElement).closest('ion-button')) return;

  arrastrando = true;
  inicioX = event.clientX;
  inicioY = event.clientY;
  ultimoX = inicioX;
  ultimoY = inicioY;

  (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
}

function mover(event: PointerEvent) {
  if (!arrastrando) return;

  const dx = event.clientX - ultimoX;
  const dy = event.clientY - ultimoY;

  ultimoX = event.clientX;
  ultimoY = event.clientY;

  moverMapa(dx, dy);
}

function terminarMovimiento() {
  arrastrando = false;
}

async function buscarDireccion() {
  if (!busqueda.value.trim()) return;

  cargando.value = true;

  try {
    const params = new URLSearchParams({
      q: busqueda.value.trim(),
      format: 'jsonv2',
      limit: '1'
    });

    const response = await fetch(
      `https://nominatim.openstreetmap.org/search?${params.toString()}`,
      { headers: { Accept: 'application/json' } }
    );

    if (!response.ok) throw new Error('No se pudo buscar la dirección.');

    const resultados = await response.json();

    if (!resultados.length) {
      throw new Error('No se encontraron resultados para esa dirección.');
    }

    const resultado = resultados[0];
    latitud.value = Number(resultado.lat);
    longitud.value = Number(resultado.lon);
    direccion.value = resultado.display_name;
    resultadoElegido.value = true;
    actualizarTiles();
  } catch (error) {
    const toast = await toastController.create({
      message:
        error instanceof Error
          ? error.message
          : 'No se pudo buscar. Podés marcar el mapa o escribir la dirección.',
      duration: 3000,
      color: 'warning'
    });
    await toast.present();
  } finally {
    cargando.value = false;
  }
}

async function confirmarUbicacion() {
  if (!direccion.value.trim()) {
    try {
      const params = new URLSearchParams({
        lat: String(latitud.value),
        lon: String(longitud.value),
        format: 'jsonv2'
      });

      const response = await fetch(
        `https://nominatim.openstreetmap.org/reverse?${params.toString()}`,
        { headers: { Accept: 'application/json' } }
      );

      if (response.ok) {
        const resultado = await response.json();
        direccion.value = resultado.display_name || '';
      }
    } catch {
      // Si falla el servicio, se puede completar la dirección manualmente.
    }
  }

  if (!direccion.value.trim()) {
    const toast = await toastController.create({
      message: 'Escribí una dirección o buscala antes de confirmar.',
      duration: 2500,
      color: 'warning'
    });
    await toast.present();
    return;
  }

  emit('confirmar', {
    direccion: direccion.value.trim(),
    latitud: latitud.value,
    longitud: longitud.value
  });
  emit('cerrar');
}

watch(
  () => props.abierto,
  (abierto) => {
    if (!abierto) return;

    direccion.value = props.direccionInicial || '';
    busqueda.value = props.direccionInicial || '';
    latitud.value = props.latitudInicial ?? -33.6757;
    longitud.value = props.longitudInicial ?? -65.4578;
    resultadoElegido.value = false;
    errorMapa.value = false;

    setTimeout(() => actualizarTiles(), 200);
  }
);

watch(
  () => tiles.value.length,
  () => {
    errorMapa.value = false;
  }
);

</script>

<template>
  <ion-modal :is-open="abierto" @didDismiss="emit('cerrar')">
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Elegir ubicación</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="emit('cerrar')">Cerrar</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <p>Buscá una dirección o mové el mapa hasta que el marcador quede en el lugar correcto.</p>

      <ion-searchbar
        v-model="busqueda"
        placeholder="Buscar dirección..."
        @keyup.enter="buscarDireccion"
      />

      <ion-button expand="block" @click="buscarDireccion" :disabled="cargando">
        {{ cargando ? 'BUSCANDO...' : 'BUSCAR DIRECCIÓN' }}
      </ion-button>

      <div
        ref="mapa"
        class="mapa"
        @pointerdown="iniciarMovimiento"
        @pointermove="mover"
        @pointerup="terminarMovimiento"
        @pointercancel="terminarMovimiento"
        @lostpointercapture="terminarMovimiento"
      >
        <div
          v-for="tile in tiles"
          :key="`${tile.x}-${tile.y}-${zoom}`"
          class="tile"
          :style="{
            left: `calc(50% + ${tile.x * 256 - desplazamiento.x}px)`,
            top: `calc(50% + ${tile.y * 256 - desplazamiento.y}px)`
          }"
        >
          <img
            :src="tile.src"
            alt=""
            draggable="false"
            @error="errorMapa = true"
          />
        </div>

        <div class="marcador" aria-label="Ubicación seleccionada">📍</div>

        <div class="zoom">
          <ion-button size="small" @click.stop="acercar(1)">+</ion-button>
          <ion-button size="small" @click.stop="acercar(-1)">−</ion-button>
        </div>
      </div>

      <ion-text v-if="errorMapa" color="warning">
        <p>No se pudieron cargar algunas imágenes del mapa. Podés escribir la dirección manualmente.</p>
      </ion-text>

      <p class="coordenadas">
        Latitud: {{ latitud.toFixed(6) }} · Longitud: {{ longitud.toFixed(6) }}
      </p>

      <ion-item>
        <ion-label position="stacked">Dirección de retiro</ion-label>
        <ion-input
          v-model="direccion"
          placeholder="Podés escribir la dirección manualmente"
        />
      </ion-item>

      <ion-button expand="block" class="ion-margin-top" @click="confirmarUbicacion">
        CONFIRMAR UBICACIÓN
      </ion-button>
    </ion-content>
  </ion-modal>
</template>

<style scoped>
.mapa {
  position: relative;
  width: 100%;
  height: 380px;
  margin-top: 12px;
  overflow: hidden;
  background: #e7e7e7;
  touch-action: none;
  cursor: grab;
  user-select: none;
}

.mapa:active {
  cursor: grabbing;
}

.tile {
  position: absolute;
  width: 256px;
  height: 256px;
  transform: translate(-50%, -50%);
  pointer-events: none;
}

.tile img {
  width: 256px;
  height: 256px;
  display: block;
  pointer-events: none;
}

.marcador {
  position: absolute;
  z-index: 10;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -100%);
  font-size: 32px;
  pointer-events: none;
}

.zoom {
  position: absolute;
  z-index: 11;
  top: 8px;
  right: 8px;
  display: flex;
  flex-direction: column;
}

.coordenadas {
  font-size: 12px;
  color: var(--ion-color-medium);
}
</style>
