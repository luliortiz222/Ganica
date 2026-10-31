
<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>
        <ion-title>Solicitudes de Retiro</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <ion-refresher slot="fixed" @ionRefresh="hacerRecarga($event)">
        <ion-refresher-content
          pulling-text="Deslizá para actualizar..."
          refreshing-spinner="circles"
          refreshing-text="Cargando solicitudes..."
        />
      </ion-refresher>

      <!-- VECINO: CREAR SOLICITUDES Y CONSULTAR LAS PROPIAS -->
      <template v-if="esVecino">
        <h2 class="app-seccion-titulo">Nueva Solicitud</h2>

        <ion-card class="app-card">
          <ion-card-content>
            <form class="app-formulario" @submit.prevent="enviarSolicitud">

              <!-- SERVICIO DE RECOLECCIÓN -->
              <ion-item lines="full">
                <ion-select
                  v-model="form.servicioRecoleccionId"
                  label="Servicio de Recolección"
                  label-placement="stacked"
                  placeholder="Seleccione un servicio"
                  :disabled="cargandoServicios || serviciosSolicitables.length === 0"
                  required
                >
                  <ion-select-option
                    v-for="servicio in serviciosSolicitables"
                    :key="servicio.id"
                    :value="Number(servicio.id)"
                  >
                    {{ servicio.nombre }}
                  </ion-select-option>
                </ion-select>
              </ion-item>

              <ion-note
                v-if="cargandoServicios"
                color="medium"
              >
                Cargando servicios disponibles...
              </ion-note>

              <ion-note
                v-else-if="serviciosSolicitables.length === 0"
                color="warning"
              >
                No hay servicios disponibles para solicitar.
              </ion-note>

              <!-- TIPO DE RESIDUO -->
              <ion-item lines="full">
                <ion-select
                  v-model="form.tipo"
                  label="Tipo de Residuo Especial"
                  label-placement="stacked"
                  placeholder="Seleccione una opción"
                  required
                >
                  <ion-select-option value="Poda y Escombros">
                    Poda y Escombros
                  </ion-select-option>

                  <ion-select-option value="Electrodomésticos-Voluminosos">
                    Electrodomésticos y voluminosos
                  </ion-select-option>

                  <ion-select-option value="Aceite Vegetal Usado">
                    Aceite Vegetal Usado
                  </ion-select-option>
                </ion-select>
              </ion-item>

              <!-- DIRECCIÓN -->
              <ion-item lines="full">
                <ion-input
                  v-model="form.direccion"
                  label="Dirección de Retiro"
                  label-placement="stacked"
                  placeholder="Ej: Av. Mitre 1234"
                  required
                  @ionInput="limpiarCoordenadas"
                />
              </ion-item>

              <!-- UBICACIÓN -->
              <ion-button
                expand="block"
                fill="outline"
                class="app-boton ion-margin-top"
                type="button"
                @click="modalUbicacionAbierto = true"
              >
                <ion-icon :icon="locationOutline" slot="start" />
                Seleccionar ubicación en el mapa
              </ion-button>

              <ion-note
                v-if="form.latitud !== null && form.longitud !== null"
                color="success"
              >
                <p class="coordenadas-seleccionadas">
                  <ion-icon :icon="checkmarkCircleOutline" />
                  Ubicación seleccionada
                </p>

                <p class="coordenadas-seleccionadas">
                  Latitud: {{ form.latitud.toFixed(6) }}
                </p>

                <p class="coordenadas-seleccionadas">
                  Longitud: {{ form.longitud.toFixed(6) }}
                </p>
              </ion-note>

              <!-- OBSERVACIONES -->
              <ion-item lines="full">
                <ion-textarea
                  v-model="form.observaciones"
                  label="Observaciones / Detalles"
                  label-placement="stacked"
                  placeholder="Ej: 2 bolsas grandes de ramas frente al garage"
                  :rows="3"
                />
              </ion-item>

              <!-- RECONOCIMIENTO DE VOZ -->
              <ion-button
                expand="block"
                fill="outline"
                type="button"
                class="app-boton ion-margin-top"
                :disabled="escuchandoVoz"
                @click="dictarObservaciones"
              >
                <ion-icon :icon="micOutline" slot="start" />
                {{
                  escuchandoVoz
                    ? 'Escuchando...'
                    : 'Dictar observaciones'
                }}
              </ion-button>

              <!-- ENVIAR -->
              <ion-button
                expand="block"
                type="submit"
                color="primary"
                class="app-boton ion-margin-top"
                :disabled="
                  enviandoSolicitud ||
                  cargandoServicios ||
                  serviciosSolicitables.length === 0
                "
              >
                <ion-icon :icon="sendOutline" slot="start" />
                {{ enviandoSolicitud ? 'Enviando...' : 'Enviar Solicitud' }}
              </ion-button>
            </form>
          </ion-card-content>
        </ion-card>

        <!-- MIS SOLICITUDES -->
        <div class="encabezado-solicitudes">
          <h3 class="app-seccion-titulo">Mis Solicitudes</h3>

          <ion-button
            fill="clear"
            :disabled="cargandoSolicitudes"
            @click="cargarSolicitudesVecino"
          >
            <ion-icon :icon="refreshOutline" slot="icon-only" />
          </ion-button>
        </div>

        <ion-note
          v-if="!cargandoSolicitudes && solicitudesVecino.length === 0"
          class="app-mensaje-vacio"
        >
          Todavía no generaste solicitudes.
        </ion-note>

        <ion-list v-if="solicitudesVecino.length > 0" lines="full">
          <ion-item
            v-for="item in solicitudesVecino"
            :key="item.id"
            class="app-lista-item"
          >
            <ion-label class="ion-text-wrap">
              <h2><strong>{{ item.tipoResiduo }}</strong></h2>

              <p>📍 {{ item.direccion }}</p>

              <p v-if="item.observaciones">
                💬 {{ item.observaciones }}
              </p>

              <p>
                Fecha:
                {{ formatearFecha(item.fechaCreacion) }}
              </p>

              <p class="estado-actual">
                Estado:
                <strong>{{ item.estado }}</strong>
              </p>

              <div
                v-if="normalizarEstadoSolicitud(item.estado) === 'en camino'"
                class="aviso-firma"
              >
                <ion-icon :icon="checkmarkCircleOutline" />

                <div>
                  <strong>El recolector está en camino</strong>

                  <p>
                    Registrá tu firma de conformidad desde el historial
                    cuando recibas el retiro.
                  </p>
                </div>
              </div>

              <div
                v-if="normalizarEstadoSolicitud(item.estado) === 'finalizada'"
                class="finalizacion-confirmada"
              >
                <ion-icon :icon="checkmarkCircleOutline" />
                Solicitud finalizada
              </div>
            </ion-label>

            <ion-badge
              slot="end"
              :color="obtenerEstado(item.estado).color"
              class="app-estado-badge"
            >
              {{ obtenerEstado(item.estado).texto }}
            </ion-badge>
          </ion-item>
        </ion-list>
      </template>

      <!-- ADMINISTRADOR -->
      <template v-else-if="esAdministrador">
        <div class="encabezado-solicitudes">
          <div>
            <h2 class="app-seccion-titulo">Solicitudes recibidas</h2>

            <p class="texto-actualizacion">
              <ion-spinner
                v-if="cargandoSolicitudes"
                name="crescent"
              />

              {{
                cargandoSolicitudes
                  ? 'Actualizando solicitudes...'
                  : 'Solicitudes generadas por los vecinos'
              }}
            </p>
          </div>

          <ion-button
            fill="clear"
            :disabled="cargandoSolicitudes"
            aria-label="Actualizar solicitudes"
            @click="cargarSolicitudesAdministrador"
          >
            <ion-icon :icon="refreshOutline" slot="icon-only" />
          </ion-button>
        </div>

        <ion-note
          v-if="!cargandoSolicitudes && solicitudesAdministrador.length === 0"
          class="app-mensaje-vacio"
        >
          Todavía no se recibieron solicitudes de los vecinos.
        </ion-note>

        <ion-list
          v-if="solicitudesAdministrador.length > 0"
          lines="full"
        >
          <ion-item
            v-for="item in solicitudesAdministrador"
            :key="item.id"
            class="app-lista-item"
          >
            <ion-label class="ion-text-wrap">
              <h2>
                <strong>{{ item.tipoResiduo }}</strong>
              </h2>

              <p>📍 {{ item.direccion }}</p>

              <p v-if="item.observaciones">
                💬 {{ item.observaciones }}
              </p>

              <p>
                Fecha:
                {{ formatearFecha(item.fechaCreacion) }}
              </p>

              <p class="estado-actual">
                Estado:
                <strong>{{ item.estado }}</strong>
              </p>

              <div
                v-if="normalizarEstadoSolicitud(item.estado) === 'en camino'"
                class="aviso-firma"
              >
                <ion-icon :icon="checkmarkCircleOutline" />

                <div>
                  <strong>Esperando la conformidad del vecino</strong>

                  <p>
                    El vecino debe registrar su firma para finalizar
                    la solicitud.
                  </p>
                </div>
              </div>

              <div
                v-else-if="
                  normalizarEstadoSolicitud(item.estado) === 'finalizada'
                "
                class="finalizacion-confirmada"
              >
                <ion-icon :icon="checkmarkCircleOutline" />
                Retiro finalizado
              </div>
            </ion-label>

            <ion-badge
              slot="end"
              :color="obtenerEstado(item.estado).color"
              class="app-estado-badge"
            >
              {{ obtenerEstado(item.estado).texto }}
            </ion-badge>
          </ion-item>
        </ion-list>
      </template>

      <!-- RECOLECTOR -->
      <template v-else-if="esRecolector">
        <div class="encabezado-solicitudes">
          <div>
            <h2 class="app-seccion-titulo">Solicitudes recibidas</h2>

            <p class="texto-actualizacion">
              <ion-spinner
                v-if="cargandoSolicitudes"
                name="crescent"
              />

              {{
                cargandoSolicitudes
                  ? 'Actualizando solicitudes...'
                  : 'Actualización automática cada 5 segundos'
              }}
            </p>
          </div>

          <ion-button
            fill="clear"
            :disabled="cargandoSolicitudes"
            aria-label="Actualizar solicitudes"
            @click="cargarSolicitudesRecolector"
          >
            <ion-icon :icon="refreshOutline" slot="icon-only" />
          </ion-button>
        </div>

        <ion-note
          v-if="!cargandoSolicitudes && solicitudesRecolector.length === 0"
          class="app-mensaje-vacio"
        >
          No tenés solicitudes recibidas.
          La aplicación seguirá buscando solicitudes nuevas automáticamente.
        </ion-note>

        <ion-list
          v-if="solicitudesRecolector.length > 0"
          lines="full"
        >
          <ion-item
            v-for="item in solicitudesRecolector"
            :key="item.id"
            class="app-lista-item"
          >
            <ion-label class="ion-text-wrap">
              <h2>
                <strong>{{ item.tipoResiduo }}</strong>
              </h2>

              <p>📍 {{ item.direccion }}</p>

              <p v-if="item.observaciones">
                💬 {{ item.observaciones }}
              </p>

              <p>
                Fecha:
                {{ formatearFecha(item.fechaCreacion) }}
              </p>

              <p class="estado-actual">
                Estado:
                <strong>{{ item.estado }}</strong>
              </p>

              <ion-select
                :value="item.estado"
                label="Actualizar estado"
                label-placement="stacked"
                interface="popover"
                :disabled="
                  actualizandoIds.includes(item.id) ||
                  normalizarEstadoSolicitud(item.estado) === 'finalizada'
                "
                @ionChange="
                  actualizarEstadoRecolector(item, $event.detail.value)
                "
              >
                <ion-select-option value="Pendiente">
                  Pendiente
                </ion-select-option>

                <ion-select-option value="Observada">
                  Observada
                </ion-select-option>

                <ion-select-option value="En camino">
                  En camino
                </ion-select-option>
              </ion-select>

              <div
                v-if="normalizarEstadoSolicitud(item.estado) === 'en camino'"
                class="aviso-firma"
              >
                <ion-icon :icon="checkmarkCircleOutline" />

                <div>
                  <strong>Esperando la conformidad del vecino</strong>

                  <p>
                    El vecino registra su firma desde su historial.
                  </p>
                </div>
              </div>

              <div
                v-else-if="
                  normalizarEstadoSolicitud(item.estado) === 'finalizada'
                "
                class="finalizacion-confirmada"
              >
                <ion-icon :icon="checkmarkCircleOutline" />
                Retiro finalizado
              </div>
            </ion-label>
          </ion-item>
        </ion-list>
      </template>

      <ion-note v-else color="warning">
        No se pudo identificar tu rol. Cerrá sesión y volvé a ingresar.
      </ion-note>
    </ion-content>

    <!-- MAPA: SOLAMENTE VECINO -->
    <SelectorUbicacion
      v-if="esVecino"
      :abierto="modalUbicacionAbierto"
      :direccion-inicial="form.direccion"
      :latitud-inicial="form.latitud"
      :longitud-inicial="form.longitud"
      @confirmar="aplicarUbicacion"
      @cerrar="modalUbicacionAbierto = false"
    />
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';

import {
  onIonViewWillEnter,
  onIonViewDidLeave
} from '@ionic/vue';

import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonButtons,
  IonMenuButton,
  IonCard,
  IonCardContent,
  IonItem,
  IonLabel,
  IonInput,
  IonTextarea,
  IonSelect,
  IonSelectOption,
  IonButton,
  IonIcon,
  IonList,
  IonBadge,
  IonNote,
  IonRefresher,
  IonRefresherContent,
  IonSpinner,
  toastController
} from '@ionic/vue';

import {
  sendOutline,
  checkmarkCircleOutline,
  locationOutline,
  micOutline,
  refreshOutline
} from 'ionicons/icons';

import SelectorUbicacion from '@/components/SelectorUbicacion.vue';

import {
  crear_solicitud_api,
  listar_solicitudes_api,
  actualizar_estado_solicitud_api
} from '@/servicies/solicitudes_service';

import type { SolicitudApi } from '@/servicies/solicitudes_service';

import {
  obtener_servicios_solicitables_api
} from '@/servicies/servicios_service';

import type { Servicio } from '@/stores/servicios_store';

import { dictar_texto } from '@/servicies/voz_service';
import { ESTADOS_SOLICITUD } from '@/utils/estados';

interface Ubicacion {
  direccion: string;
  latitud: number;
  longitud: number;
}

const INTERVALO_ACTUALIZACION_MS = 5000;

const form = ref({
  servicioRecoleccionId: null as number | null,
  tipo: '',
  direccion: '',
  observaciones: '',
  latitud: null as number | null,
  longitud: null as number | null
});

const serviciosSolicitables = ref<Servicio[]>([]);
const cargandoServicios = ref(false);

const obtenerRol = (): string => {
  try {
    const usuario = JSON.parse(localStorage.getItem('usuario') || '{}');
    const rol = usuario.rol;

    if (typeof rol === 'string') {
      return rol.trim().toLowerCase();
    }

    if (rol && typeof rol === 'object' && 'codigo' in rol) {
      return String(rol.codigo).trim().toLowerCase();
    }

    return '';
  } catch {
    return '';
  }
};

const rolUsuario = ref(obtenerRol());

const esAdministrador = ref(
  rolUsuario.value === 'administrador'
);

const esVecino = ref(
  rolUsuario.value === 'vecino'
);

const esRecolector = ref(
  rolUsuario.value === 'recolector'
);

const actualizarRol = () => {
  rolUsuario.value = obtenerRol();

  esAdministrador.value =
    rolUsuario.value === 'administrador';

  esVecino.value =
    rolUsuario.value === 'vecino';

  esRecolector.value =
    rolUsuario.value === 'recolector';
};

const normalizarEstadoSolicitud = (estado: unknown): string =>
  String(estado ?? '').trim().toLowerCase();

const cargandoSolicitudes = ref(false);
const enviandoSolicitud = ref(false);

const solicitudesVecino = ref<SolicitudApi[]>([]);
const solicitudesAdministrador = ref<SolicitudApi[]>([]);
const solicitudesRecolector = ref<SolicitudApi[]>([]);

const actualizandoIds = ref<number[]>([]);

const modalUbicacionAbierto = ref(false);
const escuchandoVoz = ref(false);

let intervaloActualizacion: ReturnType<typeof setInterval> | null = null;
let paginaActiva = false;

const ESTADO_POR_DEFECTO = {
  color: 'medium',
  texto: 'En revisión',
  icono: checkmarkCircleOutline
};

const obtenerEstado = (estado: string) => {
  return (
    ESTADOS_SOLICITUD[
      estado as keyof typeof ESTADOS_SOLICITUD
    ] ?? ESTADO_POR_DEFECTO
  );
};

const formatearFecha = (fecha: unknown): string => {
  if (!fecha) return 'Sin fecha';

  const valor = new Date(String(fecha));

  if (Number.isNaN(valor.getTime())) {
    return String(fecha);
  }

  return valor.toLocaleDateString('es-AR');
};

const mostrarToast = async (
  mensaje: string,
  color: 'success' | 'warning' | 'danger'
) => {
  const toast = await toastController.create({
    message: mensaje,
    duration: 2500,
    color,
    position: 'bottom'
  });

  await toast.present();
};

// VECINO: cargar únicamente los servicios que requieren solicitud.
const cargarServiciosSolicitables = async () => {
  if (!esVecino.value || cargandoServicios.value) return;

  cargandoServicios.value = true;

  try {
    serviciosSolicitables.value =
      await obtener_servicios_solicitables_api();
  } catch (error) {
    console.error('Error al cargar servicios solicitables:', error);

    await mostrarToast(
      'No se pudieron cargar los servicios disponibles.',
      'warning'
    );
  } finally {
    cargandoServicios.value = false;
  }
};

const aplicarUbicacion = (ubicacion: Ubicacion) => {
  if (!esVecino.value) return;

  form.value.direccion = ubicacion.direccion;
  form.value.latitud = ubicacion.latitud;
  form.value.longitud = ubicacion.longitud;
};

const limpiarCoordenadas = () => {
  form.value.latitud = null;
  form.value.longitud = null;
};

const dictarObservaciones = async () => {
  if (!esVecino.value || escuchandoVoz.value) return;

  escuchandoVoz.value = true;

  try {
    const textoReconocido = await dictar_texto();

    if (textoReconocido.trim()) {
      const textoActual = form.value.observaciones.trim();
      const textoNuevo = textoReconocido.trim();

      form.value.observaciones = textoActual
        ? `${textoActual} ${textoNuevo}`
        : textoNuevo;

      await mostrarToast(
        'Texto agregado a las observaciones.',
        'success'
      );
    }
  } catch (error: unknown) {
    console.error('Error en el reconocimiento de voz:', error);

    await mostrarToast(
      error instanceof Error
        ? error.message
        : 'No se pudo utilizar el reconocimiento de voz.',
      'warning'
    );
  } finally {
    escuchandoVoz.value = false;
  }
};

// VECINO: consultar sus propias solicitudes.
const cargarSolicitudesVecino = async () => {
  if (!esVecino.value || cargandoSolicitudes.value) return;

  cargandoSolicitudes.value = true;

  try {
    solicitudesVecino.value = await listar_solicitudes_api();
  } catch (error) {
    console.error(
      'Error al consultar solicitudes del vecino:',
      error
    );

    await mostrarToast(
      'No se pudieron cargar tus solicitudes.',
      'warning'
    );
  } finally {
    cargandoSolicitudes.value = false;
  }
};

// ADMINISTRADOR: solamente consultar todas las solicitudes.
const cargarSolicitudesAdministrador = async () => {
  if (!esAdministrador.value || cargandoSolicitudes.value) return;

  cargandoSolicitudes.value = true;

  try {
    solicitudesAdministrador.value = await listar_solicitudes_api();
  } catch (error) {
    console.error(
      'Error al consultar solicitudes del administrador:',
      error
    );

    await mostrarToast(
      'No se pudieron cargar las solicitudes recibidas.',
      'warning'
    );
  } finally {
    cargandoSolicitudes.value = false;
  }
};

// RECOLECTOR: consultar solicitudes notificadas.
const cargarSolicitudesRecolector = async () => {
  if (!esRecolector.value || cargandoSolicitudes.value) return;

  cargandoSolicitudes.value = true;

  try {
    solicitudesRecolector.value = await listar_solicitudes_api();
  } catch (error) {
    console.error(
      'Error al cargar solicitudes del recolector:',
      error
    );
  } finally {
    cargandoSolicitudes.value = false;
  }
};

// Actualización automática exclusiva del recolector.
const iniciarActualizacionAutomatica = () => {
  if (intervaloActualizacion !== null) return;

  paginaActiva = true;

  void cargarSolicitudesRecolector();

  intervaloActualizacion = setInterval(() => {
    if (paginaActiva && !cargandoSolicitudes.value) {
      void cargarSolicitudesRecolector();
    }
  }, INTERVALO_ACTUALIZACION_MS);
};

const detenerActualizacionAutomatica = () => {
  paginaActiva = false;

  if (intervaloActualizacion !== null) {
    clearInterval(intervaloActualizacion);
    intervaloActualizacion = null;
  }
};

// SOLAMENTE EL RECOLECTOR PUEDE ACTUALIZAR ESTADOS.
const actualizarEstadoRecolector = async (
  item: SolicitudApi,
  estadoNuevo: string | undefined
) => {
  if (!esRecolector.value) return;

  if (
    !estadoNuevo ||
    estadoNuevo === item.estado ||
    normalizarEstadoSolicitud(estadoNuevo) === 'finalizada'
  ) {
    return;
  }

  if (actualizandoIds.value.includes(item.id)) return;

  actualizandoIds.value.push(item.id);

  try {
    await actualizar_estado_solicitud_api(item.id, {
      estado: estadoNuevo,
      observacion: `Estado actualizado a ${estadoNuevo} desde la aplicación.`
    });

    await cargarSolicitudesRecolector();

    await mostrarToast(
      'Estado actualizado correctamente.',
      'success'
    );
  } catch (error) {
    console.error('Error al actualizar el estado:', error);

    await mostrarToast(
      error instanceof Error
        ? error.message
        : 'No se pudo actualizar el estado.',
      'danger'
    );

    await cargarSolicitudesRecolector();
  } finally {
    actualizandoIds.value = actualizandoIds.value.filter(
      id => id !== item.id
    );
  }
};

const hacerRecarga = async (event: CustomEvent) => {
  try {
    actualizarRol();

    if (esVecino.value) {
      await Promise.all([
        cargarServiciosSolicitables(),
        cargarSolicitudesVecino()
      ]);
    } else if (esAdministrador.value) {
      await cargarSolicitudesAdministrador();
    } else if (esRecolector.value) {
      await cargarSolicitudesRecolector();
    }
  } finally {
    event.detail.complete();
  }
};

const limpiarFormulario = () => {
  form.value = {
    servicioRecoleccionId: null,
    tipo: '',
    direccion: '',
    observaciones: '',
    latitud: null,
    longitud: null
  };
};

// SOLAMENTE EL VECINO PUEDE CREAR SOLICITUDES.
const enviarSolicitud = async () => {
  if (!esVecino.value) {
    await mostrarToast(
      'Solamente los vecinos pueden crear solicitudes.',
      'danger'
    );
    return;
  }

  if (enviandoSolicitud.value) return;

  if (
    !form.value.servicioRecoleccionId ||
    !form.value.tipo ||
    !form.value.direccion.trim()
  ) {
    await mostrarToast(
      'Seleccioná un servicio, el tipo de residuo y completá la dirección.',
      'warning'
    );
    return;
  }

  enviandoSolicitud.value = true;

  try {
    const datosSolicitud = {
      servicioRecoleccionId: Number(form.value.servicioRecoleccionId),
      tipoResiduo: form.value.tipo,
      direccion: form.value.direccion.trim(),
      observaciones: form.value.observaciones,
      latitud: form.value.latitud,
      longitud: form.value.longitud
    };

    console.log(
      'Enviando solicitud del vecino:',
      datosSolicitud
    );

    const response = await crear_solicitud_api(datosSolicitud);

    console.log('Solicitud creada:', response);

    limpiarFormulario();

    await cargarSolicitudesVecino();

    await mostrarToast(
      '¡Solicitud registrada! El administrador y los recolectores fueron notificados.',
      'success'
    );
  } catch (error: unknown) {
    console.error('Error al enviar solicitud:', error);

    await mostrarToast(
      error instanceof Error
        ? error.message
        : 'No se pudo registrar la solicitud.',
      'danger'
    );
  } finally {
    enviandoSolicitud.value = false;
  }
};

// Actualizar la lista cuando se produce una sincronización.
const actualizarDesdeSincronizacion = () => {
  if (esVecino.value) {
    void cargarSolicitudesVecino();
  } else if (esAdministrador.value) {
    void cargarSolicitudesAdministrador();
  } else if (esRecolector.value && paginaActiva) {
    void cargarSolicitudesRecolector();
  }
};

onIonViewWillEnter(() => {
  actualizarRol();

  if (esVecino.value) {
    void cargarServiciosSolicitables();
    void cargarSolicitudesVecino();
  } else if (esAdministrador.value) {
    void cargarSolicitudesAdministrador();
  } else if (esRecolector.value) {
    iniciarActualizacionAutomatica();
  }
});

onIonViewDidLeave(() => {
  detenerActualizacionAutomatica();
});

onMounted(() => {
  window.addEventListener(
    'solicitudes-sincronizadas',
    actualizarDesdeSincronizacion
  );
});

onUnmounted(() => {
  detenerActualizacionAutomatica();

  window.removeEventListener(
    'solicitudes-sincronizadas',
    actualizarDesdeSincronizacion
  );
});
</script>

<style scoped>
ion-card-content {
  padding: 16px;
}

ion-item h2 {
  font-size: 1rem;
  margin-bottom: 8px;
}

ion-item p {
  white-space: normal;
  overflow-wrap: anywhere;
  margin: 6px 0;
}

ion-badge {
  flex-shrink: 0;
  margin-inline-start: 8px;
}

.app-estado-badge ion-icon {
  font-size: 15px;
}

.coordenadas-seleccionadas {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 6px 0;
  font-size: 13px;
}

.estado-actual {
  margin-top: 12px;
}

.encabezado-solicitudes {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.texto-actualizacion {
  display: flex;
  align-items: center;
  gap: 7px;
  margin: -8px 0 16px;
  color: var(--ion-color-medium);
  font-size: 12px;
}

.texto-actualizacion ion-spinner {
  width: 15px;
  height: 15px;
}

.aviso-firma {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  margin-top: 14px;
  padding: 12px;
  background: #f0f8f2;
  border: 1px solid #d4e9da;
  border-radius: 12px;
  color: #176b45;
}

.aviso-firma > ion-icon {
  flex-shrink: 0;
  margin-top: 2px;
  font-size: 22px;
}

.aviso-firma strong {
  display: block;
  font-size: 13px;
}

.aviso-firma p {
  margin: 5px 0 0;
  color: #53715e;
  font-size: 12px;
  line-height: 1.5;
}

.finalizacion-confirmada {
  display: flex;
  align-items: center;
  gap: 7px;
  margin-top: 12px;
  padding: 10px 12px;
  color: #176b45;
  background: #e5f4e9;
  border-radius: 10px;
  font-size: 13px;
  font-weight: 650;
}
</style>

