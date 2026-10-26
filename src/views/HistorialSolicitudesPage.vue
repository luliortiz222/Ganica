

<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-title>Historial de solicitudes</ion-title>

        <ion-buttons slot="end">
          <ion-button
            @click="cargarHistorial"
            :disabled="cargando"
            aria-label="Actualizar historial"
          >
            <ion-icon :icon="refreshOutline" />
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding historial-page">
      <div class="encabezado">
        <div class="icono-encabezado">
          <ion-icon :icon="timeOutline" />
        </div>

        <div>
          <h1>Trazabilidad</h1>
          <p>
            {{
              esAdministrador
                ? 'Trazabilidad completa de todas las solicitudes'
                : 'Seguimiento y trazabilidad de tus solicitudes'
            }}
          </p>
        </div>
      </div>

      <div class="resumen">
        <div class="resumen-item">
          <span class="resumen-numero">{{ solicitudes.length }}</span>
          <span class="resumen-label">Solicitudes</span>
        </div>

        <div class="separador"></div>

        <div class="resumen-item">
          <span class="resumen-numero">{{ historial.length }}</span>
          <span class="resumen-label">Eventos registrados</span>
        </div>
      </div>

      <ion-button
        expand="block"
        class="boton-actualizar"
        @click="cargarHistorial"
        :disabled="cargando"
      >
        <ion-icon slot="start" :icon="refreshOutline" />
        {{ cargando ? 'Actualizando historial...' : 'Actualizar historial' }}
      </ion-button>

      <ion-card v-if="error" class="tarjeta-error">
        <ion-card-content>
          <ion-icon :icon="alertCircleOutline" />
          <p>{{ error }}</p>
          <ion-button fill="outline" @click="cargarHistorial">
            Reintentar
          </ion-button>
        </ion-card-content>
      </ion-card>

      <div v-if="cargando && historial.length === 0" class="estado-vacio">
        <ion-spinner name="crescent" />
        <p>Cargando trazabilidad...</p>
      </div>

      <div
        v-else-if="!cargando && !error && historial.length === 0"
        class="estado-vacio"
      >
        <ion-icon :icon="fileTrayOutline" />
        <h2>Sin registros todavía</h2>
        <p>
          {{
            esAdministrador
              ? 'Cuando se creen solicitudes o cambien sus estados, los eventos aparecerán aquí.'
              : 'Cuando se registren tus solicitudes o cambien sus estados, los eventos aparecerán aquí.'
          }}
        </p>
      </div>

      <!-- Solicitudes plegables -->
      <ion-accordion-group
        v-if="solicitudes.length > 0"
        :multiple="true"
        class="grupos-acordeon"
      >
        <ion-accordion
          v-for="grupo in solicitudes"
          :key="grupo.solicitudId"
          :value="String(grupo.solicitudId)"
          class="grupo-solicitud"
        >
          <!-- Encabezado de la solicitud -->
          <ion-item
            slot="header"
            lines="none"
            class="solicitud-resumen"
          >
            <div class="solicitud-icono">
              <ion-icon :icon="cubeOutline" />
            </div>

            <div class="solicitud-datos">
              <span class="etiqueta">
                SOLICITUD #{{ grupo.solicitudId }}
              </span>

              <h2>{{ grupo.tipoResiduo || 'Residuo no especificado' }}</h2>

              <p class="direccion">
                <ion-icon :icon="locationOutline" />
                <span>
                  {{ grupo.direccion || 'Dirección no especificada' }}
                </span>
              </p>

              <span class="cantidad-eventos">
                {{ grupo.eventos.length }}
                {{ grupo.eventos.length === 1 ? 'evento' : 'eventos' }}
                registrados
              </span>
            </div>

            <ion-badge
              v-if="grupo.estadoActual"
              :color="colorEstado(grupo.estadoActual)"
              class="estado-badge"
            >
              {{ grupo.estadoActual }}
            </ion-badge>
          </ion-item>

          <!-- Detalles de trazabilidad -->
          <div slot="content" class="contenido-desplegable">
            <div class="timeline">
              <div
                v-for="(item, indice) in grupo.eventos"
                :key="item.id"
                class="evento"
              >
                <div class="timeline-columna">
                  <div
                    class="evento-icono"
                    :class="claseEvento(item.accion)"
                  >
                    <ion-icon :icon="iconoEvento(item.accion)" />
                  </div>

                  <div
                    v-if="indice < grupo.eventos.length - 1"
                    class="timeline-linea"
                  ></div>
                </div>

                <div class="evento-contenido">
                  <div class="evento-titulo-fila">
                    <h3>{{ tituloEvento(item) }}</h3>

                    <ion-badge
                      v-if="item.estadoNuevo"
                      :color="colorEstado(item.estadoNuevo)"
                    >
                      {{ item.estadoNuevo }}
                    </ion-badge>
                  </div>

                  <p class="evento-fecha">
                    <ion-icon :icon="calendarOutline" />
                    {{ formatearFecha(item.fecha) }}
                  </p>

                  <p class="evento-usuario">
                    <ion-icon :icon="personOutline" />
                    {{ item.usuarioEmail || `Usuario #${item.usuarioId}` }}
                  </p>

                  <div
                    v-if="item.estadoAnterior || item.estadoNuevo"
                    class="cambio-estado"
                  >
                    <span>{{ item.estadoAnterior || 'Inicio' }}</span>
                    <ion-icon :icon="arrowForwardOutline" />
                    <strong>{{ item.estadoNuevo || '—' }}</strong>
                  </div>

                  <p v-if="item.observacion" class="evento-observacion">
                    {{ item.observacion }}
                  </p>
                </div>
              </div>
            </div>

            <!-- Firma disponible únicamente para el vecino -->
            <div
              v-if="
                esVecino &&
                normalizarEstado(grupo.estadoActual) === 'en camino' &&
                !grupo.tieneFirma
              "
              class="panel-firma"
            >
              <div class="panel-firma-icono">
                <ion-icon :icon="createOutline" />
              </div>

              <div class="panel-firma-texto">
                <h3>Confirmar recepción del servicio</h3>
                <p>
                  Si el recolector llegó y realizó el retiro,
                  registrá tu firma de conformidad para finalizar
                  la solicitud.
                </p>
              </div>

              <ion-button
                expand="block"
                color="success"
                @click="abrirFirma(grupo)"
              >
                <ion-icon slot="start" :icon="createOutline" />
                Registrar mi firma
              </ion-button>
            </div>

            <!-- Confirmación visual cuando el historial registra la firma -->
            <div
              v-if="esVecino && grupo.tieneFirma"
              class="firma-registrada"
            >
              <ion-icon :icon="checkmarkCircleOutline" />
              <div>
                <strong>Conformidad registrada</strong>
                <p>La firma de recepción figura en la trazabilidad.</p>
              </div>
            </div>
          </div>
        </ion-accordion>
      </ion-accordion-group>

      
<FirmaConformidad
  v-if="solicitudParaFirma"
  :abierto="firmaAbierta"
  :solicitud-id="solicitudParaFirma.solicitudId"
  :direccion="solicitudParaFirma.direccion || ''"
  :tipo-residuo="solicitudParaFirma.tipoResiduo || ''"
  @cerrar="cerrarFirma"
  @finalizada="alFinalizarFirma"
/>

    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';

import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonIcon,
  IonContent,
  IonBadge,
  IonSpinner,
  IonCard,
  IonCardContent,
  IonAccordion,
  IonAccordionGroup,
  IonItem
} from '@ionic/vue';

import {
  timeOutline,
  refreshOutline,
  alertCircleOutline,
  fileTrayOutline,
  cubeOutline,
  locationOutline,
  calendarOutline,
  personOutline,
  arrowForwardOutline,
  createOutline,
  eyeOutline,
  checkmarkCircleOutline,
  documentTextOutline
} from 'ionicons/icons';

import FirmaConformidad from '@/components/FirmaConformidad.vue';

import {
  listar_historial_solicitudes_api,
  type HistorialSolicitudApi
} from '@/servicies/solicitudes_service';

interface GrupoSolicitud {
  solicitudId: number;
  tipoResiduo: string | null;
  direccion: string | null;
  estadoActual: string | null;
  eventos: HistorialSolicitudApi[];
  tieneFirma: boolean;
}

interface SolicitudParaFirma {
  solicitudId: number;
  direccion: string | null;
  tipoResiduo: string | null;
}

const historial = ref<HistorialSolicitudApi[]>([]);
const cargando = ref(false);
const error = ref('');

const firmaAbierta = ref(false);
const solicitudParaFirma = ref<SolicitudParaFirma | null>(null);

/*
 * Leer el rol de la sesión.
 * La API también debe validar la identidad y la propiedad
 * de la solicitud antes de aceptar una firma.
 */
const usuarioGuardado = (() => {
  try {
    return JSON.parse(localStorage.getItem('usuario') || '{}');
  } catch {
    return {};
  }
})();

const rolGuardado = usuarioGuardado.rol;

const rolUsuario = (
  typeof rolGuardado === 'string'
    ? rolGuardado
    : rolGuardado?.codigo ?? ''
)
  .trim()
  .toLowerCase();

const esAdministrador = rolUsuario === 'administrador';
const esVecino = rolUsuario === 'vecino';

const solicitudes = computed<GrupoSolicitud[]>(() => {
  const grupos = new Map<number, GrupoSolicitud>();

  for (const item of historial.value) {
    if (!grupos.has(item.solicitudId)) {
      grupos.set(item.solicitudId, {
        solicitudId: item.solicitudId,
        tipoResiduo: item.tipoResiduo,
        direccion: item.direccion,
        estadoActual: null,
        eventos: [],
        tieneFirma: false
      });
    }

    grupos.get(item.solicitudId)!.eventos.push(item);
  }

  const resultado = Array.from(grupos.values());

  for (const grupo of resultado) {
    // Orden cronológico inverso: primero el evento más reciente.
    grupo.eventos.sort(
      (a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime()
    );

    grupo.estadoActual =
      grupo.eventos.find(evento => evento.estadoNuevo)?.estadoNuevo ?? null;

    grupo.tieneFirma = grupo.eventos.some(
      evento => evento.accion === 'FirmaConformidadRegistrada'
    );
  }

  return resultado.sort((a, b) => {
    const fechaA = a.eventos[0]?.fecha ?? '';
    const fechaB = b.eventos[0]?.fecha ?? '';

    return new Date(fechaB).getTime() - new Date(fechaA).getTime();
  });
});

function normalizarEstado(estado: string | null): string {
  return (estado || '').trim().toLowerCase();
}

function abrirFirma(grupo: GrupoSolicitud) {
  // Nunca mostrar la firma al administrador ni al recolector.
  if (!esVecino) return;

  // Solo se firma cuando el recolector está en camino.
  if (normalizarEstado(grupo.estadoActual) !== 'en camino') return;

  if (grupo.tieneFirma) return;

  solicitudParaFirma.value = {
    solicitudId: grupo.solicitudId,
    direccion: grupo.direccion,
    tipoResiduo: grupo.tipoResiduo
  };

  firmaAbierta.value = true;
}

function cerrarFirma() {
  firmaAbierta.value = false;
  solicitudParaFirma.value = null;
}

async function alFinalizarFirma() {
  cerrarFirma();
  await cargarHistorial();
}

function tituloEvento(item: HistorialSolicitudApi): string {
  switch (item.accion) {
    case 'Creada':
      return 'Solicitud creada';

    case 'NotificacionLeida':
      return 'Notificación leída';

    case 'EstadoActualizado':
      return item.estadoNuevo
        ? `Estado actualizado: ${item.estadoNuevo}`
        : 'Estado de solicitud actualizado';

    case 'FirmaConformidadRegistrada':
      return 'Firma de conformidad registrada';

    default:
      return item.accion.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}

function iconoEvento(accion: string) {
  switch (accion) {
    case 'Creada':
      return documentTextOutline;

    case 'NotificacionLeida':
      return eyeOutline;

    case 'EstadoActualizado':
    case 'FirmaConformidadRegistrada':
      return checkmarkCircleOutline;

    default:
      return createOutline;
  }
}

function claseEvento(accion: string): string {
  switch (accion) {
    case 'Creada':
      return 'evento-creado';

    case 'NotificacionLeida':
      return 'evento-leido';

    case 'EstadoActualizado':
    case 'FirmaConformidadRegistrada':
      return 'evento-actualizado';

    default:
      return 'evento-generico';
  }
}

function colorEstado(estado: string): string {
  switch (normalizarEstado(estado)) {
    case 'pendiente':
      return 'warning';

    case 'observada':
      return 'danger';

    case 'en camino':
      return 'primary';

    case 'finalizada':
      return 'success';

    default:
      return 'medium';
  }
}

function formatearFecha(fecha: string): string {
  const fechaValida = new Date(fecha);

  if (Number.isNaN(fechaValida.getTime())) {
    return fecha;
  }

  return fechaValida.toLocaleString('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
}

async function cargarHistorial() {
  if (cargando.value) return;

  cargando.value = true;
  error.value = '';

  try {
    historial.value = await listar_historial_solicitudes_api();
  } catch (e) {
    console.error('Error al cargar el historial:', e);

    error.value =
      'No se pudo cargar el historial. Verificá tu conexión y tus permisos.';
  } finally {
    cargando.value = false;
  }
}

onMounted(cargarHistorial);
</script>

<style scoped>
.historial-page {
  --dorado: #b08d57;
  --texto: #25221f;
  --texto-secundario: #77716a;
}

.encabezado {
  display: flex;
  align-items: center;
  gap: 14px;
  margin: 8px 0 22px;
}

.icono-encabezado {
  width: 54px;
  height: 54px;
  flex-shrink: 0;
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #eee3d2;
  color: #80602e;
}

.icono-encabezado ion-icon {
  font-size: 28px;
}

.encabezado h1 {
  margin: 0 0 5px;
  font-size: 23px;
  font-weight: 750;
  color: var(--texto);
}

.encabezado p {
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
  color: var(--texto-secundario);
}

.resumen {
  display: flex;
  align-items: center;
  padding: 17px;
  border: 1px solid #e9dfd1;
  border-radius: 16px;
  background: #fffdf9;
  margin-bottom: 15px;
}

.resumen-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 4px;
  text-align: center;
}

.resumen-numero {
  font-size: 24px;
  font-weight: 750;
  color: var(--texto);
}

.resumen-label {
  font-size: 12px;
  color: var(--texto-secundario);
}

.separador {
  height: 38px;
  width: 1px;
  background: #e9dfd1;
  margin: 0 10px;
}

.boton-actualizar {
  margin: 0 0 22px;
  --background: #b08d57;
  --background-hover: #96713d;
  --color: #ffffff;
  --border-radius: 12px;
  font-weight: 650;
}

.grupos-acordeon {
  max-width: 900px;
  margin: 0 auto;
}

.grupo-solicitud {
  margin: 0 0 14px;
  border: 1px solid #eae5de;
  border-radius: 16px;
  overflow: hidden;
  background: #ffffff;
  box-shadow: 0 3px 12px rgba(44, 36, 25, 0.04);
}

.solicitud-resumen {
  --background: #fcfaf6;
  --color: #25221f;
  --padding-start: 15px;
  --padding-end: 12px;
  --inner-padding-end: 0;
  --min-height: 94px;
}

.solicitud-icono {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 42px;
  height: 42px;
  flex-shrink: 0;
  margin-right: 12px;
  border-radius: 12px;
  background: #eee3d2;
  color: #80602e;
  font-size: 22px;
}

.solicitud-datos {
  flex: 1;
  min-width: 0;
  padding: 12px 0;
  white-space: normal;
}

.etiqueta {
  font-size: 10px;
  font-weight: 750;
  letter-spacing: 0.8px;
  color: #92713e;
}

.solicitud-datos h2 {
  margin: 5px 0 8px;
  font-size: 15px;
  line-height: 1.4;
  font-weight: 700;
  color: var(--texto);
}

.direccion {
  display: flex;
  align-items: flex-start;
  gap: 5px;
  margin: 0;
  color: var(--texto-secundario);
  font-size: 12px;
  line-height: 1.5;
  overflow-wrap: anywhere;
}

.direccion ion-icon {
  flex-shrink: 0;
  margin-top: 2px;
  font-size: 15px;
}

.cantidad-eventos {
  display: block;
  margin-top: 7px;
  color: var(--texto-secundario);
  font-size: 11px;
}

.estado-badge {
  flex-shrink: 0;
  margin-left: 8px;
}

.contenido-desplegable {
  background: #ffffff;
  border-top: 1px solid #eee8df;
}

.timeline {
  padding: 19px 15px 4px;
}

.evento {
  display: flex;
  gap: 12px;
}

.timeline-columna {
  width: 30px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.evento-icono {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  flex-shrink: 0;
  border-radius: 50%;
  font-size: 16px;
  z-index: 1;
}

.evento-creado {
  background: #f3e8d5;
  color: #89662e;
}

.evento-leido {
  background: #e3ebf9;
  color: #315a96;
}

.evento-actualizado {
  background: #dcefe2;
  color: #276749;
}

.evento-generico {
  background: #ece8e2;
  color: #625a50;
}

.timeline-linea {
  flex: 1;
  min-height: 25px;
  width: 2px;
  background: #e6dfd5;
}

.evento-contenido {
  flex: 1;
  min-width: 0;
  padding: 2px 0 25px;
}

.evento-titulo-fila {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.evento-titulo-fila h3 {
  margin: 2px 0 8px;
  color: var(--texto);
  font-size: 14px;
  line-height: 1.4;
  font-weight: 700;
}

.evento-titulo-fila ion-badge {
  flex-shrink: 0;
}

.evento-fecha,
.evento-usuario {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 0 0 6px;
  color: var(--texto-secundario);
  font-size: 12px;
  line-height: 1.5;
  overflow-wrap: anywhere;
}

.evento-fecha ion-icon,
.evento-usuario ion-icon {
  flex-shrink: 0;
  font-size: 14px;
}

.cambio-estado {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin: 11px 0;
  padding: 9px 10px;
  border-radius: 9px;
  background: #f8f5f0;
  font-size: 12px;
  color: #6e675e;
}

.cambio-estado ion-icon {
  font-size: 15px;
  color: #a0804c;
}

.cambio-estado strong {
  color: #276749;
}

.evento-observacion {
  margin: 8px 0 0;
  padding-left: 10px;
  border-left: 2px solid #d8c3a2;
  color: #686158;
  font-size: 12px;
  line-height: 1.6;
}

.panel-firma {
  margin: 18px 15px 20px;
  padding: 18px;
  border: 1px solid #d6e9dc;
  border-radius: 16px;
  background: #f7fcf8;
}

.panel-firma-icono {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 42px;
  height: 42px;
  margin-bottom: 12px;
  border-radius: 12px;
  background: #dcefe2;
  color: #276749;
  font-size: 23px;
}

.panel-firma-texto h3 {
  margin: 0 0 7px;
  color: #244b35;
  font-size: 16px;
  font-weight: 700;
}

.panel-firma-texto p {
  margin: 0 0 15px;
  color: #5d6d62;
  font-size: 13px;
  line-height: 1.6;
}

.panel-firma ion-button {
  margin: 0;
  --border-radius: 11px;
  font-weight: 650;
}

.firma-registrada {
  display: flex;
  align-items: flex-start;
  gap: 11px;
  margin: 18px 15px 20px;
  padding: 15px;
  border: 1px solid #d6e9dc;
  border-radius: 13px;
  background: #f7fcf8;
  color: #276749;
}

.firma-registrada > ion-icon {
  flex-shrink: 0;
  font-size: 25px;
}

.firma-registrada strong {
  display: block;
  margin-bottom: 4px;
}

.firma-registrada p {
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
}

.estado-vacio {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 45px 20px;
  color: var(--texto-secundario);
}

.estado-vacio > ion-icon {
  font-size: 43px;
  color: #b7a58c;
}

.estado-vacio h2 {
  color: var(--texto);
  font-size: 17px;
  margin: 14px 0 5px;
}

.estado-vacio p {
  max-width: 300px;
  font-size: 13px;
  line-height: 1.6;
}

.estado-vacio ion-spinner {
  color: var(--dorado);
}

.tarjeta-error {
  border: 1px solid #f0c9c5;
  box-shadow: none;
}

.tarjeta-error ion-card-content {
  text-align: center;
}

.tarjeta-error ion-card-content > ion-icon {
  font-size: 30px;
  color: var(--ion-color-danger);
}

.tarjeta-error p {
  color: var(--ion-color-danger);
  line-height: 1.5;
}

@media (min-width: 768px) {
  .historial-page {
    --padding-start: 28px;
    --padding-end: 28px;
  }

  .encabezado,
  .resumen,
  .boton-actualizar,
  .estado-vacio,
  .tarjeta-error {
    max-width: 900px;
    margin-left: auto;
    margin-right: auto;
  }

  .boton-actualizar {
    margin-bottom: 22px;
  }
}
</style>
