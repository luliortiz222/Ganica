import { createRouter, createWebHistory } from '@ionic/vue-router';
import { RouteRecordRaw } from 'vue-router';
import { rutasNavegacion } from '@/navegacion';
import { authService } from '@/servicies/auth_service';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: '/home'
  },
  ...rutasNavegacion.map((ruta: any) => ({
    path: ruta.path,
    name: ruta.name,
    component: ruta.componente,
    meta: ruta.meta
  }))
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
});

router.beforeEach((to, from, next) => {
  const autenticado = authService.estaAutenticado();
  const meta = to.meta as { publica?: boolean; roles?: string[] };

  // 1. Si va a Mi Cuenta, permitimos siempre para que pueda loguearse
  if (to.path === '/micuenta') {
    return next();
  }

  // 2. Si no está autenticado, lo mandamos a Mi Cuenta
  if (!autenticado) {
    return next({ path: '/micuenta', replace: true });
  }

  // 3. Si la ruta es abierta (TODOS) o no tiene roles estrictos, pasa
  const rolesPermitidos = meta?.roles;
  if (!rolesPermitidos || rolesPermitidos.includes('TODOS')) {
    return next();
  }

  // 4. Leemos el rol que guardó tu backend en el login original
  const usuarioGuardado = localStorage.getItem('usuario');
  let rolUsuario = '';
  
  if (usuarioGuardado) {
    try {
      const parsed = JSON.parse(usuarioGuardado);
      rolUsuario = parsed.rol || '';
    } catch (e) {
      rolUsuario = '';
    }
  }

  // 5. Verificamos si el rol del backend está permitido en esta ruta
  if (rolesPermitidos.includes(rolUsuario)) {
    return next();
  }

  // 6. Si tiene sesión pero no el rol necesario, vuelve a Home de forma segura
  return next({ path: '/home', replace: true });
});

export default router;