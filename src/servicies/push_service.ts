
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
 * Mostrar una notificación local cuando llega un push
 * mientras Gánica está abierta.
 */
async function mostrarNotificacionConAppAbierta(
  titulo: string,
  mensaje: string
): Promise<void> {
  try {
    const permisos = await LocalNotifications.checkPermissions();

    if (permisos.display !== 'granted') {
      console.log(
        'Push: no se pueden mostrar notificaciones locales porque falta el permiso.'
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
      'Push: notificación visible generada con la aplicación abierta.'
    );
  } catch (error) {
    console.error(
      'Push: error al mostrar la notificación local:',
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
      'Push: las notificaciones se inicializan solamente en dispositivos nativos.'
    );
    return;
  }

  inicializado = true;

  try {
    // Crear el canal de notificaciones de Android.
    if (Capacitor.getPlatform() === 'android') {
      await PushNotifications.createChannel({
        id: 'avisos',
        name: 'Avisos de Gánica',
        description: 'Notificaciones de solicitudes y recolección',
        importance: 5,
        visibility: 1,
        sound: 'default'
      });
    }

    // Escuchar cuando Firebase entrega el token.
    await PushNotifications.addListener(
      'registration',
      async (token: Token) => {
        tokenFCM = token.value;

        console.log(
          'Push: token del dispositivo recibido correctamente.'
        );

        await registrarTokenEnServidor(token.value);
      }
    );

    // Escuchar errores de registro.
    await PushNotifications.addListener(
      'registrationError',
      (error) => {
        console.error(
          'Push: error al obtener el token:',
          error
        );
      }
    );

    // Escuchar notificaciones recibidas con la app abierta.
    await PushNotifications.addListener(
      'pushNotificationReceived',
      async (notification) => {
        console.log(
          'Push: notificación recibida:',
          notification.title,
          notification.body
        );

        await mostrarNotificacionConAppAbierta(
          notification.title || 'Gánica',
          notification.body || 'Tenés una nueva notificación.'
        );
      }
    );

    // Escuchar cuando el usuario toca una notificación.
    await PushNotifications.addListener(
      'pushNotificationActionPerformed',
      (action) => {
        console.log(
          'Push: el usuario abrió una notificación:',
          action.notification
        );
      }
    );

    // Solicitar permisos de notificaciones push.
    let permisos = await PushNotifications.checkPermissions();

    if (permisos.receive === 'prompt') {
      permisos = await PushNotifications.requestPermissions();
    }

    if (permisos.receive !== 'granted') {
      console.log(
        'Push: el usuario no concedió permiso para notificaciones.'
      );
      return;
    }

    // Solicitar permiso para notificaciones locales.
    const permisosLocales =
      await LocalNotifications.checkPermissions();

    if (permisosLocales.display !== 'granted') {
      await LocalNotifications.requestPermissions();
    }

    // Solicitar el registro en Firebase.
    await PushNotifications.register();

    // Si ya había un token guardado, intentar registrarlo
    // para la sesión actual.
    const tokenGuardado = localStorage.getItem(TOKEN_FCM_KEY);

    if (tokenGuardado) {
      tokenFCM = tokenGuardado;
      await registrarTokenEnServidor(tokenGuardado);
    }

    // Registrar el dispositivo cuando cambia la sesión.
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
      'Push: error al inicializar el servicio:',
      error
    );
  }
}

