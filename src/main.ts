
import { createApp } from 'vue'
import App from './App.vue'
import router from './router';

import { IonicVue } from '@ionic/vue';
import { createPinia } from 'pinia';
import { iniciar_offline_service } from './servicies/offline_service';
import { iniciar_push_service } from './servicies/push_service';
import { SocialLogin } from '@capgo/capacitor-social-login';

/* Core CSS required for Ionic components to work properly */
import '@ionic/vue/css/core.css';

/* Basic CSS for apps built with Ionic */
import '@ionic/vue/css/normalize.css';
import '@ionic/vue/css/structure.css';
import '@ionic/vue/css/typography.css';

/* Optional CSS utils that can be commented out */
import '@ionic/vue/css/padding.css';
import '@ionic/vue/css/float-elements.css';
import '@ionic/vue/css/text-alignment.css';
import '@ionic/vue/css/text-transformation.css';
import '@ionic/vue/css/flex-utils.css';
import '@ionic/vue/css/display.css';

/* Ionic Dark Mode */
import '@ionic/vue/css/palettes/dark.class.css';

/* Theme variables */
import './theme/variables.css';

const app = createApp(App);

const pinia = createPinia();
iniciar_offline_service();

app
  .use(IonicVue)
  .use(router)
  .use(pinia);

/* INICIALIZAR GOOGLE, PUSH Y ARRANCAR LA APLICACIÓN */
const iniciarAplicacion = async () => {
  try {
    await SocialLogin.initialize({
      google: {
        webClientId: '9492405865-9m3cu3m9gip3jaa1tgjcb13dhij3nl75.apps.googleusercontent.com',
        mode: 'online'
      }
    });

    console.log('Google Login inicializado correctamente.');
  } catch (error) {
    console.error('Error al inicializar Google Login:', error);
  }

  await router.isReady();
  app.mount('#app');

  // Inicializar notificaciones push después de montar la aplicación.
  try {
    await iniciar_push_service();
  } catch (error) {
    console.error(
      'Error al iniciar las notificaciones push:',
      error
    );
  }
};

iniciarAplicacion();
