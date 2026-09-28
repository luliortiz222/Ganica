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
} from '../servicies/notificaciones_service';

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

const obtenerUsuarioActual = () => {
  const usuarioGuardado = localStorage.getItem('usuario');

  if (!usuarioGuardado) return null;

  try {
    return JSON.parse(usuarioGuardado);
  } catch {
    return null;
  }
};

// ==========================================
// CARGAR NOTIFICACIONES
// ==========================================

const cargarNotificaciones = async () => {
  const usuarioActual = obtenerUsuarioActual();

  console.log('Usuario actual:', usuarioActual);

  if (!usuarioActual?.id) {
    console.log('No se encontró ID de usuario.');
    return;
  }

  const res = await obtener_bandeja_notificaciones(usuarioActual.id);

  console.log('Resultado bandeja:', res);

  if (res.ok) {
    notificaciones.value = res.notificaciones;
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
  if (!notificacion.leidaEn) {
    const res = await marcar_notificacion_leida(notificacion.id);

    if (res.ok) {
      notificacion.leidaEn = new Date().toISOString();
    }
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

  if (!usuarioGuardado) return false;

  try {
    const parsed = JSON.parse(usuarioGuardado);

    if (parsed.rol !== 'administrador') {
      const alerta = await alertController.create({
        header: 'Acceso Denegado (403)',
        message:
          'Las modificaciones en el sistema solo las puede realizar el administrador.',
        buttons: ['Aceptar']
      });

      await alerta.present();

      return false;
    }

    return true;
  } catch (e) {
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

    <!-- ==========================================
         HEADER
    =========================================== -->

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

      <!-- ==========================================
           BANDEJA DE AVISOS
      =========================================== -->

      <ion-card
        style="margin-bottom: 20px; cursor: pointer;"
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


      <!-- ==========================================
           VENTANA DE AVISOS
      =========================================== -->

      <div v-if="mostrarAvisos" class="bandeja-avisos">

        <ion-card>

          <ion-card-header>

            <ion-card-title>
              Avisos de Recolección
            </ion-card-title>

          </ion-card-header>


          <ion-card-content>

            <!-- SIN NOTIFICACIONES -->

            <div
              v-if="notificaciones.length === 0"
              class="estado-avisos"
            >

              <ion-icon
                :icon="notificationsOutline"
                class="icono-vacio"
              />

              <p>
                No tenés avisos pendientes.
              </p>

            </div>


            <!-- LISTA DE NOTIFICACIONES -->

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

                  <p>
                    {{ notificacion.mensaje }}
                  </p>


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
                    @click="marcarComoLeida(notificacion)"
                  >

                    Marcar como leído

                  </ion-button>


                  <p
                    v-else
                    class="aviso-leido"
                  >

                    ✓ Aviso leído

                  </p>

                </ion-card-content>

              </ion-card>

            </div>


            <!-- CERRAR -->

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


      <!-- ==========================================
           TÍTULO SERVICIOS
      =========================================== -->

      <div class="app-portada">

        <ion-title
          size="large"
          class="ion-no-padding"
        >

          Servicios de Recolección

        </ion-title>

      </div>


      <!-- ==========================================
           1. ESTADO CARGANDO
      =========================================== -->

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


      <!-- ==========================================
           2. ESTADO ERROR
      =========================================== -->

      <div
        v-else-if="error"
        class="estado-contenedor"
      >

        <ion-icon
          :icon="alertCircleOutline"
          class="icono-error"
        />

        <h2>
          Error de conexión
        </h2>

        <p>
          {{ error }}
        </p>

        <ion-button
          color="primary"
          fill="solid"
          @click="reintentar"
        >

          REINTENTAR

        </ion-button>

      </div>


      <!-- ==========================================
           3. ESTADO VACÍO
      =========================================== -->

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


      <!-- ==========================================
           4. LISTA REAL CON ACCIONES ABM
      =========================================== -->

      <div v-else>

        <ion-card
          v-for="s in lista"
          :key="s.id"
        >

          <ion-card-header>

            <div
              style="
                display: flex;
                justify-content: space-between;
                align-items: center;
              "
            >

              <ion-card-title>

                {{ s.nombre || s.titulo }}

              </ion-card-title>


              <div>

                <!-- EDITAR -->
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


                <!-- ELIMINAR -->
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

            <p>
              {{ s.descripcion || s.detalle }}
            </p>


            <p
              v-if="s.dias"
              style="margin-top: 8px;"
            >

              <strong>Días:</strong>
              {{ s.dias }}

            </p>


            <p v-if="s.horario">

              <strong>Horario:</strong>
              {{ s.horario }}

            </p>

          </ion-card-content>

        </ion-card>

      </div>


      <!-- ==========================================
           BOTÓN AGREGAR SERVICIO
      =========================================== -->

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


      <!-- ==========================================
           MODAL DE EDICIÓN / ALTA
      =========================================== -->

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


/* ==========================================
   BANDEJA DE AVISOS
========================================== */

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

.estado-avisos {
  text-align: center;
  padding: 20px;
}


/* ==========================================
   ESTADOS DE SERVICIOS
========================================== */

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