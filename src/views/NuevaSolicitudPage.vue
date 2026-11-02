<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>
        <ion-title>Nueva solicitud</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <h2 class="app-seccion-titulo">Solicitar recolección</h2>

      <p>
        Completá los datos del residuo que necesitás retirar.
        El equipo de recolección recibirá tu solicitud.
      </p>

      <ion-card class="app-card">
        <ion-card-content>
          <form class="app-formulario" @submit.prevent="enviarSolicitud">

            <!-- SERVICIOS DISPONIBLES -->
            <ion-item lines="full">
              <ion-select
                v-model="form.servicioRecoleccionId"
                label="Servicio de recolección"
                label-placement="stacked"
                placeholder="Seleccioná un servicio"
                :disabled="
                  serviciosCargando ||
                  serviciosDisponibles.length === 0 ||
                  enviando
                "
                required
              >
                <ion-select-option
                  v-for="servicio in serviciosDisponibles"
                  :key="servicio.id"
                  :value="Number(servicio.id)"
                >
                  {{ servicio.nombre || servicio.titulo || 'Servicio sin nombre' }}
                </ion-select-option>
              </ion-select>
            </ion-item>

            <ion-note v-if="serviciosCargando" color="medium">
              Cargando servicios disponibles...
            </ion-note>

            <ion-note
              v-else-if="serviciosDisponibles.length === 0"
              color="warning"
            >
              No hay servicios habilitados para solicitudes en este momento.
            </ion-note>

            <!-- INFORMACIÓN DEL SERVICIO -->
            <ion-item
              v-if="servicioSeleccionado"
              lines="none"
            >
              <ion-label class="ion-text-wrap">
                <p v-if="servicioSeleccionado.descripcion">
                  {{ servicioSeleccionado.descripcion }}
                </p>

                <p v-if="servicioSeleccionado.dias">
                  Días: {{ servicioSeleccionado.dias }}
                </p>

                <p v-if="servicioSeleccionado.horario">
                  Horario: {{ servicioSeleccionado.horario }}
                </p>
              </ion-label>
            </ion-item>

            <!-- DIRECCIÓN -->
            <ion-item lines="full">
              <ion-input
                v-model="form.direccion"
                label="Dirección de retiro"
                label-placement="stacked"
                placeholder="Ej.: Av. Mitre 1234"
                required
                @ionInput="limpiarCoordenadas"
              />
            </ion-item>

            <!-- UBICACIÓN EN EL MAPA -->
            <ion-button
              expand="block"
              fill="outline"
              class="ion-margin-top"
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
              <p class="coordenadas">
                <ion-icon :icon="checkmarkCircleOutline" />
                Ubicación seleccionada
              </p>

              <p class="coordenadas">
                Latitud: {{ form.latitud.toFixed(6) }}
              </p>

              <p class="coordenadas">
                Longitud: {{ form.longitud.toFixed(6) }}
              </p>
            </ion-note>

            <!-- OBSERVACIONES -->
            <ion-item lines="full">
              <ion-textarea
                v-model="form.observaciones"
                label="Observaciones y detalles"
                label-placement="stacked"
                placeholder="Ej.: ramas en bolsas frente al garage"
                :rows="3"
              />
            </ion-item>

            <!-- DICTADO POR VOZ -->
            <ion-button
              expand="block"
              fill="outline"
              type="button"
              class="ion-margin-top"
              :disabled="escuchandoVoz || enviando"
              @click="dictarObservaciones"
            >
              <ion-icon :icon="micOutline" slot="start" />
              {{ escuchandoVoz ? 'Escuchando...' : 'Dictar observaciones' }}
            </ion-button>

            <!-- ENVIAR -->
            <ion-button
              expand="block"
              type="submit"
              class="ion-margin-top"
              :disabled="
                enviando ||
                serviciosCargando ||
                serviciosDisponibles.length === 0
              "
            >
              <ion-icon :icon="sendOutline" slot="start" />
              {{ enviando ? 'Enviando...' : 'Enviar solicitud' }}
            </ion-button>

          </form>
        </ion-card-content>
      </ion-card>

      <!-- MIS SOLICITUDES -->
      <h2 class="app-seccion-titulo">Mis solicitudes</h2>

      <ion-note v-if="cargando" color="medium">
        Cargando tus solicitudes...
      </ion-note>

      <ion-note
        v-else-if="misSolicitudes.length === 0"
        color="medium"
      >
        Todavía no tenés solicitudes registradas.
      </ion-note>

      <ion-list v-else lines="full">
        <ion-item
          v-for="item in misSolicitudes"
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

            <p>
              Estado:
              <strong>{{ item.estado }}</strong>
            </p>

            <p
              v-if="tieneFirmaRegistrada(item)"
              class="estado-firma registrada"
            >
              <ion-icon :icon="checkmarkCircleOutline" />
              Conformidad firmada y registrada
            </p>

            <p
              v-else-if="estaEnCamino(item)"
              class="estado-firma pendiente"
            >
              <ion-icon :icon="timeOutline" />
              Conformidad pendiente de firma
            </p>

            <!-- FIRMAR CONFORMIDAD -->
            <ion-button
              v-if="puedeFirmar(item)"
              expand="block"
              class="ion-margin-top"
              @click="abrirFirma(item)"
            >
              <ion-icon :icon="createOutline" slot="start" />
              Firmar conformidad
            </ion-button>

            <!-- VER QR PÚBLICO -->
            <ion-button
              v-if="item.id > 0"
              expand="block"
              fill="outline"
              class="ion-margin-top"
              :disabled="
                qrCargandoId !== null ||
                pdfProcesandoId !== null
              "
              @click="abrirQr(item)"
            >
              <ion-icon :icon="qrCodeOutline" slot="start" />
              {{
                qrCargandoId === item.id
                  ? 'Generando QR...'
                  : 'Ver QR público'
              }}
            </ion-button>

            <!-- DESCARGAR COMPROBANTE PDF -->
            <ion-button
              v-if="item.id > 0"
              expand="block"
              fill="outline"
              color="success"
              class="ion-margin-top"
              :disabled="
                pdfProcesandoId !== null ||
                qrCargandoId !== null
              "
              @click="descargarComprobante(item)"
            >
              <ion-icon :icon="downloadOutline" slot="start" />
              {{
                pdfProcesandoId === item.id &&
                pdfOperacion === 'descargar'
                  ? 'Descargando comprobante...'
                  : 'Descargar comprobante PDF'
              }}
            </ion-button>

            <!-- COMPARTIR COMPROBANTE PDF -->
            <ion-button
              v-if="item.id > 0"
              expand="block"
              fill="outline"
              class="ion-margin-top"
              :disabled="
                pdfProcesandoId !== null ||
                qrCargandoId !== null
              "
              @click="compartirComprobante(item)"
            >
              <ion-icon :icon="shareSocialOutline" slot="start" />
              {{
                pdfProcesandoId === item.id &&
                pdfOperacion === 'compartir'
                  ? 'Preparando comprobante...'
                  : 'Compartir comprobante'
              }}
            </ion-button>

          </ion-label>
        </ion-item>
      </ion-list>
    </ion-content>

    <!-- MODAL DE UBICACIÓN -->
    <SelectorUbicacion
      :abierto="modalUbicacionAbierto"
      :direccion-inicial="form.direccion"
      :latitud-inicial="form.latitud"
      :longitud-inicial="form.longitud"
      @confirmar="aplicarUbicacion"
      @cerrar="modalUbicacionAbierto = false"
    />

    <!-- MODAL DE FIRMA -->
    <FirmaConformidad
      v-if="solicitudParaFirma"
      :abierto="modalFirmaAbierto"
      :solicitud-id="solicitudParaFirma.id"
      :direccion="solicitudParaFirma.direccion"
      :tipo-residuo="solicitudParaFirma.tipoResiduo"
      @cerrar="cerrarFirma"
      @finalizada="finalizarFirma"
    />

    <!-- MODAL DEL QR PÚBLICO -->
    <ion-modal
      :is-open="modalQrAbierto"
      @didDismiss="cerrarQr"
    >
      <ion-header>
        <ion-toolbar color="primary">
          <ion-title>QR de solicitud #{{ solicitudQrId }}</ion-title>

          <ion-buttons slot="end">
            <ion-button @click="modalQrAbierto = false">
              Cerrar
            </ion-button>
          </ion-buttons>
        </ion-toolbar>
      </ion-header>

      <ion-content class="ion-padding">
        <div v-if="qrUrl" class="qr-contenedor">
          <p>
            Escaneá este código para consultar públicamente
            el estado de tu solicitud.
          </p>

          <img
            :src="qrUrl"
            alt="Código QR de la solicitud"
            class="imagen-qr"
          />

          <ion-note color="medium">
            No es necesario iniciar sesión para consultar la solicitud.
          </ion-note>
        </div>
      </ion-content>
    </ion-modal>

  </ion-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';

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
  IonNote,
  IonModal,
  toastController
} from '@ionic/vue';

import {
  sendOutline,
  checkmarkCircleOutline,
  locationOutline,
  micOutline,
  createOutline,
  timeOutline,
  qrCodeOutline,
  downloadOutline,
  shareSocialOutline
} from 'ionicons/icons';

import { Capacitor } from '@capacitor/core';
import { Directory, Filesystem } from '@capacitor/filesystem';
import { Share } from '@capacitor/share';

import SelectorUbicacion from '@/components/SelectorUbicacion.vue';
import FirmaConformidad from '@/components/FirmaConformidad.vue';

import {
  crear_solicitud_api,
  listar_solicitudes_api,
  obtener_qr_solicitud_api,
  obtener_comprobante_pdf_solicitud_api
} from '@/servicies/solicitudes_service';

import type {
  SolicitudApi
} from '@/servicies/solicitudes_service';

import {
  obtener_servicios_solicitables_api
} from '@/servicies/servicios_service';

import type {
  Servicio
} from '@/stores/servicios_store';

import {
  dictar_texto
} from '@/servicies/voz_service';


interface Ubicacion {
  direccion: string;
  latitud: number;
  longitud: number;
}

interface SolicitudLocal {
  id: number;
  servicioRecoleccionId: number;
  tipoResiduo: string;
  direccion: string;
  observaciones: string;
  fechaCreacion: string;
  estado: string;
  latitud: number | null;
  longitud: number | null;
  firmaConformidadArchivo?: string | null;
  firmaConformidadFecha?: string | null;
}

interface SolicitudConFirma extends SolicitudApi {
  firmaConformidadArchivo?: string | null;
  firmaConformidadFecha?: string | null;
}


const CLAVE_LOCAL = 'ganica_solicitudes_vecino';


// FORMULARIO
const form = ref({
  servicioRecoleccionId: 0,
  direccion: '',
  observaciones: '',
  latitud: null as number | null,
  longitud: null as number | null
});


// SERVICIOS
const serviciosDisponibles = ref<Servicio[]>([]);
const serviciosCargando = ref(false);

const servicioSeleccionado = computed(() => {
  return serviciosDisponibles.value.find(
    servicio =>
      Number(servicio.id) ===
      Number(form.value.servicioRecoleccionId)
  ) ?? null;
});


// SOLICITUDES
const misSolicitudes = ref<SolicitudApi[]>([]);
const cargando = ref(false);
const enviando = ref(false);
const escuchandoVoz = ref(false);


// MODALES
const modalUbicacionAbierto = ref(false);
const solicitudParaFirma = ref<SolicitudApi | null>(null);
const modalFirmaAbierto = ref(false);


// QR PÚBLICO
const modalQrAbierto = ref(false);
const qrCargandoId = ref<number | null>(null);
const qrUrl = ref<string | null>(null);
const solicitudQrId = ref<number | null>(null);


// COMPROBANTE PDF
const pdfProcesandoId = ref<number | null>(null);
const pdfOperacion = ref<'descargar' | 'compartir' | null>(null);


// TOAST
const mostrarToast = async (
  mensaje: string,
  color: 'success' | 'warning' | 'danger'
) => {
  const toast = await toastController.create({
    message: mensaje,
    duration: 2800,
    color,
    position: 'bottom'
  });

  await toast.present();
};


// CARGAR SERVICIOS
const cargarServicios = async () => {
  serviciosCargando.value = true;

  try {
    const servicios = await obtener_servicios_solicitables_api();

    serviciosDisponibles.value = Array.isArray(servicios)
      ? servicios.filter(servicio =>
          servicio.requiere_solicitud === true
        )
      : [];

    // Verificar que la selección siga disponible.
    const existeSeleccion = serviciosDisponibles.value.some(
      servicio =>
        Number(servicio.id) ===
        Number(form.value.servicioRecoleccionId)
    );

    if (!existeSeleccion) {
      form.value.servicioRecoleccionId = 0;
    }

  } catch (error) {
    console.error('Error al cargar servicios:', error);

    serviciosDisponibles.value = [];

    await mostrarToast(
      'No se pudieron cargar los servicios de recolección.',
      'warning'
    );

  } finally {
    serviciosCargando.value = false;
  }
};


// LEER SOLICITUDES LOCALES
const leerSolicitudesLocales = (): SolicitudLocal[] => {
  try {
    const datos = localStorage.getItem(CLAVE_LOCAL);

    if (!datos) return [];

    const solicitudes = JSON.parse(datos);

    return Array.isArray(solicitudes)
      ? solicitudes
      : [];

  } catch (error) {
    console.error(
      'Error al leer solicitudes locales:',
      error
    );

    return [];
  }
};


// GUARDAR SOLICITUDES LOCALES
const guardarSolicitudesLocales = (
  solicitudes: SolicitudLocal[]
) => {
  localStorage.setItem(
    CLAVE_LOCAL,
    JSON.stringify(solicitudes)
  );
};


// GUARDAR SOLICITUD PENDIENTE
const guardarPendienteLocal = (
  solicitud: SolicitudLocal
) => {
  const guardadas = leerSolicitudesLocales();

  guardadas.unshift(solicitud);

  guardarSolicitudesLocales(guardadas);
};


// APLICAR UBICACIÓN DEL MAPA
const aplicarUbicacion = (ubicacion: Ubicacion) => {
  form.value.direccion = ubicacion.direccion;
  form.value.latitud = ubicacion.latitud;
  form.value.longitud = ubicacion.longitud;
};


// LIMPIAR COORDENADAS
const limpiarCoordenadas = () => {
  form.value.latitud = null;
  form.value.longitud = null;
};


// DICTADO POR VOZ
const dictarObservaciones = async () => {
  if (escuchandoVoz.value || enviando.value) return;

  escuchandoVoz.value = true;

  try {
    const texto = await dictar_texto();

    if (texto.trim()) {
      const actual = form.value.observaciones.trim();

      form.value.observaciones = actual
        ? `${actual} ${texto.trim()}`
        : texto.trim();

      await mostrarToast(
        'Observaciones agregadas.',
        'success'
      );
    }

  } catch (error) {
    console.error(
      'Error al dictar observaciones:',
      error
    );

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


// CONVERTIR SOLICITUD LOCAL A FORMATO API
const convertirSolicitudLocal = (
  solicitud: SolicitudLocal
): SolicitudApi => {
  return {
    id: solicitud.id,
    servicioRecoleccionId: solicitud.servicioRecoleccionId,
    tipoResiduo: solicitud.tipoResiduo,
    direccion: solicitud.direccion,
    observaciones: solicitud.observaciones,
    fechaCreacion: solicitud.fechaCreacion,
    estado: solicitud.estado,
    latitud: solicitud.latitud,
    longitud: solicitud.longitud,
    firmaConformidadArchivo:
      solicitud.firmaConformidadArchivo ?? null,
    firmaConformidadFecha:
      solicitud.firmaConformidadFecha ?? null
  };
};


// CARGAR MIS SOLICITUDES
const cargarMisSolicitudes = async () => {
  cargando.value = true;

  const locales = leerSolicitudesLocales();

  try {
    const remotas = await listar_solicitudes_api();

    const idsRemotos = new Set(
      remotas.map(solicitud => solicitud.id)
    );

    const localesNoDuplicadas = locales.filter(
      solicitud => !idsRemotos.has(solicitud.id)
    );

    misSolicitudes.value = [
      ...localesNoDuplicadas.map(convertirSolicitudLocal),
      ...remotas
    ];

  } catch (error) {
    console.error(
      'Error al cargar solicitudes:',
      error
    );

    misSolicitudes.value = locales.map(
      convertirSolicitudLocal
    );

  } finally {
    cargando.value = false;
  }
};


// LIMPIAR FORMULARIO
const limpiarFormulario = () => {
  form.value = {
    servicioRecoleccionId: 0,
    direccion: '',
    observaciones: '',
    latitud: null,
    longitud: null
  };
};


// ENVIAR SOLICITUD
const enviarSolicitud = async () => {
  if (enviando.value) return;

  const servicio = servicioSeleccionado.value;

  // Validar servicio.
  if (!servicio) {
    await mostrarToast(
      'Seleccioná un servicio de recolección.',
      'warning'
    );

    return;
  }

  const servicioId = Number(servicio.id);

  if (!Number.isInteger(servicioId) || servicioId <= 0) {
    await mostrarToast(
      'El servicio seleccionado no tiene un identificador válido.',
      'warning'
    );

    return;
  }

  // Validar dirección.
  if (!form.value.direccion.trim()) {
    await mostrarToast(
      'Debés indicar la dirección de recolección.',
      'warning'
    );

    return;
  }

  // Obtener nombre del servicio.
  const nombreServicio = String(
    servicio.nombre ?? servicio.titulo ?? ''
  ).trim();

  if (!nombreServicio) {
    await mostrarToast(
      'El servicio seleccionado no tiene un nombre válido.',
      'warning'
    );

    return;
  }

  enviando.value = true;

  // Estos datos se usan tanto para la API
  // como para guardar la solicitud sin conexión.
  const datos = {
    servicioRecoleccionId: servicioId,
    tipoResiduo: nombreServicio,
    direccion: form.value.direccion.trim(),
    observaciones: form.value.observaciones.trim(),
    latitud: form.value.latitud,
    longitud: form.value.longitud
  };

  try {
    await crear_solicitud_api({
      servicioRecoleccionId: datos.servicioRecoleccionId,
      tipoResiduo: datos.tipoResiduo,
      direccion: datos.direccion,
      observaciones: datos.observaciones,
      latitud: datos.latitud,
      longitud: datos.longitud
    });

    limpiarFormulario();

    await cargarMisSolicitudes();

    await mostrarToast(
      'Solicitud registrada correctamente.',
      'success'
    );

  } catch (error) {
    console.error(
      'Error al crear solicitud:',
      error
    );

    const mensaje = error instanceof Error
      ? error.message
      : '';

    const fueEncolada =
      mensaje.includes('Sin conexión') ||
      mensaje.includes('Tiempo de espera');

    if (fueEncolada) {

      // Guardar también el ID del servicio elegido.
      guardarPendienteLocal({
        id: -Date.now(),
        servicioRecoleccionId: datos.servicioRecoleccionId,
        tipoResiduo: datos.tipoResiduo,
        direccion: datos.direccion,
        observaciones: datos.observaciones,
        fechaCreacion: new Date().toISOString(),
        estado: 'Sin enviar',
        latitud: datos.latitud,
        longitud: datos.longitud
      });

      limpiarFormulario();

      await cargarMisSolicitudes();

      await mostrarToast(
        'Sin conexión. La solicitud quedó guardada y se enviará cuando vuelva internet.',
        'warning'
      );

    } else {
      await mostrarToast(
        mensaje || 'No se pudo registrar la solicitud.',
        'danger'
      );
    }

  } finally {
    enviando.value = false;
  }
};


// NORMALIZAR ESTADO
const normalizarEstado = (
  estado: string | null | undefined
) => {
  return (estado || '')
    .trim()
    .toLocaleLowerCase('es-AR');
};


// VERIFICAR SI ESTÁ EN CAMINO
const estaEnCamino = (item: SolicitudApi) => {
  return normalizarEstado(item.estado) === 'en camino';
};


// VERIFICAR FIRMA REGISTRADA
const tieneFirmaRegistrada = (item: SolicitudApi) => {
  const solicitud = item as SolicitudConFirma;

  return Boolean(
    solicitud.firmaConformidadArchivo ||
    solicitud.firmaConformidadFecha
  );
};


// VERIFICAR SI PUEDE FIRMAR
const puedeFirmar = (item: SolicitudApi) => {
  return (
    estaEnCamino(item) &&
    !tieneFirmaRegistrada(item) &&
    item.servicioRecoleccionId !== 0 &&
    item.id > 0
  );
};


// ABRIR FIRMA
const abrirFirma = (item: SolicitudApi) => {
  if (!puedeFirmar(item)) {
    void mostrarToast(
      'Solo podés firmar una solicitud en camino que todavía no tenga conformidad registrada.',
      'warning'
    );

    return;
  }

  solicitudParaFirma.value = item;
  modalFirmaAbierto.value = true;
};


// CERRAR FIRMA
const cerrarFirma = () => {
  modalFirmaAbierto.value = false;
  solicitudParaFirma.value = null;
};


// FINALIZAR FIRMA
const finalizarFirma = async () => {
  modalFirmaAbierto.value = false;
  solicitudParaFirma.value = null;

  await cargarMisSolicitudes();

  await mostrarToast(
    'Conformidad firmada y registrada correctamente.',
    'success'
  );
};


// CONVERTIR PDF A BASE64
const convertirBlobABase64 = (archivo: Blob): Promise<string> => {
  return new Promise((resolve, reject) => {
    const lector = new FileReader();

    lector.onloadend = () => {
      if (typeof lector.result !== 'string') {
        reject(new Error('No se pudo procesar el comprobante PDF.'));
        return;
      }

      const base64 = lector.result.split(',')[1];

      if (!base64) {
        reject(new Error('El comprobante PDF está vacío.'));
        return;
      }

      resolve(base64);
    };

    lector.onerror = () => {
      reject(new Error('No se pudo leer el archivo PDF.'));
    };

    lector.readAsDataURL(archivo);
  });
};


// DESCARGAR COMPROBANTE PDF
const descargarComprobante = async (item: SolicitudApi) => {
  if (item.id <= 0) {
    await mostrarToast(
      'Primero necesitás sincronizar la solicitud.',
      'warning'
    );

    return;
  }

  if (pdfProcesandoId.value !== null) return;

  pdfProcesandoId.value = item.id;
  pdfOperacion.value = 'descargar';

  try {
    const archivo = await obtener_comprobante_pdf_solicitud_api(item.id);

    const nombreArchivo = `comprobante-solicitud-${item.id}.pdf`;

    if (Capacitor.isNativePlatform()) {
      const base64 = await convertirBlobABase64(archivo);

      await Filesystem.writeFile({
        path: `Ganica/comprobantes/${nombreArchivo}`,
        data: base64,
        directory: Directory.Documents,
        recursive: true
      });

      await mostrarToast(
        'Comprobante guardado en Documentos de Gánica.',
        'success'
      );

    } else {
      const url = URL.createObjectURL(archivo);

      const enlace = document.createElement('a');
      enlace.href = url;
      enlace.download = nombreArchivo;

      document.body.appendChild(enlace);
      enlace.click();
      enlace.remove();

      URL.revokeObjectURL(url);

      await mostrarToast(
        'Comprobante descargado correctamente.',
        'success'
      );
    }

  } catch (error) {
    console.error('Error al descargar comprobante:', error);

    await mostrarToast(
      error instanceof Error
        ? error.message
        : 'No se pudo descargar el comprobante.',
      'danger'
    );

  } finally {
    pdfProcesandoId.value = null;
    pdfOperacion.value = null;
  }
};


// COMPARTIR COMPROBANTE PDF
const compartirComprobante = async (item: SolicitudApi) => {
  if (item.id <= 0) {
    await mostrarToast(
      'Primero necesitás sincronizar la solicitud.',
      'warning'
    );

    return;
  }

  if (pdfProcesandoId.value !== null) return;

  pdfProcesandoId.value = item.id;
  pdfOperacion.value = 'compartir';

  try {
    const archivo = await obtener_comprobante_pdf_solicitud_api(item.id);

    const nombreArchivo = `comprobante-solicitud-${item.id}.pdf`;

    if (Capacitor.isNativePlatform()) {
      const base64 = await convertirBlobABase64(archivo);

      // Guardar temporalmente el PDF en la caché.
      const archivoTemporal = await Filesystem.writeFile({
        path: `comprobante-solicitud-${item.id}-${Date.now()}.pdf`,
        data: base64,
        directory: Directory.Cache
      });

      // Abrir el menú nativo para compartir el PDF.
      await Share.share({
        title: `Comprobante de solicitud #${item.id}`,
        text: 'Te comparto el comprobante de mi solicitud de Gánica.',
        files: [archivoTemporal.uri],
        dialogTitle: 'Compartir comprobante'
      });

    } else {
      const archivoCompartir = new File(
        [archivo],
        nombreArchivo,
        { type: 'application/pdf' }
      );

      if (
        navigator.share &&
        navigator.canShare &&
        navigator.canShare({ files: [archivoCompartir] })
      ) {
        await navigator.share({
          title: `Comprobante de solicitud #${item.id}`,
          text: 'Te comparto el comprobante de mi solicitud de Gánica.',
          files: [archivoCompartir]
        });

      } else {
        // Si el navegador no permite compartir archivos,
        // ofrecemos la descarga como alternativa.
        const url = URL.createObjectURL(archivo);

        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = nombreArchivo;

        document.body.appendChild(enlace);
        enlace.click();
        enlace.remove();

        URL.revokeObjectURL(url);

        await mostrarToast(
          'Tu navegador no permite compartir archivos. Se descargó el comprobante.',
          'warning'
        );

        return;
      }
    }

    await mostrarToast(
      'Se abrió el menú para compartir el comprobante.',
      'success'
    );

  } catch (error) {
    console.error('Error al compartir comprobante:', error);

    // El usuario puede cancelar el menú de compartir.
    if (
      error instanceof Error &&
      error.name === 'AbortError'
    ) {
      return;
    }

    await mostrarToast(
      error instanceof Error
        ? error.message
        : 'No se pudo compartir el comprobante.',
      'danger'
    );

  } finally {
    pdfProcesandoId.value = null;
    pdfOperacion.value = null;
  }
};


// LIBERAR IMAGEN QR ANTERIOR
const liberarImagenQr = () => {
  if (qrUrl.value) {
    URL.revokeObjectURL(qrUrl.value);
    qrUrl.value = null;
  }
};


// GENERAR Y ABRIR QR
const abrirQr = async (item: SolicitudApi) => {
  if (item.id <= 0) {
    await mostrarToast(
      'Primero necesitás sincronizar la solicitud.',
      'warning'
    );

    return;
  }

  if (qrCargandoId.value !== null) {
    return;
  }

  if (pdfProcesandoId.value !== null) {
    return;
  }

  qrCargandoId.value = item.id;

  liberarImagenQr();

  try {
    const imagen = await obtener_qr_solicitud_api(item.id);

    qrUrl.value = URL.createObjectURL(imagen);
    solicitudQrId.value = item.id;
    modalQrAbierto.value = true;

  } catch (error) {
    console.error(
      'Error al generar el QR:',
      error
    );

    await mostrarToast(
      error instanceof Error
        ? error.message
        : 'No se pudo generar el código QR.',
      'danger'
    );

  } finally {
    qrCargandoId.value = null;
  }
};


// CERRAR QR Y LIBERAR MEMORIA
const cerrarQr = () => {
  modalQrAbierto.value = false;
  liberarImagenQr();
  solicitudQrId.value = null;
};


// FORMATEAR FECHA
const formatearFecha = (fecha: string) => {
  if (!fecha) return 'Sin fecha';

  const valor = new Date(fecha);

  if (Number.isNaN(valor.getTime())) return fecha;

  return valor.toLocaleDateString('es-AR');
};


// ACTUALIZAR DESPUÉS DE SINCRONIZAR
const actualizarTrasSincronizacion = () => {
  void cargarMisSolicitudes();
};


// ACTUALIZAR AL RECUPERAR INTERNET
const actualizarAlVolverInternet = () => {
  void cargarMisSolicitudes();
};


// INICIO
onMounted(() => {
  void cargarServicios();
  void cargarMisSolicitudes();

  window.addEventListener(
    'solicitudes-sincronizadas',
    actualizarTrasSincronizacion
  );

  window.addEventListener(
    'online',
    actualizarAlVolverInternet
  );
});


// LIMPIEZA
onUnmounted(() => {
  window.removeEventListener(
    'solicitudes-sincronizadas',
    actualizarTrasSincronizacion
  );

  window.removeEventListener(
    'online',
    actualizarAlVolverInternet
  );

  liberarImagenQr();
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

.coordenadas {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 6px 0;
  font-size: 13px;
}

.estado-firma {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 0.9rem;
}

.estado-firma.registrada {
  color: var(--ion-color-success);
}

.estado-firma.pendiente {
  color: var(--ion-color-warning-shade);
}

.qr-contenedor {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  text-align: center;
  padding: 16px;
}

.imagen-qr {
  width: 100%;
  max-width: 280px;
  height: auto;
  object-fit: contain;
  background: white;
  padding: 12px;
  border-radius: 12px;
}
</style>
