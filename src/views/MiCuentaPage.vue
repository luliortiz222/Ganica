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
      <h2 class="app-titulo-ganica">Portal de Autenticación</h2>

      <!-- SI NO ESTÁ AUTENTICADO: MOSTRAR FORMULARIO DE LOGIN -->
      <ion-card v-if="!autenticado">
        <ion-card-header>
          <ion-card-title>Iniciar Sesión</ion-card-title>
          <ion-card-subtitle>Ingrese sus credenciales de Ganica</ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <form @submit.prevent="handleLogin">
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

            <ion-button expand="block" color="primary" class="ion-margin-top" type="submit">
              Ingresar
            </ion-button>
          </form>
        </ion-card-content>
      </ion-card>

      <!-- SI ESTÁ AUTENTICADO: MOSTRAR PERFIL Y ROL -->
      <ion-card v-else>
        <ion-card-header>
          <ion-card-title>{{ usuarioInfo.email }}</ion-card-title>
          <ion-card-subtitle>Rol en el sistema: <strong>{{ usuarioInfo.rol || 'Sin rol asignado' }}</strong></ion-card-subtitle>
        </ion-card-header>

        <ion-card-content>
          <div v-if="!usuarioInfo.rol" class="ion-padding-bottom">
            <p color="warning">Su cuenta está activa pero aún no cuenta con un rol asignado para operar en las secciones del sistema[cite: 1].</p>
          </div>

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

            <ion-item>
              <ion-toggle v-model="perfil.notificaciones">
                Recibir alertas de recolección
              </ion-toggle>
            </ion-item>

            <ion-item>
              <ion-toggle :checked="esOscuro" @ionChange="alternarTema">
                Modo Oscuro
              </ion-toggle>
            </ion-item>
          </ion-list>

          <ion-button expand="block" color="success" class="ion-margin-top" @click="guardarPerfil">
            <ion-icon :icon="saveOutline" slot="start" />
            Guardar Cambios
          </ion-button>

          <ion-button expand="block" color="danger" class="ion-margin-top" @click="cerrarSesion">
            Cerrar Sesión
          </ion-button>
        </ion-card-content>
      </ion-card>
    </ion-content>
  </ion-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { 
  IonPage, IonHeader, IonToolbar, IonTitle, IonContent, 
  IonButtons, IonMenuButton, IonCard, IonCardHeader, 
  IonCardTitle, IonCardSubtitle, IonCardContent, IonList, 
  IonItem, IonInput, IonToggle, IonButton, IonIcon, 
  toastController 
} from '@ionic/vue';
import { saveOutline } from 'ionicons/icons';
import { obtenerTemaGuardado, aplicarTema } from '@/config/tema';
import { authService } from '@/servicies/auth_service';

const PERFIL_KEY = 'ganica_perfil_usuario';
const esOscuro = ref(false);
const autenticado = ref(false);

const credenciales = ref({
  email: '',
  password: ''
});

const usuarioInfo = ref({
  email: '',
  rol: ''
});

const perfil = ref({
  nombre: 'Lourdes Ortiz',
  direccion: 'Av. Mitre 1234',
  notificaciones: true
});

onMounted(() => {
  esOscuro.value = obtenerTemaGuardado();
  verificarSesion();
  
  const guardado = localStorage.getItem(PERFIL_KEY);
  if (guardado) {
    perfil.value = JSON.parse(guardado);
  }
});

const verificarSesion = () => {
  autenticado.value = authService.estaAutenticado();
  if (autenticado.value) {
    const userStr = localStorage.getItem('usuario');
    if (userStr) {
      try {
        usuarioInfo.value = JSON.parse(userStr);
      } catch (e) {
        usuarioInfo.value = { email: '', rol: '' };
      }
    }
  }
};

const handleLogin = async () => {
  try {
    await authService.login(credenciales.value);
    verificarSesion();
    
    const toast = await toastController.create({
      message: '¡Inicio de sesión exitoso!',
      duration: 2000,
      color: 'success',
      position: 'bottom'
    });
    await toast.present();
  } catch (error) {
    const toast = await toastController.create({
      message: 'Error al iniciar sesión. Verifique sus credenciales.',
      duration: 2500,
      color: 'danger',
      position: 'bottom'
    });
    await toast.present();
  }
};

const cerrarSesion = () => {
  authService.logout();
  autenticado.value = false;
  usuarioInfo.value = { email: '', rol: '' };
};

const alternarTema = (event: CustomEvent) => {
  esOscuro.value = event.detail.checked;
  aplicarTema(esOscuro.value);
};

const guardarPerfil = async () => {
  localStorage.setItem(PERFIL_KEY, JSON.stringify(perfil.value));
  
  const toast = await toastController.create({
    message: 'Perfil actualizado con éxito',
    duration: 2000,
    color: 'success',
    position: 'bottom'
  });
  await toast.present();
};
</script>