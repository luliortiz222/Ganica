import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';

export async function tomarFotoParaServicio() {
  try {
    // Esto disparará automáticamente el diálogo nativo de permisos la primera vez
    const imagen = await Camera.getPhoto({
      quality: 90,
      allowEditing: false,
      resultType: CameraResultType.Uri,
      source: CameraSource.Prompt, // Permite elegir entre Cámara o Galería
      width: 1600
    });

    return imagen.webPath; // Retorna la ruta temporal para mostrarla en la UI
  } catch (error) {
    console.error('Error al capturar la foto:', error);
    return null;
  }
}