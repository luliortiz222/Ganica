
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
          refreshing-text="Cargando puntos..."
        />
      </ion-refresher>

      <h2 class="app-titulo-ganica">Puntos Verdes y Reciclaje</h2>

      <!-- Gestión exclusiva del administrador -->
      <div v-if="esAdministrador" class="ion-padding-bottom">
        <ion-button expand="block" @click="abrirFormularioNuevo">
          <ion-icon :icon="addCircleOutline" slot="start" />
          Agregar punto de retiro
        </ion-button>
      </div>

      <!-- Cargando -->
      <div v-if="cargando" class="ion-text-center ion-padding">
        <ion-spinner name="crescent" />
        <p>Cargando puntos de retiro...</p>
      </div>

      <!-- Error -->
      <ion-card v-else-if="error">
        <ion-card-content class="ion-text-center">
          <ion-text color="danger">{{ error }}</ion-text>
          <ion-button expand="block" fill="outline" @click="cargarPuntos">
            Reintentar
          </ion-button>
        </ion-card-content>
      </ion-card>

      <!-- Sin resultados -->
      <div
        v-else-if="puntosFiltrados.length === 0"
        class="ion-text-center ion-padding"
      >
        <ion-note>
          No se encontraron puntos de retiro que coincidan con la búsqueda.
        </ion-note>
      </div>

      <!-- Listado -->
      <ion-card v-for="punto in puntosFiltrados" :key="punto.id">
        <ion-card-header>
          <ion-card-title>{{ punto.nombre }}</ion-card-title>

          <ion-card-subtitle>
            📍 {{ punto.direccion }} ({{ punto.barrio }})
          </ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <p style="margin-bottom: 8px;">
            <ion-icon
              :icon="timeOutline"
              style="vertical-align: middle; margin-right: 4px;"
            />
            <strong>Horario:</strong> {{ punto.horario }}
          </p>

          <div class="chips-container">
            <ion-chip
              v-for="material in punto.materiales"
              :key="material"
              color="primary"
            >
              <ion-label>{{ material }}</ion-label>
            </ion-chip>
          </div>

          <!-- Acciones del administrador -->
          <div v-if="esAdministrador" class="acciones-admin">
            <ion-button
              size="small"
              fill="outline"
              @click="abrirFormularioEditar(punto)"
            >
              <ion-icon :icon="createOutline" slot="start" />
              Editar
            </ion-button>

            <ion-button
              size="small"
              color="danger"
              fill="outline"
              @click="confirmarDesactivacion(punto)"
            >
              <ion-icon :icon="trashOutline" slot="start" />
              Desactivar
            </ion-button>
          </div>
        </ion-card-content>
      </ion-card>

      <!-- Formulario de alta y modificación -->
      <ion-modal
        :is-open="mostrarFormulario"
        @didDismiss="cerrarFormulario"
      >
        <ion-header>
          <ion-toolbar color="primary">
            <ion-title>
              {{ editando ? 'Modificar punto' : 'Nuevo punto de retiro' }}
            </ion-title>

            <ion-buttons slot="end">
              <ion-button @click="cerrarFormulario">
                Cerrar
              </ion-button>
            </ion-buttons>
          </ion-toolbar>
        </ion-header>

        <ion-content class="ion-padding">
          <ion-item>
            <ion-label position="stacked">Nombre del punto</ion-label>
            <ion-input
              v-model="form.nombre"
              placeholder="Ej. Punto Verde Centro"
            />
          </ion-item>

          <ion-item>
            <ion-label position="stacked">Barrio</ion-label>
            <ion-select
              v-model="form.barrio"
              placeholder="Seleccioná un barrio"
              interface="action-sheet"
            >
              <ion-select-option
                v-for="barrio in barrios"
                :key="barrio"
                :value="barrio"
              >
                {{ barrio }}
              </ion-select-option>
            </ion-select>
          </ion-item>

          <ion-item>
            <ion-label position="stacked">Dirección</ion-label>
            <ion-input
              v-model="form.direccion"
              placeholder="Ej. Av. Mitre 2100"
            />
          </ion-item>

          <ion-item>
            <ion-label position="stacked">Horario de atención</ion-label>
            <ion-input
              v-model="form.horarioAtencion"
              placeholder="Ej. Lunes a viernes de 08:00 a 18:00"
            />
          </ion-item>

          <ion-item>
            <ion-label position="stacked">Materiales aceptados</ion-label>
            <ion-textarea
              v-model="form.materiales"
              placeholder="Separá los materiales con comas. Ej. Plástico, Cartón, Vidrio"
              :auto-grow="true"
            />
          </ion-item>

          <ion-text color="danger" v-if="errorFormulario">
            <p>{{ errorFormulario }}</p>
          </ion-text>

          <ion-button
            expand="block"
            class="ion-margin-top"
            :disabled="guardando"
            @click="guardarPunto"
          >
            <ion-spinner v-if="guardando" name="crescent" />
            <span v-else>
              {{ editando ? 'Guardar cambios' : 'Crear punto' }}
            </span>
          </ion-button>

          <ion-button
            expand="block"
            fill="clear"
            color="medium"
            @click="cerrarFormulario"
          >
            Cancelar
          </ion-button>
        </ion-content>
      </ion-modal>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';

import {
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent,
  IonButtons, IonMenuButton, IonSearchbar, IonCard,
  IonCardHeader, IonCardTitle, IonCardSubtitle, IonCardContent,
  IonChip, IonLabel, IonNote, IonIcon, IonRefresher,
  IonRefresherContent, IonButton, IonSpinner, IonText,
  IonModal, IonItem, IonInput, IonSelect, IonSelectOption,
  IonTextarea, alertController
} from '@ionic/vue';

import {
  timeOutline,
  addCircleOutline,
  createOutline,
  trashOutline
} from 'ionicons/icons';

import {
  puntosRetiroService,
  type PuntoRetiro,
  type PuntoRetiroRequest
} from '@/servicies/puntos_retiro_service';

// --------------------------------------------------
// SESIÓN Y ROL
// --------------------------------------------------

const usuarioGuardado = localStorage.getItem('usuario');

let rolUsuario = '';

try {
  const usuario = JSON.parse(usuarioGuardado || '{}');
  rolUsuario = String(usuario.rol || '').trim().toLowerCase();
} catch {
  rolUsuario = '';
}

const esAdministrador = computed(
  () => rolUsuario === 'administrador'
);

// --------------------------------------------------
// DATOS
// --------------------------------------------------

interface PuntoRetiroVista {
  id: number;
  nombre: string;
  barrio: string;
  direccion: string;
  horario: string;
  materiales: string[];
}

const puntos = ref<PuntoRetiroVista[]>([]);
const busqueda = ref('');
const cargando = ref(false);
const guardando = ref(false);
const error = ref('');
const errorFormulario = ref('');

const mostrarFormulario = ref(false);
const editando = ref(false);
const idEditando = ref<number | null>(null);

// --------------------------------------------------
// BARRIOS
// --------------------------------------------------

// Lista inicial editable. Solo incluye barrios,
// no complejos habitacionales.
const barrios = [
  'Centro',
  'Almirante Brown',
  'Altos del Oeste',
  'Barrio 100 Viviendas',
  'Barrio 108 Viviendas',
  'Barrio 110 Viviendas',
  'Barrio 120 Viviendas',
  'Barrio 640 Viviendas',
  'Barrio ATE I',
  'Barrio ATE II',
  'Barrio Belgrano',
  'Barrio Campaña del Desierto',
  'Barrio Colón',
  'Barrio El Criollo',
  'Barrio Estación',
  'Barrio Eva Perón',
  'Barrio Jardín del Sur',
  'Barrio La Ribera',
  'Barrio Las Mirandas',
  'Barrio Libertad',
  'Barrio Los Poetas',
  'Barrio Norte',
  'Barrio Oeste',
  'Barrio Parque',
  'Barrio Pellegrini',
  'Barrio Santa Rita',
  'Barrio Sur',
  'Barrio Villa Celestina', 
  'Barrio Güemes', 
  'Barrio San Jose'
];

// --------------------------------------------------
// FORMULARIO
// --------------------------------------------------

const formularioVacio = (): PuntoRetiroRequest => ({
  nombre: '',
  barrio: '',
  direccion: '',
  horarioAtencion: '',
  materiales: ''
});

const form = ref<PuntoRetiroRequest>(formularioVacio());

// --------------------------------------------------
// ADAPTACIÓN DE DATOS DE LA API
// --------------------------------------------------

function convertirPunto(punto: PuntoRetiro): PuntoRetiroVista {
  return {
    id: punto.id,
    nombre: punto.nombre,
    barrio: punto.barrio,
    direccion: punto.direccion,
    horario: punto.horarioAtencion,
    materiales: String(punto.materiales || '')
      .split(',')
      .map(material => material.trim())
      .filter(material => material.length > 0)
  };
}

// --------------------------------------------------
// CONSULTAR PUNTOS
// --------------------------------------------------

async function cargarPuntos() {
  cargando.value = true;
  error.value = '';

  try {
    const respuesta = await puntosRetiroService.listar();

    puntos.value = respuesta.map(convertirPunto);
  } catch (e: any) {
    error.value = e?.message || 'No se pudieron cargar los puntos de retiro.';
  } finally {
    cargando.value = false;
  }
}

// --------------------------------------------------
// ACTUALIZAR LISTADO
// --------------------------------------------------

async function hacerRecarga(event: CustomEvent) {
  busqueda.value = '';

  await cargarPuntos();

  (event.target as HTMLIonRefresherElement)?.complete();
}

// --------------------------------------------------
// BÚSQUEDA
// --------------------------------------------------

const puntosFiltrados = computed(() => {
  const q = busqueda.value.trim().toLowerCase();

  if (!q) return puntos.value;

  return puntos.value.filter(p =>
    p.nombre.toLowerCase().includes(q) ||
    p.barrio.toLowerCase().includes(q) ||
    p.direccion.toLowerCase().includes(q) ||
    p.materiales.some(m => m.toLowerCase().includes(q))
  );
});

// --------------------------------------------------
// ABRIR FORMULARIO NUEVO
// --------------------------------------------------

function abrirFormularioNuevo() {
  if (!esAdministrador.value) return;

  form.value = formularioVacio();
  editando.value = false;
  idEditando.value = null;
  errorFormulario.value = '';
  mostrarFormulario.value = true;
}

// --------------------------------------------------
// ABRIR FORMULARIO DE EDICIÓN
// --------------------------------------------------

function abrirFormularioEditar(punto: PuntoRetiroVista) {
  if (!esAdministrador.value) return;

  form.value = {
    nombre: punto.nombre,
    barrio: punto.barrio,
    direccion: punto.direccion,
    horarioAtencion: punto.horario,
    materiales: punto.materiales.join(', ')
  };

  editando.value = true;
  idEditando.value = punto.id;
  errorFormulario.value = '';
  mostrarFormulario.value = true;
}

// --------------------------------------------------
// CERRAR FORMULARIO
// --------------------------------------------------

function cerrarFormulario() {
  mostrarFormulario.value = false;
  errorFormulario.value = '';
}

// --------------------------------------------------
// GUARDAR
// --------------------------------------------------

async function guardarPunto() {
  if (!esAdministrador.value) return;

  errorFormulario.value = '';

  const datos: PuntoRetiroRequest = {
    nombre: form.value.nombre.trim(),
    barrio: form.value.barrio.trim(),
    direccion: form.value.direccion.trim(),
    horarioAtencion: form.value.horarioAtencion.trim(),
    materiales: form.value.materiales.trim()
  };

  if (
    !datos.nombre ||
    !datos.barrio ||
    !datos.direccion ||
    !datos.horarioAtencion ||
    !datos.materiales
  ) {
    errorFormulario.value = 'Completá todos los campos obligatorios.';
    return;
  }

  guardando.value = true;

  try {
    if (editando.value && idEditando.value !== null) {
      await puntosRetiroService.modificar(idEditando.value, datos);
    } else {
      await puntosRetiroService.crear(datos);
    }

    cerrarFormulario();
    await cargarPuntos();

    const alerta = await alertController.create({
      header: 'Operación exitosa',
      message: editando.value
        ? 'El punto de retiro fue actualizado correctamente.'
        : 'El punto de retiro fue creado correctamente.',
      buttons: ['Aceptar']
    });

    await alerta.present();

  } catch (e: any) {
    errorFormulario.value = e?.message || 'No se pudo guardar el punto de retiro.';
  } finally {
    guardando.value = false;
  }
}

// --------------------------------------------------
// DESACTIVAR
// --------------------------------------------------

async function confirmarDesactivacion(punto: PuntoRetiroVista) {
  if (!esAdministrador.value) return;

  const alerta = await alertController.create({
    header: 'Desactivar punto de retiro',
    message: `¿Querés desactivar el punto "${punto.nombre}"? Ya no estará disponible para los usuarios.`,
    buttons: [
      {
        text: 'Cancelar',
        role: 'cancel'
      },
      {
        text: 'Desactivar',
        role: 'destructive',
        handler: async () => {
          try {
            await puntosRetiroService.desactivar(punto.id);
            await cargarPuntos();
          } catch (e: any) {
            const errorAlerta = await alertController.create({
              header: 'No se pudo desactivar',
              message: e?.message || 'Ocurrió un error al desactivar el punto.',
              buttons: ['Aceptar']
            });

            await errorAlerta.present();
          }
        }
      }
    ]
  });

  await alerta.present();
}

// --------------------------------------------------
// INICIO
// --------------------------------------------------

onMounted(() => {
  cargarPuntos();
});
</script>

<style scoped>
.chips-container {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-top: 8px;
}

.acciones-admin {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 16px;
}
</style>
