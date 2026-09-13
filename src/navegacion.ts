import { 
  homeOutline, 
  locationOutline, 
  documentTextOutline, 
  personOutline 
} from 'ionicons/icons';

export interface RutaNavegacion {
  name: string;
  path: string;
  titulo: string;
  icono: string;
  componente: () => Promise<any>;
  meta?: {
    roles: string[];
  };
}

export const rutasNavegacion: RutaNavegacion[] = [
  {
    name: 'Home',
    path: '/home',
    titulo: 'Inicio',
    icono: homeOutline,
    componente: () => import('@/views/HomePage.vue'),
    meta: { roles: ['TODOS'] }, // Accesible para todos los autenticados
  },
  {
    name: 'Puntos',
    path: '/puntos',
    titulo: 'Puntos de Retiro',
    icono: locationOutline,
    componente: () => import('@/views/PuntosPage.vue'),
    meta: { roles: ['administrador', 'recolector'] }, // Admin y operativo
  },
  {
    name: 'Solicitudes',
    path: '/solicitudes',
    titulo: 'Solicitudes',
    icono: documentTextOutline,
    componente: () => import('@/views/SolicitudesPage.vue'),
    meta: { roles: ['administrador'] }, // Exclusivo del administrador
  },
  {
    name: 'MiCuenta',
    path: '/micuenta',
    titulo: 'Mi Cuenta',
    icono: personOutline,
    componente: () => import('@/views/MiCuentaPage.vue'),
    meta: { roles: ['TODOS'] }, // Accesible para todos (cada quien edita su perfil)
  },
];