
import { Capacitor } from '@capacitor/core';

import {
  PushNotifications,
  Token
} from '@capacitor/push-notifications';

import {
  LocalNotifications
} from '@capacitor/local-notifications';

import { obtener_api_url } from '@/config/debug';

const TOKEN_FCM_KEY = 'ganica_fcm_token';

let inicializado = false;
let tokenFCM: string | null = null;
let idNotificacionLocal = 1000;

/**
 * Crear los canales de notificaciones de Android.
 */
async function crearCanalesNotificaciones(): Promise<void> {
  if (Capacitor.getPlatform() !== 'android') return;

  await PushNotifications.createChannel({
    id: 'avisos',
    name: 'Avisos de Gánica',
    description: 'Notificaciones de solicitudes y recolección',
    importance: 5,
    visibility: 1,
    sound: 'default'
  });

  await LocalNotifications.createChannel({
    id: 'avisos',
    name: 'Avisos de Gánica',
    description: 'Notificaciones de solicitudes y recolección',
    importance: 5,
    visibility: 1,
    sound: 'default'
  });

  console.log('Push: canales de notificaciones creados.');
}

/**
 * Enviar el token del dispositivo a la API.
 */
async function registrarTokenEnServidor(
  token: string
): Promise<void> {
  try {
    const jwt = localStorage.getItem('token');

    if (!jwt) {
      console.log(
        'Push: no hay sesión iniciada. Se registrará más tarde.'
      );
      return;
    }

    const respuesta = await fetch(
      `${obtener_api_url()}/dispositivos`,
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${jwt}`
        },
        body: JSON.stringify({
          token,
          plataforma: Capacitor.getPlatform()
        })
      }
    );

    if (!respuesta.ok) {
      console.error(
        'Push: no se pudo registrar el dispositivo.',
        respuesta.status
      );
      return;
    }

    localStorage.setItem(TOKEN_FCM_KEY, token);

    console.log(
      'Push: dispositivo registrado correctamente.'
    );

  } catch (error) {
    console.error(
      'Push: error al registrar el dispositivo:',
      error
    );
  }
}

/**
 * Mostrar notificación local cuando llega un push
 * con Gánica abierta.
 */
async function mostrarNotificacionConAppAbierta(
  titulo: string,
  mensaje: string
): Promise<void> {
  try {
    const permisos = await LocalNotifications.checkPermissions();

    if (permisos.display !== 'granted') {
      console.warn(
        'Push: falta permiso para mostrar notificaciones locales.'
      );
      return;
    }

    await LocalNotifications.schedule({
      notifications: [
        {
          id: idNotificacionLocal++,
          title: titulo || 'Gánica',
          body: mensaje || 'Tenés una nueva notificación.',
          channelId: 'avisos',
          schedule: {
            at: new Date(Date.now() + 500)
          }
        }
      ]
    });

    console.log(
      'Push: notificación local programada correctamente.'
    );

  } catch (error) {
    console.error(
      'Push: error al mostrar notificación local:',
      error
    );
  }
}

/**
 * Inicializar las notificaciones push.
 */
export async function iniciar_push_service(): Promise<void> {

  if (inicializado) return;

  if (!Capacitor.isNativePlatform()) {
    console.log(
      'Push: las notificaciones requieren una plataforma nativa.'
    );
    return;
  }

  inicializado = true;

  try {

    // 1. Crear canales.
    await crearCanalesNotificaciones();

    // 2. Escuchar recepción del token FCM.
    await PushNotifications.addListener(
      'registration',
      async (token: Token) => {

        tokenFCM = token.value;

        console.log(
          'Push: token FCM recibido correctamente.'
        );

        await registrarTokenEnServidor(token.value);
      }
    );

    // 3. Errores de registro.
    await PushNotifications.addListener(
      'registrationError',
      (error) => {
        console.error(
          'Push: error al obtener token FCM:',
          error
        );
      }
    );

    // 4. Recibir notificaciones con Gánica abierta.
    await PushNotifications.addListener(
      'pushNotificationReceived',
      async (notification) => {

        console.log('🔔 ===== PUSH RECIBIDO EN GÁNICA =====');
console.log('📩 Notificación completa:', notification);
console.log('📌 Título:', notification.title);
console.log('💬 Mensaje:', notification.body);
console.log('📦 Datos:', notification.data);
console.log('🕒 Fecha:', new Date().toLocaleString());
console.log('🔔 =================================');

        // Mostrar cartel emergente de Android.
        await mostrarNotificacionConAppAbierta(
          notification.title || 'Gánica',
          notification.body || 'Tenés una nueva notificación.'
        );

        // Actualizar pantallas de la aplicación.
        window.dispatchEvent(
          new Event('ganica-notificaciones-actualizadas')
        );
      }
    );

    // 5. Cuando el usuario toca una notificación.
    await PushNotifications.addListener(
      'pushNotificationActionPerformed',
      (action) => {

        console.log(
          'Push: usuario abrió la notificación:',
          action.notification
        );

        window.dispatchEvent(
          new Event('ganica-notificaciones-actualizadas')
        );
      }
    );

    // 6. Comprobar permisos push.
    let permisos = await PushNotifications.checkPermissions();

    if (permisos.receive === 'prompt') {
      permisos = await PushNotifications.requestPermissions();
    }

    if (permisos.receive !== 'granted') {
      console.warn(
        'Push: el usuario no concedió permisos.'
      );
      inicializado = false;
      return;
    }

    // 7. Comprobar permisos locales.
    let permisosLocales =
      await LocalNotifications.checkPermissions();

    if (permisosLocales.display !== 'granted') {
      permisosLocales =
        await LocalNotifications.requestPermissions();
    }

    if (permisosLocales.display !== 'granted') {
      console.warn(
        'Push: no se concedieron permisos de notificaciones locales.'
      );
    }

    // 8. Registrar dispositivo en Firebase.
    await PushNotifications.register();

    // 9. Recuperar token guardado.
    const tokenGuardado = localStorage.getItem(TOKEN_FCM_KEY);

    if (tokenGuardado) {
      tokenFCM = tokenGuardado;
      await registrarTokenEnServidor(tokenGuardado);
    }

    // 10. Registrar nuevamente el dispositivo al cambiar sesión.
    window.addEventListener(
      'sesion-actualizada',
      () => {
        if (tokenFCM) {
          void registrarTokenEnServidor(tokenFCM);
        }
      }
    );

    console.log(
      'Push: servicio inicializado correctamente.'
    );

  } catch (error) {

    inicializado = false;

    console.error(
      'Push: error al inicializar:',
      error
    );
  }
}

