```vue
<template>
  <ion-page>
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-menu-button />
        </ion-buttons>

        <ion-title>Mi Cuenta</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">

      <h2 class="app-titulo-ganica">
        Portal de Autenticación
      </h2>

      <!-- USUARIO NO AUTENTICADO -->

      <ion-card v-if="!autenticado">

        <!-- FORMULARIO DE LOGIN -->

        <template v-if="!modoRegistro">

          <ion-card-header>
            <ion-card-title>
              Iniciar Sesión
            </ion-card-title>

            <ion-card-subtitle>
              Ingrese sus credenciales de Ganica
            </ion-card-subtitle>
          </ion-card-header>

          <ion-card-content>

            <form @submit.prevent="handle">

              <ion-list lines="full">

                <ion-item>
                  <ion-input
                    v-model="credenciales.email"
                    type="email"
                    label="Correo electrónico"
                    label-placement="stacked"
                    placeholder="admin@ganica.gob.ar"
                    required
                  />
                </ion-item>

                <ion-item>
                  <ion-input
                    v-model="credenciales.password"
                    type="password"
                    label="Contraseña"
                    label-placement="stacked"
                    placeholder="********"
                    required
                  />
                </ion-item>

              </ion-list>

              <ion-button
                expand="block"
                color="primary"
                class="ion-margin-top"
                type="submit"
              >
                Ingresar
              </ion-button>

            </form>

            <!-- ENTRAR AL REGISTRO -->

            <div class="ion-text-center ion-margin-top">

              <p>
                ¿No tenés una cuenta?
              </p>

              <ion-button
                fill="clear"
                color="primary"
                @click="mostrarRegistro"
              >
                Registrate
              </ion-button>

            </div>

          </ion-card-content>

        </template>

        <!-- FORMULARIO DE REGISTRO -->

        <template v-else>

          <ion-card-header>

            <ion-card-title>
              Crear una cuenta
            </ion-card-title>

            <ion-card-subtitle>
              Registrate para utilizar Ganica
            </ion-card-subtitle>

          </ion-card-header>


          <ion-card-content>

            <form @submit.prevent="registrar">

              <ion-list lines="full">

                <!-- EMAIL -->

                <ion-item>
                  <ion-input
                    v-model="registro.email"
                    type="email"
                    label="Correo electrónico"
                    label-placement="stacked"
                    placeholder="tu@email.com"
                    required
                  />
                </ion-item>


                <!-- CONTRASEÑA -->

                <ion-item>
                  <ion-input
                    v-model="registro.password"
                    type="password"
                    label="Contraseña"
                    label-placement="stacked"
                    placeholder="********"
                    required
                  />
                </ion-item>


                <!-- CONFIRMAR CONTRASEÑA -->

                <ion-item>
                  <ion-input
                    v-model="registro.confirmarPassword"
                    type="password"
                    label="Confirmar contraseña"
                    label-placement="stacked"
                    placeholder="********"
                    required
                  />
                </ion-item>

              </ion-list>


              <!-- BOTÓN REGISTRARSE -->

              <ion-button
                expand="block"
                color="success"
                class="ion-margin-top"
                type="submit"
              >
                Registrarse
              </ion-button>

            </form>


            <!-- VOLVER AL LOGIN -->

            <div class="ion-text-center ion-margin-top">

              <p>
                ¿Ya tenés una cuenta?
              </p>

              <ion-button
                fill="clear"
                color="primary"
                @click="mostrarLogin"
              >
                Iniciar sesión
              </ion-button>

            </div>

          </ion-card-content>

        </template>

      </ion-card>


      <!-- USUARIO AUTENTICADO -->

      <ion-card v-else>

        <ion-card-header>

          <ion-card-title>
            {{ usuarioInfo.email }}
          </ion-card-title>

          <ion-card-subtitle>
            Rol en el sistema:

            <strong>
              {{ usuarioInfo.rol || 'Sin rol asignado' }}
            </strong>
          </ion-card-subtitle>

        </ion-card-header>


        <ion-card-content>

          <!-- CUENTA SIN ROL -->

          <div
            v-if="!usuarioInfo.rol"
            class="ion-padding-bottom"
          >
            <p color="warning">
              Su cuenta está activa pero aún no cuenta con un rol
              asignado para operar en las secciones del sistema.
            </p>
          </div>


          <!-- DATOS DEL PERFIL -->

          <ion-list lines="full">

            <ion-item>

              <ion-input
                v-model="perfil.nombre"
                label="Nombre completo"
                label-placement="stacked"
              />

            </ion-item>


            <ion-item>

              <ion-input
                v-model="perfil.direccion"
                label="Dirección principal"
                label-placement="stacked"
              />

            </ion-item>


            <!-- NOTIFICACIONES -->

            <ion-item>

              <ion-toggle
                v-model="perfil.notificaciones"
              >
                Recibir alertas de recolección
              </ion-toggle>

            </ion-item>


            <!-- MODO OSCURO -->

            <ion-item>

              <ion-toggle
                :checked="esOscuro"
                @ionChange="alternarTema"
              >
                Modo Oscuro
              </ion-toggle>

            </ion-item>


            <!-- VIBRACIÓN -->

            <ion-item>

              <ion-toggle
                :checked="vibracionActiva"
                @ionChange="cambiarVibracion"
              >
                Vibración háptica al tacto
              </ion-toggle>

            </ion-item>

          </ion-list>


          <!-- GUARDAR PERFIL -->

          <ion-button
            expand="block"
            color="success"
            class="ion-margin-top"
            @click="guardarPerfil"
          >

            <ion-icon
              :icon="saveOutline"
              slot="start"
            />

            Guardar Cambios

          </ion-button>


          <!-- CERRAR SESIÓN -->

          <ion-button
            expand="block"
            color="danger"
            class="ion-margin-top"
            @click="cerrarSesion"
          >
            Cerrar Sesión
          </ion-button>

        </ion-card-content>

      </ion-card>

    </ion-content>
  </ion-page>
</template>


<script setup lang="ts">

import {
  ref,
  computed,
  onMounted
} from 'vue';


import {
  IonPage,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonButtons,
  IonMenuButton,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardSubtitle,
  IonCardContent,
  IonList,
  IonItem,
  IonInput,
  IonToggle,
  IonButton,
  IonIcon,
  toastController
} from '@ionic/vue';


import { saveOutline } from 'ionicons/icons';


import {
  obtenerTemaGuardado,
  aplicarTema
} from '@/config/tema';


import {
  authService
} from '@/servicies/auth_service';


import {
  vibrar_toque
} from '@/servicies/vibracion_service';


import {
  use_tema_store
} from '../stores/tema_store';


/* VIBRACIÓN */

const vibracionActiva = computed(() => {

  const temaStore = use_tema_store();

  return temaStore.vibracion_activa;

});


const cambiarVibracion = (event: CustomEvent) => {

  const temaStore = use_tema_store();

  temaStore.alternar_vibracion();

  if (temaStore.vibracion_activa) {
    vibrar_toque();
  }

};


/* Variables generales */

const PERFIL_KEY = 'ganica_perfil_usuario';

const esOscuro = ref(false);

const autenticado = ref(false);

const modoRegistro = ref(false);


/* CREDENCIALES LOGIN */

const credenciales = ref({

  email: '',

  password: ''

});


/* DATOS REGISTRO */

const registro = ref({

  email: '',

  password: '',

  confirmarPassword: ''

});


/* INFORMACIÓN DEL USUARIO */

const usuarioInfo = ref({

  email: '',

  rol: ''

});


/* PERFIL LOCAL */

const perfil = ref({

  nombre: '',

  direccion: '',

  notificaciones: true

});


const esSinRol = ref(false);

/* AL INICIAR LA VISTA */


onMounted(() => {

  esOscuro.value = obtenerTemaGuardado();

  verificarSesion();


  const guardado =
    localStorage.getItem(PERFIL_KEY);


  if (guardado) {

    perfil.value =
      JSON.parse(guardado);

  }

});

/* VERIFICAR SESIÓN */

const verificarSesion = () => {

  autenticado.value =
    authService.estaAutenticado();


  if (autenticado.value) {

    const userStr =
      localStorage.getItem('usuario');


    if (userStr) {

      try {

        usuarioInfo.value =
          JSON.parse(userStr);


        esSinRol.value =
          !usuarioInfo.value.rol ||
          usuarioInfo.value.rol ===
            'Sin rol asignado';


        if (!perfil.value.nombre) {

          perfil.value.nombre =
            usuarioInfo.value.email;

        }

      } catch (e) {

        usuarioInfo.value = {

          email: '',

          rol: ''

        };

      }

    }

  }

};


/* MOSTRAR REGISTRO */
const mostrarRegistro = () => {

  modoRegistro.value = true;


  registro.value = {

    email: '',

    password: '',

    confirmarPassword: ''

  };

};


/* VOLVER AL LOGIN */

const mostrarLogin = () => {

  modoRegistro.value = false;


  registro.value = {

    email: '',

    password: '',

    confirmarPassword: ''

  };

};


/* INICIAR SESIÓN */

const handle = async () => {

  try {

    await authService.login(
      credenciales.value
    );


    verificarSesion();


    const toast =
      await toastController.create({

        message:
          '¡Inicio de sesión exitoso!',

        duration: 2000,

        color: 'success',

        position: 'bottom'

      });


    await toast.present();


  } catch (error) {

    const toast =
      await toastController.create({

        message:
          'Error al iniciar sesión. Verifique sus credenciales.',

        duration: 2500,

        color: 'danger',

        position: 'bottom'

      });


    await toast.present();

  }

};


/* REGISTRAR USUARIO */

const registrar = async () => {
  /* Verificar contraseña*/

  if (
    registro.value.password !==
    registro.value.confirmarPassword
  ) {

    const toast =
      await toastController.create({

        message:
          'Las contraseñas no coinciden.',

        duration: 2500,

        color: 'warning',

        position: 'bottom'

      });


    await toast.present();

    return;

  }


  /* Longitud minima */

  if (
    registro.value.password.length < 6
  ) {

    const toast =
      await toastController.create({

        message:
          'La contraseña debe tener al menos 6 caracteres.',

        duration: 2500,

        color: 'warning',

        position: 'bottom'

      });


    await toast.present();

    return;

  }


  /* Enviar registro */

  try {

    await authService.registrar({

      email:
        registro.value.email,

      password:
        registro.value.password

    });


    const toast =
      await toastController.create({

        message:
          '¡Cuenta creada correctamente! Ahora podés iniciar sesión.',

        duration: 3000,

        color: 'success',

        position: 'bottom'

      });


    await toast.present();


    /* Volver al login */

    modoRegistro.value = false;


    /* Limpiar formulario */

    registro.value = {

      email: '',

      password: '',

      confirmarPassword: ''

    };


  } catch (error) {

    const mensaje =
      error instanceof Error
        ? error.message
        : 'No se pudo crear la cuenta.';


    const toast =
      await toastController.create({

        message: mensaje,

        duration: 3000,

        color: 'danger',

        position: 'bottom'

      });


    await toast.present();

  }

};

/* Cerrar sesion */


const cerrarSesion = () => {

  authService.logout();


  autenticado.value = false;


  usuarioInfo.value = {

    email: '',

    rol: ''

  };

};

/* Cambiar tema */

const alternarTema = (
  event: CustomEvent
) => {

  esOscuro.value =
    event.detail.checked;


  aplicarTema(
    esOscuro.value
  );

};

/* Guardar Perfil */

const guardarPerfil = async () => {

  localStorage.setItem(

    PERFIL_KEY,

    JSON.stringify(
      perfil.value
    )

  );


  const toast =
    await toastController.create({

      message:
        'Perfil actualizado con éxito',

      duration: 2000,

      color: 'success',

      position: 'bottom'

    });


  await toast.present();

};

</script>
```