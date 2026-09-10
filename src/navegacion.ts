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
}

export const rutasNavegacion: RutaNavegacion[] = [
  {
    name: 'Home',
    path: '/home',
    titulo: 'Inicio',
    icono: homeOutline,
    componente: () => import('@/views/HomePage.vue'),
  },
  {
    name: 'Puntos',
    path: '/puntos',
    titulo: 'Puntos de Retiro',
    icono: locationOutline,
    componente: () => import('@/views/PuntosPage.vue'),
  },
  {
    name: 'Solicitudes',
    path: '/solicitudes',
    titulo: 'Solicitudes',
    icono: documentTextOutline,
    componente: () => import('@/views/SolicitudesPage.vue'),
  },
  {
    name: 'MiCuenta',
    path: '/micuenta',
    titulo: 'Mi Cuenta',
    icono: personOutline,
    componente: () => import('@/views/MiCuentaPage.vue'),
  },
];