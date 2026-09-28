import { Filesystem, Directory } from '@capacitor/filesystem';
import { Share } from '@capacitor/share';
import { obtener_api_url } from '@/config/debug';

export async function descargarYCompartirPdf(servicioId: number, nombreServicio: string) {
  try {
    // 1. Construimos la URL completa del endpoint de la API
    const baseUrl = obtener_api_url();
    const url = `${baseUrl}/servicios/${servicioId}/pdf`;

    // 2. Solicitamos el PDF usando fetch directamente (para manejar la respuesta como blob)
    const token = localStorage.getItem('token');
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        ...(token ? { 'Authorization': `Bearer ${token}` } : {})
      }
    });

    if (!response.ok) {
      // Si el servidor devuelve un 409 Conflict u otro error, lanzamos un objeto con el status
      const errorData = await response.json().catch(() => ({}));
      const err: any = new Error(errorData?.mensaje || `Error HTTP: ${response.status}`);
      err.status = response.status;
      throw err;
    }

    const blob = await response.blob();

    // 3. Convertimos el blob a Base64 para guardarlo localmente en el celular
    const base64Data = await blobToBase64(blob);
    const fileName = `comprobante_servicio_${servicioId}.pdf`;

    // 4. Guardamos el archivo temporalmente en la caché del dispositivo
    const savedFile = await Filesystem.writeFile({
      path: fileName,
      data: base64Data,
      directory: Directory.Cache
    });

    // 5. Abrimos el menú nativo del celular para compartir el archivo
    await Share.share({
      title: 'Comprobante de Recolección - GANICA',
      text: `Aquí tenés el comprobante oficial del servicio: ${nombreServicio}`,
      url: savedFile.uri,
      dialogTitle: 'Compartir Comprobante PDF'
    });

  } catch (error: any) {
    console.error('Error al generar o compartir el PDF:', error);
    throw error;
  }
}

// Función auxiliar para pasar de Blob a Base64
function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => {
      const base64String = reader.result as string;
      resolve(base64String);
    };
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}