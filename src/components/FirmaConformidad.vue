
<template>
  <ion-modal
    :is-open="abierto"
    :backdrop-dismiss="false"
    @did-present="inicializarFirma"
    @did-dismiss="cerrar"
  >
    <ion-header>
      <ion-toolbar color="success">
        <ion-buttons slot="start">
          <ion-button
            :disabled="enviando"
            @click="cerrar"
          >
            <ion-icon :icon="closeOutline" />
          </ion-button>
        </ion-buttons>

        <ion-title>Conformidad de retiro</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding firma-content">
      <div class="firma-contenedor">
        <div class="firma-icono">
          <ion-icon :icon="createOutline" />
        </div>

        <h2>Firma del vecino</h2>

        <p class="firma-descripcion">
          Solicitá al vecino que firme dentro del recuadro
          para confirmar la recepción del servicio.
        </p>

        <div class="solicitud-resumen">
          <div>
            <span class="dato-label">Solicitud</span>
            <strong>#{{ solicitudId }}</strong>
          </div>

          <div>
            <span class="dato-label">Dirección</span>
            <strong>{{ direccion || 'No especificada' }}</strong>
          </div>

          <div>
            <span class="dato-label">Residuo</span>
            <strong>{{ tipoResiduo || 'No especificado' }}</strong>
          </div>
        </div>

        <div class="firma-instruccion">
          <ion-icon :icon="fingerPrintOutline" />
          Firmá con el dedo en el siguiente espacio
        </div>

        <div
          class="firma-panel"
          :class="{ 'firma-panel-error': errorFirma }"
        >
          <canvas
            ref="canvasRef"
            class="firma-canvas"
            @pointerdown="iniciarTrazo"
            @pointermove="continuarTrazo"
            @pointerup="terminarTrazo"
            @pointercancel="terminarTrazo"
            @lostpointercapture="terminarTrazo"
          />
        </div>

        <p
          v-if="errorFirma"
          class="mensaje-error"
          role="alert"
        >
          {{ errorFirma }}
        </p>

        <div class="acciones-firma">
          <ion-button
            fill="outline"
            color="medium"
            :disabled="enviando"
            @click="limpiarFirma"
          >
            <ion-icon
              slot="start"
              :icon="refreshOutline"
            />
            Limpiar
          </ion-button>

          <ion-button
            color="success"
            class="boton-confirmar"
            :disabled="enviando"
            @click="confirmarFirma"
          >
            <ion-spinner
              v-if="enviando"
              name="crescent"
            />

            <template v-else>
              <ion-icon
                slot="start"
                :icon="checkmarkCircleOutline"
              />
              Confirmar y finalizar
            </template>
          </ion-button>
        </div>

        <p class="aviso-legal">
          Al confirmar, se registrará la firma como constancia
          de conformidad del retiro.
        </p>
      </div>
    </ion-content>
  </ion-modal>
</template>

<script setup lang="ts">
import {
  ref,
  watch,
  nextTick,
  onBeforeUnmount,
} from 'vue';

import {
  IonModal,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonIcon,
  IonContent,
  IonSpinner,
  alertController,
} from '@ionic/vue';

import {
  closeOutline,
  createOutline,
  fingerPrintOutline,
  refreshOutline,
  checkmarkCircleOutline,
} from 'ionicons/icons';

import { ajax_request } from '@/servicies/ajax_service';

const props = defineProps<{
  abierto: boolean;
  solicitudId: number;
  direccion: string;
  tipoResiduo: string;
}>();

const emit = defineEmits<{
  (e: 'cerrar'): void;
  (e: 'finalizada'): void;
}>();

const canvasRef = ref<HTMLCanvasElement | null>(null);
const enviando = ref(false);
const errorFirma = ref('');

let contexto: CanvasRenderingContext2D | null = null;
let dibujando = false;
let tieneFirma = false;
let ultimoX = 0;
let ultimoY = 0;
let observador: ResizeObserver | null = null;

function configurarCanvas() {
  const canvas = canvasRef.value;
  if (!canvas) return;

  const rect = canvas.getBoundingClientRect();

  // El modal debe estar visible y el canvas tener dimensiones reales.
  if (rect.width <= 0 || rect.height <= 0) return;

  const escala = Math.max(window.devicePixelRatio || 1, 1);
  const ancho = Math.round(rect.width * escala);
  const alto = Math.round(rect.height * escala);

  // Evitar reinicializar el canvas si nada cambió.
  if (
    canvas.width === ancho &&
    canvas.height === alto &&
    contexto
  ) {
    return;
  }

  // Preservar la firma si el tamaño del canvas cambia.
  let firmaAnterior: string | null = null;

  if (tieneFirma) {
    firmaAnterior = canvas.toDataURL('image/png');
  }

  canvas.width = ancho;
  canvas.height = alto;

  contexto = canvas.getContext('2d');
  if (!contexto) return;

  contexto.setTransform(escala, 0, 0, escala, 0, 0);
  contexto.strokeStyle = '#176B45';
  contexto.fillStyle = '#176B45';
  contexto.lineWidth = 3;
  contexto.lineCap = 'round';
  contexto.lineJoin = 'round';

  if (firmaAnterior) {
    const imagen = new Image();

    imagen.onload = () => {
      if (!contexto || !canvasRef.value) return;

      const actual = canvasRef.value.getBoundingClientRect();

      contexto.drawImage(
        imagen,
        0,
        0,
        actual.width,
        actual.height
      );
    };

    imagen.src = firmaAnterior;
  }
}

function obtenerCoordenadas(evento: PointerEvent) {
  const canvas = canvasRef.value;
  if (!canvas) return null;

  const rect = canvas.getBoundingClientRect();

  return {
    x: evento.clientX - rect.left,
    y: evento.clientY - rect.top,
  };
}

function iniciarTrazo(evento: PointerEvent) {
  if (enviando.value) return;

  // Filtrar solamente los botones secundarios del ratón.
  if (evento.pointerType === 'mouse' && evento.button !== 0) {
    return;
  }

  const canvas = canvasRef.value;
  const punto = obtenerCoordenadas(evento);

  if (!canvas || !punto) return;

  // Recuperar el contexto si todavía no está configurado.
  if (!contexto) configurarCanvas();
  if (!contexto) return;

  evento.preventDefault();

  try {
    canvas.setPointerCapture(evento.pointerId);
  } catch (error) {
    console.warn('No se pudo capturar el puntero:', error);
  }

  dibujando = true;
  ultimoX = punto.x;
  ultimoY = punto.y;

  // Registrar también una firma formada por un único punto.
  contexto.beginPath();
  contexto.arc(punto.x, punto.y, 1.5, 0, Math.PI * 2);
  contexto.fill();

  tieneFirma = true;
  errorFirma.value = '';
}

function continuarTrazo(evento: PointerEvent) {
  if (!dibujando || enviando.value || !contexto) return;

  const punto = obtenerCoordenadas(evento);
  if (!punto) return;

  evento.preventDefault();

  contexto.beginPath();
  contexto.moveTo(ultimoX, ultimoY);
  contexto.lineTo(punto.x, punto.y);
  contexto.stroke();

  ultimoX = punto.x;
  ultimoY = punto.y;
  tieneFirma = true;
}

function terminarTrazo() {
  dibujando = false;
}

function limpiarFirma() {
  const canvas = canvasRef.value;

  if (!canvas || !contexto || enviando.value) return;

  const rect = canvas.getBoundingClientRect();

  contexto.clearRect(0, 0, rect.width, rect.height);

  tieneFirma = false;
  errorFirma.value = '';
}

function cerrar() {
  if (enviando.value) return;
  emit('cerrar');
}

async function inicializarFirma() {
  await nextTick();

  configurarCanvas();

  observador?.disconnect();

  if (canvasRef.value) {
    observador = new ResizeObserver(() => {
      // No modificar el canvas mientras se está dibujando.
      if (!dibujando) {
        configurarCanvas();
      }
    });

    observador.observe(canvasRef.value);
  }
}

watch(
  () => props.abierto,
  async (abierto) => {
    if (!abierto) {
      observador?.disconnect();
      return;
    }

    // La configuración definitiva ocurre en did-present.
    await nextTick();
  },
  { immediate: true }
);

async function confirmarFirma() {
  if (enviando.value) return;

  const canvas = canvasRef.value;

  if (!canvas || !tieneFirma) {
    errorFirma.value = 'El vecino debe firmar antes de confirmar.';
    return;
  }

  const confirmacion = await alertController.create({
    header: 'Confirmar conformidad',
    message:
      '¿Confirmás que el vecino firmó y que el retiro fue realizado?',
    buttons: [
      {
        text: 'Volver',
        role: 'cancel',
      },
      {
        text: 'Confirmar',
        role: 'confirm',
      },
    ],
  });

  await confirmacion.present();

  const resultado = await confirmacion.onDidDismiss();

  if (resultado.role !== 'confirm') return;

  enviando.value = true;
  errorFirma.value = '';

  try {
    const blob = await new Promise<Blob>((resolve, reject) => {
      canvas.toBlob((resultadoBlob) => {
        if (resultadoBlob) {
          resolve(resultadoBlob);
        } else {
          reject(
            new Error('No se pudo generar la imagen de la firma.')
          );
        }
      }, 'image/png');
    });

    const formulario = new FormData();

    formulario.append(
      'firma',
      blob,
      `firma-solicitud-${props.solicitudId}.png`
    );

    await ajax_request(
      `Solicitudes/${props.solicitudId}/firma`,
      {
        method: 'POST',
        body: formulario,
      }
    );

    emit('finalizada');
  } catch (error: any) {
    errorFirma.value =
      error?.message ||
      'No se pudo guardar la firma. Intentá nuevamente.';
  } finally {
    enviando.value = false;
  }
}

onBeforeUnmount(() => {
  observador?.disconnect();
  observador = null;
});
</script>

<style scoped>
.firma-content {
  --background: #f5f8f6;
}

.firma-contenedor {
  max-width: 620px;
  margin: 0 auto;
  padding: 12px 0 28px;
}

.firma-icono {
  width: 58px;
  height: 58px;
  margin: 4px auto 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 18px;
  background: #e1f2e8;
  color: #176b45;
  font-size: 29px;
}

h2 {
  margin: 0;
  text-align: center;
  color: #173b2a;
  font-size: 23px;
  font-weight: 750;
}

.firma-descripcion {
  max-width: 440px;
  margin: 10px auto 22px;
  text-align: center;
  color: #66756c;
  font-size: 14px;
  line-height: 1.55;
}

.solicitud-resumen {
  display: grid;
  gap: 13px;
  padding: 17px;
  margin-bottom: 23px;
  background: #fff;
  border: 1px solid #e1eae4;
  border-radius: 15px;
}

.solicitud-resumen div {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.dato-label {
  color: #718078;
  font-size: 12px;
}

.solicitud-resumen strong {
  color: #253b2e;
  font-size: 14px;
  overflow-wrap: anywhere;
}

.firma-instruccion {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
  color: #315b43;
  font-size: 13px;
  font-weight: 600;
}

.firma-instruccion ion-icon {
  font-size: 21px;
}

.firma-panel {
  width: 100%;
  height: 220px;
  overflow: hidden;
  background: #fff;
  border: 2px dashed #40a66d;
  border-radius: 15px;
  box-shadow: 0 3px 12px rgb(25 80 48 / 5%);
}

.firma-panel-error {
  border-color: #c0392b;
}

.firma-canvas {
  display: block;
  width: 100%;
  height: 100%;
  touch-action: none;
  user-select: none;
  -webkit-user-select: none;
  -webkit-touch-callout: none;
  cursor: crosshair;
}

.acciones-firma {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 19px;
}

.acciones-firma ion-button {
  min-height: 46px;
  margin: 0;
  font-weight: 650;
  --border-radius: 12px;
}

.boton-confirmar {
  --background: #176b45;
  --background-hover: #105536;
  --background-activated: #105536;
}

.mensaje-error {
  margin: 9px 0 0;
  color: #b42318;
  font-size: 13px;
}

.aviso-legal {
  margin: 17px 4px 0;
  color: #7a887f;
  font-size: 11px;
  line-height: 1.5;
  text-align: center;
}

@media (min-width: 520px) {
  .acciones-firma {
    flex-direction: row;
    justify-content: flex-end;
  }

  .acciones-firma ion-button {
    flex: 1;
  }
}
</style>
