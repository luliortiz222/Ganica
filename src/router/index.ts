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
    meta: ruta.meta // Aseguramos que pasen los roles y permisos definidos en la navegación
  }))
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
});

// El Guard que por fin bloquea y lee los roles según la versión 5
router.beforeEach((to, from, next) => {
  const autenticado = authService.estaAutenticado();
  const meta = to.meta as { publica?: boolean; roles?: string[] };

  // 1. Si la ruta es pública
  if (meta?.publica) {
    if (autenticado) {
      return next({ path: '/home', replace: true });
    }
    return next();
  }

  // 2. Si no está autenticado, va al login
 if (!autenticado && to.path !== '/micuenta') {
  return next({ path: '/micuenta', replace: true });
}

  // 3. Validación de roles y permisos
  const rolesPermitidos = meta?.roles;
  if (!rolesPermitidos || rolesPermitidos.includes('TODOS')) {
    return next();
  }

  // Recuperamos el rol activo del usuario logueado
  const usuarioGuardado = localStorage.getItem('usuario');
  let rolUsuario = '';
  if (usuarioGuardado) {
    try {
      const parsed = JSON.parse(usuarioGuardado);
      rolUsuario = parsed.rol;
    } catch (e) {
      rolUsuario = '';
    }
  }

  if (rolesPermitidos.includes(rolUsuario)) {
    return next();
  }

  // 4. Autenticado pero sin permiso: vuelve a Inicio (403 explicado) en vez de echarlo al login
  return next({ path: '/home', replace: true });
});

export default router;