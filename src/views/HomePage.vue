
<script setup lang="ts">
import {
  ref,
  computed,
  onMounted,
  onUnmounted
} from 'vue';

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
  refreshOutline,
  sunnyOutline,
  moonOutline,
  alertCircleOutline,
  trashOutline,
  addOutline,
  createOutline,
  trashBinOutline,
  notificationsOutline,
  addCircleOutline,
  timeOutline,
  personOutline,
  locationOutline
} from 'ionicons/icons';

import { useRouter } from 'vue-router';

import {
  obtenerTemaGuardado,
  aplicarTema
} from '@/config/tema';

import {
  servicios_store,
  Servicio
} from '@/stores/servicios_store';

import ModalServicio from '@/components/ModalServicio.vue';

import {
  obtener_bandeja_notificaciones,
  marcar_notificacion_leida
} from '@/servicies/notificaciones_service';

import {
  actualizar_estado_solicitud_api
} from '@/servicies/solicitudes_service';

const router = useRouter();

const esOscuro = ref(false);
const recargandoInicio = ref(false);

// ==========================================
// USUARIO Y ROLES
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
  const rol = usuario?.rol;

  // Admite tanto rol como texto como rol.codigo.
  if (typeof rol === 'string') {
    return rol.trim().toLowerCase();
  }

  if (rol && typeof rol === 'object' && 'codigo' in rol) {
    return String(rol.codigo).trim().toLowerCase();
  }

  return '';
};

// El usuario y el rol son reactivos.
const usuarioSesion = ref(obtenerUsuarioActual());
const rolSesion = ref(obtenerRolActual());

// Actualiza ambos datos cuando cambia la sesión.
const actualizarRolSesion = () => {
  usuarioSesion.value = obtenerUsuarioActual();
  rolSesion.value = obtenerRolActual();
};

const esAdministrador = computed(
  () => rolSesion.value === 'administrador'
);

const esRecolector = computed(
  () => rolSesion.value === 'recolector'
);

const esVecino = computed(
  () => rolSesion.value === 'vecino'
);

const puedeGestionarSolicitudes = computed(
  () => esAdministrador.value || esRecolector.value
);

// El saludo ahora utiliza el usuario reactivo.
const nombreUsuario = computed(() => {
  const usuario = usuarioSesion.value;

  return (
    usuario?.nombre ||
    usuario?.Nombre ||
    usuario?.email ||
    usuario?.Email ||
    'Usuario'
  );
});

// ==========================================
// RECARGAR INICIO
// ==========================================

const recargarInicio = async () => {
  if (recargandoInicio.value) return;

  recargandoInicio.value = true;

  try {
    // Leer el usuario y el rol actuales.
    actualizarRolSesion();

    // Actualizar el tema.
    esOscuro.value = obtenerTemaGuardado();

    // Recargar los servicios según el rol.
    if (!esVecino.value) {
      await servicios_store.cargar_servicios();
    }

    // Actualizar las notificaciones.
    await cargarNotificaciones();
  } catch (error) {
    console.error('Error al recargar Inicio:', error);
  } finally {
    recargandoInicio.value = false;
  }
};

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
// CARGAR NOTIFICACIONES
// ==========================================

const cargarNotificaciones = async () => {
  if (!puedeGestionarSolicitudes.value) {
    notificaciones.value = [];
    return;
  }

  const usuarioActual = obtenerUsuarioActual();

  if (!usuarioActual?.id) {
    notificaciones.value = [];
    return;
  }

  try {
    const res = await obtener_bandeja_notificaciones(
      usuarioActual.id
    );

    if (res.ok) {
      notificaciones.value = res.notificaciones ?? [];
    }
  } catch (error) {
    console.error(
      'Error al cargar las notificaciones:',
      error
    );
  }
};

// ==========================================
// BANDEJA DE AVISOS
// ==========================================

const abrirBandeja = async () => {
  await cargarNotificaciones();
  mostrarAvisos.value = true;
};

const cerrarBandeja = () => {
  mostrarAvisos.value = false;
};

// ==========================================
// MARCAR NOTIFICACIÓN COMO LEÍDA
// ==========================================

const marcarComoLeida = async (notificacion: any) => {
  if (notificacion.leidaEn) return;

  try {
    const res = await marcar_notificacion_leida(
      notificacion.id
    );

    if (res.ok) {
      notificacion.leidaEn = new Date().toISOString();
    } else {
      const alerta = await alertController.create({
        header: 'No se pudo marcar como leída',
        message:
          'El servidor no pudo actualizar la notificación.',
        buttons: ['Aceptar'],
        cssClass: 'alerta-ganica'
      });

      await alerta.present();
    }
  } catch (error) {
    console.error(
      'Error al marcar la notificación como leída:',
      error
    );

    const alerta = await alertController.create({
      header: 'No se pudo marcar como leída',
      message:
        'Verificá tu conexión e intentá nuevamente.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
  }
};

// ==========================================
// ACTUALIZAR ESTADO DE UNA SOLICITUD
// La firma del vecino es la que finaliza el retiro.
// ==========================================

const actualizarEstadoSolicitud = async (
  notificacion: any,
  estado: 'Observada' | 'En camino'
) => {
  const solicitudId = Number(notificacion.solicitudId);

  if (
    !Number.isInteger(solicitudId) ||
    solicitudId <= 0
  ) {
    const alerta = await alertController.create({
      header: 'Solicitud no identificada',
      message:
        'Esta notificación no contiene un identificador válido de solicitud.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
    return;
  }

  if (!notificacion.leidaEn) {
    const alerta = await alertController.create({
      header: 'Leé primero el aviso',
      message:
        'Marcá la notificación como leída antes de actualizar el estado.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
    return;
  }

  if (!puedeGestionarSolicitudes.value) {
    const alerta = await alertController.create({
      header: 'Acceso restringido',
      message:
        'No tenés permisos para modificar el estado de la solicitud.',
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
      observacion:
        `Estado actualizado a "${estado}" desde la notificación.`
    });

    notificacion.estadoSolicitud = estado;

    const alerta = await alertController.create({
      header: 'Estado actualizado',
      message:
        `La solicitud #${solicitudId} ahora está "${estado}".`,
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();

    await cargarNotificaciones();
  } catch (error) {
    console.error(
      'Error al actualizar el estado de la solicitud:',
      error
    );

    const alerta = await alertController.create({
      header: 'No se pudo actualizar',
      message:
        'No se pudo cambiar el estado de la solicitud. Verificá tu conexión y tus permisos e intentá nuevamente.',
      buttons: ['Aceptar'],
      cssClass: 'alerta-ganica'
    });

    await alerta.present();
  } finally {
    estadosActualizando.value =
      estadosActualizando.value.filter(
        id => id !== solicitudId
      );
  }
};

// ==========================================
// COMPUTED DE SERVICIOS
// ==========================================

const cargando = computed(
  () => servicios_store.cargando
);

const error = computed(
  () => servicios_store.error
);

const hay_servicios = computed(
  () => servicios_store.hay_servicios
);

const lista = computed(
  () => servicios_store.lista
);

// ==========================================
// NOTIFICACIONES SIN LEER
// ==========================================

const notificacionesNoLeidas = computed(() => {
  return notificaciones.value.filter(
    n => !n.leidaEn
  ).length;
});

// ==========================================
// NAVEGACIÓN DEL VECINO
// ==========================================

const irANuevaSolicitud = () => {
  router.push('/nueva-solicitud');
};

const irAHistorial = () => {
  router.push('/historial-solicitudes');
};

const irAMiCuenta = () => {
  router.push('/micuenta');
};

const irAPuntos = () => {
  router.push('/puntos');
};

const irASolicitudes = () => {
  router.push('/solicitudes');
};

// ==========================================
// REINTENTAR CARGA DE SERVICIOS
// ==========================================

const reintentar = () => {
  servicios_store.cargar_servicios();
};

// ==========================================
// VALIDACIÓN DE ADMINISTRADOR
// ==========================================

const verificarPermisoAdministrador =
  async (): Promise<boolean> => {
    const usuarioGuardado =
      localStorage.getItem('usuario');

    if (!usuarioGuardado) {
      const alerta = await alertController.create({
        header: 'Sesión no identificada',
        message: 'Iniciá sesión para continuar.',
        buttons: ['Entendido'],
        cssClass: 'alerta-ganica'
      });

      await alerta.present();
      return false;
    }

    try {
      const usuario = JSON.parse(usuarioGuardado);
      const rol = usuario?.rol;

      const rolNormalizado =
        typeof rol === 'string'
          ? rol.trim().toLowerCase()
          : String(rol?.codigo ?? '').trim().toLowerCase();

      if (rolNormalizado !== 'administrador') {
        const alerta = await alertController.create({
          header: 'Acceso restringido',
          message:
            'La administración de los servicios está reservada para el administrador.',
          buttons: ['Entendido'],
          cssClass: 'alerta-ganica'
        });

        await alerta.present();
        return false;
      }

      return true;
    } catch {
      const alerta = await alertController.create({
        header: 'No se pudo verificar tu acceso',
        message:
          'Volvé a iniciar sesión e intentá nuevamente.',
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
  const esAdmin =
    await verificarPermisoAdministrador();

  if (!esAdmin) return;

  servicioAEditar.value = null;
  modalAbierto.value = true;
};

// ==========================================
// EDITAR SERVICIO
// ==========================================

const abrirModalEditar = async (
  servicio: Servicio
) => {
  const esAdmin =
    await verificarPermisoAdministrador();

  if (!esAdmin) return;

  servicioAEditar.value = servicio;
  modalAbierto.value = true;
};

// ==========================================
// DAR DE BAJA SERVICIO
// ==========================================

const darDeBaja = async (
  id: string | number
) => {
  const esAdmin =
    await verificarPermisoAdministrador();

  if (!esAdmin) return;

  const alerta = await alertController.create({
    header: 'Dar de baja servicio',
    message:
      '¿Estás segura de que querés dar de baja este servicio?',
    buttons: [
      {
        text: 'Cancelar',
        role: 'cancel'
      },
      {
        text: 'Dar de baja',
        role: 'destructive',
        handler: () => {
          void servicios_store.eliminar_servicio(id);
        }
      }
    ],
    cssClass: 'alerta-ganica'
  });

  await alerta.present();
};

// ==========================================
// AL CARGAR LA PÁGINA
// ==========================================

onMounted(() => {
  esOscuro.value = obtenerTemaGuardado();

  actualizarRolSesion();

  window.addEventListener(
    'sesion-actualizada',
    actualizarRolSesion
  );
});

onUnmounted(() => {
  window.removeEventListener(
    'sesion-actualizada',
    actualizarRolSesion
  );
});

// ==========================================
// CADA VEZ QUE ENTRAMOS A LA PÁGINA
// ==========================================

onIonViewWillEnter(async () => {
  actualizarRolSesion();

  esOscuro.value = obtenerTemaGuardado();

  if (!esVecino.value) {
    await servicios_store.cargar_servicios();
  } else {
    notificaciones.value = [];
  }

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

        <ion-title>
          Gánica · Inicio
        </ion-title>

        <ion-buttons slot="end">
          <ion-button
            @click="recargarInicio"
            :disabled="recargandoInicio"
            aria-label="Recargar inicio"
          >
            <ion-icon
              :icon="refreshOutline"
              slot="icon-only"
            />
          </ion-button>

          <ion-button
            @click="alternarTema"
            aria-label="Cambiar tema"
          >
            <ion-icon
              :icon="esOscuro ? sunnyOutline : moonOutline"
              slot="icon-only"
            />
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <!-- INICIO DEL VECINO -->
      <section v-if="esVecino" class="bienvenida">
        <p class="sobrelinea">GESTIÓN DE RECOLECCIÓN</p>

        <h1>¡Hola, {{ nombreUsuario }}!</h1>

        <p class="texto-bienvenida">
          Desde acá podés solicitar el retiro de residuos
          y seguir el estado de tus solicitudes.
        </p>

        <ion-card class="tarjeta-principal">
          <ion-card-content>
            <div class="icono-principal">
              <ion-icon :icon="addCircleOutline" />
            </div>

            <h2>¿Necesitás un retiro?</h2>

            <p>
              Registrá una solicitud con los datos y la
              ubicación del retiro.
            </p>

            <ion-button
              expand="block"
              @click="irANuevaSolicitud"
            >
              <ion-icon
                slot="start"
                :icon="addCircleOutline"
              />
              Solicitar retiro
            </ion-button>
          </ion-card-content>
        </ion-card>

        <h2 class="titulo-seccion">
          Accesos rápidos
        </h2>

        <ion-card
          class="tarjeta-acceso"
          button
          @click="irAHistorial"
        >
          <ion-item lines="none">
            <ion-icon
              slot="start"
              :icon="timeOutline"
              color="primary"
            />

            <ion-label>
              <h3>Mi historial</h3>
              <p>
                Consultá el seguimiento de tus solicitudes.
              </p>
            </ion-label>

            <ion-icon
              slot="end"
              :icon="timeOutline"
              color="medium"
            />
          </ion-item>
        </ion-card>

        <ion-card
          class="tarjeta-acceso"
          button
          @click="irAMiCuenta"
        >
          <ion-item lines="none">
            <ion-icon
              slot="start"
              :icon="personOutline"
              color="primary"
            />

            <ion-label>
              <h3>Mi cuenta</h3>
              <p>
                Consultá los datos de tu perfil.
              </p>
            </ion-label>

            <ion-icon
              slot="end"
              :icon="personOutline"
              color="medium"
            />
          </ion-item>
        </ion-card>

        <div class="aviso-conformidad">
          <ion-icon :icon="alertCircleOutline" />

          <p>
            Cuando el recolector marque tu solicitud
            como <strong>En camino</strong>, podrás registrar
            tu firma de conformidad desde el seguimiento
            de la solicitud.
          </p>
        </div>
      </section>

      <!-- INICIO DEL ADMINISTRADOR -->
      <section
        v-else-if="esAdministrador"
        class="bienvenida"
      >
        <p class="sobrelinea">PANEL DE ADMINISTRACIÓN</p>

        <h1>Bienvenido, {{ nombreUsuario }}</h1>

        <p class="texto-bienvenida">
          Administrá los servicios de recolección y
          supervisá las solicitudes recibidas.
        </p>

        <div class="rejilla-accesos">
          <ion-card
            class="tarjeta-acceso"
            button
            @click="irASolicitudes"
          >
            <ion-card-content>
              <ion-icon
                :icon="notificationsOutline"
                class="icono-acceso"
              />

              <h3>Solicitudes</h3>

              <p>
                Revisá y actualizá los estados.
              </p>
            </ion-card-content>
          </ion-card>

          <ion-card
            class="tarjeta-acceso"
            button
            @click="irAPuntos"
          >
            <ion-card-content>
              <ion-icon
                :icon="locationOutline"
                class="icono-acceso"
              />

              <h3>Puntos de retiro</h3>

              <p>
                Consultá los puntos registrados.
              </p>
            </ion-card-content>
          </ion-card>
        </div>
      </section>

      <!-- INICIO DEL RECOLECTOR -->
      <section
        v-else-if="esRecolector"
        class="bienvenida"
      >
        <p class="sobrelinea">PANEL DEL RECOLECTOR</p>

        <h1>¡Hola, {{ nombreUsuario }}!</h1>

        <p class="texto-bienvenida">
          Consultá los avisos recibidos y mantené
          actualizado el estado de tus solicitudes.
        </p>

        <div class="rejilla-accesos">
          <ion-card
            class="tarjeta-acceso"
            button
            @click="irASolicitudes"
          >
            <ion-card-content>
              <ion-icon
                :icon="notificationsOutline"
                class="icono-acceso"
              />

              <h3>Mis solicitudes</h3>

              <p>
                Revisá los retiros asignados.
              </p>
            </ion-card-content>
          </ion-card>

          <ion-card
            class="tarjeta-acceso"
            button
            @click="irAPuntos"
          >
            <ion-card-content>
              <ion-icon
                :icon="locationOutline"
                class="icono-acceso"
              />

              <h3>Puntos de retiro</h3>

              <p>
                Consultá los puntos disponibles.
              </p>
            </ion-card-content>
          </ion-card>
        </div>
      </section>

      <!-- USUARIO SIN ROL RECONOCIDO -->
      <section
        v-else
        class="estado-contenedor"
      >
        <ion-icon
          :icon="alertCircleOutline"
          class="icono-error"
        />

        <h2>Sesión no identificada</h2>

        <p>
          No pudimos identificar tu tipo de usuario.
          Cerrá sesión e ingresá nuevamente.
        </p>
      </section>

      <!-- BANDEJA DE AVISOS -->
      <section v-if="puedeGestionarSolicitudes">
        <ion-card
          class="tarjeta-bandeja"
          button
          @click="abrirBandeja"
        >
          <ion-item lines="none">
            <ion-icon
              :icon="notificationsOutline"
              slot="start"
              color="primary"
            />

            <ion-label>
              <h3>Bandeja de avisos</h3>

              <p>
                <strong>
                  {{ notificacionesNoLeidas }}
                </strong>
                notificaciones sin leer
              </p>
            </ion-label>
          </ion-item>
        </ion-card>

        <!-- VENTANA DE AVISOS -->
        <div
          v-if="mostrarAvisos"
          class="bandeja-avisos"
        >
          <ion-card>
            <ion-card-header>
              <ion-card-title>
                Avisos de recolección
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

                <p>
                  No hay notificaciones para mostrar.
                </p>
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
                      {{
                        new Date(
                          notificacion.creadoEn
                        ).toLocaleString()
                      }}
                    </p>

                    <ion-button
                      v-if="!notificacion.leidaEn"
                      size="small"
                      color="primary"
                      @click.stop="
                        marcarComoLeida(notificacion)
                      "
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
                        notificacion.solicitudId
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
                          {{
                            notificacion.estadoSolicitud ||
                            'Consultar solicitud'
                          }}
                        </strong>
                      </p>

                      <template
                        v-if="
                          notificacion.estadoSolicitud !==
                          'Finalizada'
                        "
                      >
                        <p class="indicacion-estados">
                          Seleccioná el nuevo estado:
                        </p>

                        <div class="botones-estados">
                          <ion-button
                            size="small"
                            fill="outline"
                            color="warning"
                            :disabled="
                              estadosActualizando.includes(
                                Number(
                                  notificacion.solicitudId
                                )
                              )
                            "
                            @click.stop="
                              actualizarEstadoSolicitud(
                                notificacion,
                                'Observada'
                              )
                            "
                          >
                            Observada
                          </ion-button>

                          <ion-button
                            size="small"
                            fill="outline"
                            color="primary"
                            :disabled="
                              estadosActualizando.includes(
                                Number(
                                  notificacion.solicitudId
                                )
                              )
                            "
                            @click.stop="
                              actualizarEstadoSolicitud(
                                notificacion,
                                'En camino'
                              )
                            "
                          >
                            En camino
                          </ion-button>
                        </div>
                      </template>

                      <p
                        v-else
                        class="aviso-leido"
                      >
                        Esta solicitud ya fue finalizada.
                      </p>
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
      </section>

      <!-- SERVICIOS DE RECOLECCIÓN -->
      <section
        v-if="!esVecino && (esAdministrador || esRecolector)"
      >
        <div class="app-portada">
          <h2 class="titulo-seccion">
            Servicios de recolección
          </h2>

          <p v-if="esAdministrador">
            Administrá los servicios disponibles.
          </p>

          <p v-else>
            Consultá los servicios y sus horarios.
          </p>
        </div>

        <!-- CARGANDO -->
        <div v-if="cargando">
          <ion-card
            v-for="n in 3"
            :key="n"
          >
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

        <!-- ERROR -->
        <div
          v-else-if="error"
          class="estado-contenedor"
        >
          <ion-icon
            :icon="alertCircleOutline"
            class="icono-error"
          />

          <h2>Error de conexión</h2>

          <p>{{ error }}</p>

          <ion-button
            color="primary"
            @click="reintentar"
          >
            Reintentar
          </ion-button>
        </div>

        <!-- SIN SERVICIOS -->
        <div
          v-else-if="!hay_servicios"
          class="estado-contenedor"
        >
          <ion-icon
            :icon="trashOutline"
            class="icono-vacio"
          />

          <p>
            No hay servicios disponibles en este momento.
          </p>
        </div>

        <!-- LISTA DE SERVICIOS -->
        <div v-else>
          <ion-card
            v-for="s in lista"
            :key="s.id"
          >
            <ion-card-header>
              <div class="encabezado-servicio">
                <ion-card-title>
                  {{ s.nombre || s.titulo }}
                </ion-card-title>

                <!-- Acciones exclusivas del administrador -->
                <div v-if="esAdministrador">
                  <ion-button
                    fill="clear"
                    size="small"
                    aria-label="Editar servicio"
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
                    aria-label="Dar de baja servicio"
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
              <p>
                {{ s.descripcion || s.detalle }}
              </p>

              <p
                v-if="s.dias"
                class="detalle-servicio"
              >
                <strong>Días:</strong> {{ s.dias }}
              </p>

              <p v-if="s.horario">
                <strong>Horario:</strong> {{ s.horario }}
              </p>
            </ion-card-content>
          </ion-card>
        </div>

        <!-- AGREGAR SERVICIO: SOLO ADMINISTRADOR -->
        <ion-fab
          v-if="esAdministrador"
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
      </section>
    </ion-content>
  </ion-page>
</template>

<style scoped>
.bienvenida {
  margin-bottom: 24px;
}

.sobrelinea {
  margin-bottom: 8px;
  color: var(--ion-color-primary);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 1.5px;
}

.bienvenida h1 {
  margin-top: 4px;
  margin-bottom: 10px;
  font-size: 1.65rem;
  font-weight: 750;
}

.texto-bienvenida {
  color: var(--ion-color-medium);
  line-height: 1.6;
}

.tarjeta-principal {
  margin: 20px 0;
  border-radius: 18px;
}

.tarjeta-principal ion-card-content {
  padding: 22px;
}

.icono-principal {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 54px;
  height: 54px;
  margin-bottom: 12px;
  border-radius: 16px;
  background: var(--ion-color-primary);
  color: var(--ion-color-primary-contrast);
  font-size: 28px;
}

.tarjeta-principal h2 {
  margin-top: 8px;
  font-size: 1.25rem;
  font-weight: 700;
}

.tarjeta-principal p {
  color: var(--ion-color-medium);
  line-height: 1.5;
}

.tarjeta-principal ion-button {
  margin-top: 18px;
}

.titulo-seccion {
  margin: 24px 0 8px;
  font-size: 1.2rem;
  font-weight: 700;
}

.rejilla-accesos {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
  margin-top: 16px;
}

.rejilla-accesos ion-card {
  margin: 0;
}

.tarjeta-acceso {
  border-radius: 14px;
}

.tarjeta-acceso ion-card-content {
  padding: 16px;
}

.tarjeta-acceso h3 {
  font-size: 1rem;
  font-weight: 700;
}

.tarjeta-acceso p {
  color: var(--ion-color-medium);
  font-size: 0.88rem;
  line-height: 1.45;
}

.icono-acceso {
  font-size: 28px;
  color: var(--ion-color-primary);
}

.aviso-conformidad {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  margin-top: 20px;
  padding: 14px;
  border-radius: 12px;
  background: var(--ion-color-light);
}

.aviso-conformidad ion-icon {
  flex-shrink: 0;
  margin-top: 2px;
  color: var(--ion-color-primary);
  font-size: 22px;
}

.aviso-conformidad p {
  margin: 0;
  font-size: 0.9rem;
  line-height: 1.55;
}

.app-portada {
  margin: 24px 0 12px;
}

.app-portada p {
  color: var(--ion-color-medium);
}

.tarjeta-bandeja {
  margin-bottom: 20px;
  border-radius: 14px;
}

.bandeja-avisos {
  margin-bottom: 20px;
}

.notificacion-card {
  margin-top: 12px;
  border-radius: 12px;
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
  line-height: 1.5;
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
  padding: 20px;
  text-align: center;
}

.encabezado-servicio {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.encabezado-servicio ion-card-title {
  flex: 1;
}

.detalle-servicio {
  margin-top: 8px;
}

.estado-contenedor {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 40px 20px;
  text-align: center;
}

.icono-error {
  font-size: 56px;
  color: var(--ion-color-danger);
}

.icono-vacio {
  font-size: 56px;
  color: var(--ion-color-medium);
}

/* Alertas de GANICA */
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

@media (max-width: 360px) {
  .rejilla-accesos {
    grid-template-columns: 1fr;
  }
}
</style>
