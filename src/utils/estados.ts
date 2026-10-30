
import {
  cloudOfflineOutline,
  checkmarkCircleOutline,
  timeOutline,
  alertCircleOutline,
  carOutline
} from 'ionicons/icons';

export const ESTADOS_SOLICITUD = {
  sin_enviar: {
    texto: 'Sin enviar',
    color: 'warning',
    icono: cloudOfflineOutline
  },

  enviada: {
    texto: 'Enviada',
    color: 'success',
    icono: checkmarkCircleOutline
  },

  Pendiente: {
    texto: 'Pendiente',
    color: 'warning',
    icono: timeOutline
  },

  Observada: {
    texto: 'Observada',
    color: 'danger',
    icono: alertCircleOutline
  },

  Finalizada: {
    texto: 'Finalizada',
    color: 'success',
    icono: checkmarkCircleOutline
  }
} as const;
