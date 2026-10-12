
import {
  cloudOfflineOutline,
  checkmarkCircleOutline
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
  }
} as const;