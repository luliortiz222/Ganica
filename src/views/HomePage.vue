
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
  IonItem,
  IonLabel,
  onIonViewWillEnter,
  alertController
} from '@ionic/vue';

import {
  sunnyOutline,
  moonOutline,
  alertCircleOutline,
  trashOutline,
  addOutline,
  createOutline,
  trashBinOutline,
  notificationsOutline
} from 'ionicons/icons';

import { obtenerTemaGuardado, aplicarTema } from '@/config/tema';
import { servicios_store, Servicio } from '@/stores/servicios_store';
import ModalServicio from '@/components/ModalServicio.vue';

import {
  obtener_bandeja_notificaciones,
  marcar_notificacion_leida
} from '@/servicies/notificaciones_service';

import {
  actualizar_estado_solicitud_api
} from '@/servicies/solicitudes_service';

const esOscuro = ref(false);

// ==========================================
// CONTROL DEL MODAL DE SERVICIOS
// ==========================================

const modalAbierto = ref(false);
const servicioAEditar = ref<Servicio | null>(null);

// ==========================================
// ESTADO DE NOTIFICACIONES
// ==========================================

const notificaciones = ref<any[]>([]);
const mostrarAvisos = ref(false);
const estadosActualizando = ref<number[]>([]);

// ==========================================
// USUARIO ACTUAL
// ==========================================

const obtenerUsuarioActual = () => {
  const usuarioGuardado = localStorage.getItem('usuario');

  if (!usuarioGuardado) return null;

  try {
    return JSON.parse(usuarioGuardado);
  } catch {
    return null;
  }
};

const obtenerRolActual = (): string => {
  const usuario = obtenerUsuarioActual();
  return String(usuario?.rol ?? '').toLowerCase();
};

const esRecolector = computed(
  () => obtenerRolActual() === 'recolector'
);

// ==========================================
// CARGAR NOTIFICACIONES
// ==========================================

const cargarNotificaciones = async () => {
  const usuarioActual = obtenerUsuarioActual();

  if (!usuarioActual?.id) {
    notificaciones.value = [];
    return;
  }

  try {
    const res = await obtener_bandeja_notificaciones(usuarioActual.id);

    if (res.ok) {
      notificaciones.value = res.notificaciones;
    }
  } catch (error) {
    console.error('Error al cargar las notificaciones:', error);
  }
};

// ==========================================
// ABRIR BANDEJA DE AVISOS
// ==========================================

const abrirBandeja = async () => {
  await cargarNotificaciones();
  mostrarAvisos.value = true;
};

// ==========================================
// CERRAR BANDEJA
// ==========================================

const cerrarBandeja = () => {
  mostrarAvisos.value = false;
};

// ==========================================
// MARCAR NOTIFICACIÓN COMO LEÍDA
// ==========================================

const marcarComoLeida = async (notificacion: any) => {
  if (notificacion.leidaEn) return;

  try {
    const res = await marcar_notificacion_leida(notificacion.id);

    if (res.ok) {
      notificacion.leidaEn = new Date().toISOString();
    } else {
      const alerta = await alertController.create({
        header: 'No se pudo marcar como leída',
        message: 'El servidor no pudo actualizar la notificación.',
        buttons: ['Aceptar'],
        cssClass: 'alerta-ganica'
      });

      await alerta.present();
    }
  } catch (error) {
    console.error('Error al marcar la notificación como leída:', error);

    const alerta = await alertController.create({
      header: 'No se pudo marcar como leída',
      message: 'Verificá tu conexión e intentá nuevamente.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
  }
};

// ==========================================
// ACTUALIZAR ESTADO DE UNA SOLICITUD
// ==========================================

const actualizarEstadoSolicitud = async (
  notificacion: any,
  estado: 'Observada' | 'En camino' | 'Finalizada'
) => {
  const solicitudId = Number(notificacion.solicitudId);

  if (!Number.isInteger(solicitudId) || solicitudId <= 0) {
    const alerta = await alertController.create({
      header: 'Solicitud no identificada',
      message: 'Esta notificación no contiene un identificador válido de solicitud.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
    return;
  }

  if (!notificacion.leidaEn) {
    return;
  }

  if (!esRecolector.value && obtenerRolActual() !== 'administrador') {
    const alerta = await alertController.create({
      header: 'Acceso restringido',
      message: 'No tenés permisos para modificar el estado de la solicitud.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
    return;
  }

  if (estadosActualizando.value.includes(solicitudId)) {
    return;
  }

  estadosActualizando.value.push(solicitudId);

  try {
    await actualizar_estado_solicitud_api(solicitudId, {
      estado,
      observacion: `Estado actualizado a "${estado}" desde la notificación.`
    });

    notificacion.estadoSolicitud = estado;

    const alerta = await alertController.create({
      header: 'Estado actualizado',
      message: `La solicitud #${solicitudId} ahora está "${estado}".`,
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();

    // Volver a consultar los datos disponibles en el servidor.
    await cargarNotificaciones();
  } catch (error) {
    console.error('Error al actualizar el estado de la solicitud:', error);

    const alerta = await alertController.create({
      header: 'No se pudo actualizar',
      message:
        'No se pudo cambiar el estado de la solicitud. Verificá tu conexión y tus permisos e intentá nuevamente.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
  } finally {
    estadosActualizando.value = estadosActualizando.value.filter(
      id => id !== solicitudId
    );
  }
};

// ==========================================
// COMPUTED DE SERVICIOS
// ==========================================

const cargando = computed(() => servicios_store.cargando);
const error = computed(() => servicios_store.error);
const hay_servicios = computed(() => servicios_store.hay_servicios);
const lista = computed(() => servicios_store.lista);

// ==========================================
// CANTIDAD DE NOTIFICACIONES SIN LEER
// ==========================================

const notificacionesNoLeidas = computed(() => {
  return notificaciones.value.filter(n => !n.leidaEn).length;
});

// ==========================================
// REINTENTAR CARGA DE SERVICIOS
// ==========================================

const reintentar = () => {
  servicios_store.cargar_servicios();
};

// ==========================================
// VALIDACIÓN DE ADMINISTRADOR
// ==========================================

const verificarPermisoAdministrador = async (): Promise<boolean> => {
  const usuarioGuardado = localStorage.getItem('usuario');

  if (!usuarioGuardado) {
    const alerta = await alertController.create({
      header: 'Sesión no identificada',
      message: 'Iniciá sesión para continuar.',
      buttons: [
        {
          text: 'Entendido',
          role: 'cancel'
        }
      ],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
    return false;
  }

  try {
    const parsed = JSON.parse(usuarioGuardado);

    if (parsed.rol !== 'administrador') {
      const alerta = await alertController.create({
        header: '🛡️ Acceso restringido',
        message:
          'No tenés permisos para administrar los servicios de recolección. Esta acción está reservada para el administrador.',
        buttons: [
          {
            text: 'Entendido',
            role: 'cancel',
            cssClass: 'boton-alerta-ganica'
          }
        ],
        cssClass: 'alerta-ganica'
      });

      await alerta.present();
      return false;
    }

    return true;
  } catch {
    const alerta = await alertController.create({
      header: 'No se pudo verificar tu acceso',
      message: 'Volvé a iniciar sesión e intentá nuevamente.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
    return false;
  }
};

// ==========================================
// NUEVO SERVICIO
// ==========================================

const abrirModalNuevo = async () => {
  const esAdmin = await verificarPermisoAdministrador();

  if (!esAdmin) return;

  servicioAEditar.value = null;
  modalAbierto.value = true;
};

// ==========================================
// EDITAR SERVICIO
// ==========================================

const abrirModalEditar = async (servicio: Servicio) => {
  const esAdmin = await verificarPermisoAdministrador();

  if (!esAdmin) return;

  servicioAEditar.value = servicio;
  modalAbierto.value = true;
};

// ==========================================
// DAR DE BAJA SERVICIO
// ==========================================

const darDeBaja = async (id: string | number) => {
  const esAdmin = await verificarPermisoAdministrador();

  if (!esAdmin) return;

  if (confirm('¿Estás seguro de que querés dar de baja este servicio?')) {
    await servicios_store.eliminar_servicio(id);
  }
};

// ==========================================
// AL CARGAR LA PÁGINA
// ==========================================

onMounted(() => {
  esOscuro.value = obtenerTemaGuardado();
});

// ==========================================
// CADA VEZ QUE ENTRAMOS A LA PÁGINA
// ==========================================

onIonViewWillEnter(async () => {
  servicios_store.cargar_servicios();
  await cargarNotificaciones();
});

// ==========================================
// CAMBIAR TEMA
// ==========================================

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
      <!-- BANDEJA DE AVISOS -->

      <ion-card
        class="tarjeta-bandeja"
        @click="abrirBandeja"
      >
        <ion-item lines="none">
          <ion-icon
            :icon="notificationsOutline"
            slot="start"
            color="primary"
          />

          <ion-label>
            <h3>Bandeja de Avisos</h3>
            <p>
              <strong>{{ notificacionesNoLeidas }}</strong>
              notificaciones sin leer
            </p>
          </ion-label>
        </ion-item>
      </ion-card>

      <!-- VENTANA DE AVISOS -->

      <div v-if="mostrarAvisos" class="bandeja-avisos">
        <ion-card>
          <ion-card-header>
            <ion-card-title>
              Avisos de Recolección
            </ion-card-title>
          </ion-card-header>

          <ion-card-content>
            <div
              v-if="notificaciones.length === 0"
              class="estado-avisos"
            >
              <ion-icon
                :icon="notificationsOutline"
                class="icono-vacio"
              />

              <p>No tenés avisos pendientes.</p>
            </div>

            <div v-else>
              <ion-card
                v-for="notificacion in notificaciones"
                :key="notificacion.id"
                class="notificacion-card"
              >
                <ion-card-header>
                  <ion-card-title>
                    {{ notificacion.titulo }}
                  </ion-card-title>
                </ion-card-header>

                <ion-card-content>
                  <p>{{ notificacion.mensaje }}</p>

                  <p
                    v-if="notificacion.creadoEn"
                    class="fecha-notificacion"
                  >
                    <strong>Recibido:</strong>
                    {{ new Date(notificacion.creadoEn).toLocaleString() }}
                  </p>

                  <ion-button
                    v-if="!notificacion.leidaEn"
                    size="small"
                    color="primary"
                    @click.stop="marcarComoLeida(notificacion)"
                  >
                    Marcar como leído
                  </ion-button>

                  <p
                    v-else
                    class="aviso-leido"
                  >
                    ✓ Aviso leído
                  </p>

                  <!-- ESTADOS DE LA SOLICITUD -->

                  <div
                    v-if="
                      notificacion.leidaEn &&
                      notificacion.solicitudId &&
                      (esRecolector || obtenerRolActual() === 'administrador')
                    "
                    class="acciones-solicitud"
                  >
                    <p>
                      <strong>
                        Solicitud #{{ notificacion.solicitudId }}
                      </strong>
                    </p>

                    <p class="estado-actual">
                      Estado mostrado:
                      <strong>
                        {{ notificacion.estadoSolicitud || 'Sin actualizar en esta bandeja' }}
                      </strong>
                    </p>

                    <p class="indicacion-estados">
                      Seleccioná el nuevo estado de la solicitud:
                    </p>

                    <div class="botones-estados">
                      <ion-button
                        size="small"
                        fill="outline"
                        color="warning"
                        :disabled="
                          estadosActualizando.includes(Number(notificacion.solicitudId)) ||
                          notificacion.estadoSolicitud === 'Finalizada'
                        "
                        @click.stop="actualizarEstadoSolicitud(notificacion, 'Observada')"
                      >
                        Observada
                      </ion-button>

                      <ion-button
                        size="small"
                        fill="outline"
                        color="primary"
                        :disabled="
                          estadosActualizando.includes(Number(notificacion.solicitudId)) ||
                          notificacion.estadoSolicitud === 'Finalizada'
                        "
                        @click.stop="actualizarEstadoSolicitud(notificacion, 'En camino')"
                      >
                        En camino
                      </ion-button>

                      <ion-button
                        size="small"
                        fill="solid"
                        color="success"
                        :disabled="
                          estadosActualizando.includes(Number(notificacion.solicitudId)) ||
                          notificacion.estadoSolicitud === 'Finalizada'
                        "
                        @click.stop="actualizarEstadoSolicitud(notificacion, 'Finalizada')"
                      >
                        Finalizada
                      </ion-button>
                    </div>
                  </div>
                </ion-card-content>
              </ion-card>
            </div>

            <ion-button
              expand="block"
              fill="outline"
              @click="cerrarBandeja"
            >
              Cerrar bandeja
            </ion-button>
          </ion-card-content>
        </ion-card>
      </div>

      <!-- TÍTULO SERVICIOS -->

      <div class="app-portada">
        <ion-title size="large" class="ion-no-padding">
          Servicios de Recolección
        </ion-title>
      </div>

      <!-- ESTADO CARGANDO -->

      <div v-if="cargando">
        <ion-card v-for="n in 3" :key="n">
          <ion-card-header>
            <ion-card-title>
              <ion-skeleton-text
                animated
                style="width: 60%; height: 20px;"
              />
            </ion-card-title>
          </ion-card-header>

          <ion-card-content>
            <ion-skeleton-text
              animated
              style="width: 90%;"
            />
            <ion-skeleton-text
              animated
              style="width: 45%; margin-top: 8px;"
            />
            <ion-skeleton-text
              animated
              style="width: 50%; margin-top: 4px;"
            />
          </ion-card-content>
        </ion-card>
      </div>

      <!-- ESTADO ERROR -->

      <div v-else-if="error" class="estado-contenedor">
        <ion-icon
          :icon="alertCircleOutline"
          class="icono-error"
        />

        <h2>Error de conexión</h2>
        <p>{{ error }}</p>

        <ion-button
          color="primary"
          fill="solid"
          @click="reintentar"
        >
          REINTENTAR
        </ion-button>
      </div>

      <!-- ESTADO VACÍO -->

      <div v-else-if="!hay_servicios" class="estado-contenedor">
        <ion-icon
          :icon="trashOutline"
          class="icono-vacio"
        />

        <p>No hay servicios disponibles en este momento.</p>
      </div>

      <!-- LISTA DE SERVICIOS -->

      <div v-else>
        <ion-card v-for="s in lista" :key="s.id">
          <ion-card-header>
            <div class="encabezado-servicio">
              <ion-card-title>
                {{ s.nombre || s.titulo }}
              </ion-card-title>

              <div>
                <ion-button
                  fill="clear"
                  size="small"
                  @click="abrirModalEditar(s)"
                >
                  <ion-icon
                    slot="icon-only"
                    :icon="createOutline"
                  />
                </ion-button>

                <ion-button
                  fill="clear"
                  color="danger"
                  size="small"
                  @click="darDeBaja(s.id)"
                >
                  <ion-icon
                    slot="icon-only"
                    :icon="trashBinOutline"
                  />
                </ion-button>
              </div>
            </div>
          </ion-card-header>

          <ion-card-content>
            <p>{{ s.descripcion || s.detalle }}</p>

            <p v-if="s.dias" class="detalle-servicio">
              <strong>Días:</strong> {{ s.dias }}
            </p>

            <p v-if="s.horario">
              <strong>Horario:</strong> {{ s.horario }}
            </p>
          </ion-card-content>
        </ion-card>
      </div>

      <!-- BOTÓN AGREGAR SERVICIO -->

      <ion-fab
        vertical="bottom"
        horizontal="end"
        slot="fixed"
      >
        <ion-fab-button
          color="primary"
          @click="abrirModalNuevo"
        >
          <ion-icon :icon="addOutline" />
        </ion-fab-button>
      </ion-fab>

      <!-- MODAL DE SERVICIOS -->

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

.tarjeta-bandeja {
  margin-bottom: 20px;
  cursor: pointer;
}

.bandeja-avisos {
  margin-bottom: 20px;
}

.notificacion-card {
  margin-top: 12px;
}

.fecha-notificacion {
  margin-top: 12px;
  font-size: 0.85rem;
  color: var(--ion-color-medium);
}

.aviso-leido {
  margin-top: 10px;
  color: var(--ion-color-success);
  font-weight: 600;
}

.acciones-solicitud {
  margin-top: 16px;
  padding: 12px;
  border-radius: 12px;
  background: var(--ion-color-light);
}

.estado-actual,
.indicacion-estados {
  margin-top: 8px;
}

.botones-estados {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 10px;
}

.botones-estados ion-button {
  margin: 0;
}

.estado-avisos {
  text-align: center;
  padding: 20px;
}

.encabezado-servicio {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}

.detalle-servicio {
  margin-top: 8px;
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

/* Alertas de permisos de GANICA */

:global(.alerta-ganica .alert-wrapper) {
  border-radius: 20px;
  --background: var(--ion-background-color, #ffffff);
}

:global(.alerta-ganica .alert-head) {
  padding-bottom: 12px;
}

:global(.alerta-ganica .alert-title) {
  font-weight: 700;
  color: var(--ion-color-primary);
}

:global(.alerta-ganica .alert-message) {
  display: block;
  visibility: visible;
  opacity: 1;
  white-space: normal;
  line-height: 1.6;
  color: var(--ion-text-color, #222222);
}

:global(.alerta-ganica .alert-button) {
  font-weight: 700;
  text-transform: none;
}
</style>
