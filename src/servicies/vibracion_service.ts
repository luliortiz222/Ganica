import { Haptics, ImpactStyle, NotificationType } from '@capacitor/haptics';
import { use_tema_store } from '../stores/tema_store'; // Ruta relativa corregida

function vibracion_apagada() {
  return !use_tema_store().vibracion_activa;
}

export async function vibrar_toque() {
  if (vibracion_apagada()) return;
  try {
    await Haptics.impact({ style: ImpactStyle.Light });
  } catch {
    // Sin motor háptico (escritorio)
  }
}

export async function vibrar_error() {
  if (vibracion_apagada()) return;
  try {
    // Usamos directamente el tipo de notificación o string compatible
    await Haptics.notification({ type: NotificationType.Error });
  } catch {
    // Silencio si no hay hardware
  }
}