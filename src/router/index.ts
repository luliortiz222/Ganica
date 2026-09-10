import { createRouter, createWebHistory } from '@ionic/vue-router';
import { RouteRecordRaw } from 'vue-router';
import { rutasNavegacion } from '@/navegacion';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/home'
  },
  ...rutasNavegacion.map((ruta) => ({
    path: ruta.path,
    name: ruta.name,
    component: ruta.componente
  }))
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
});

export default router;
