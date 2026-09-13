export function obtener_api_url() {
  // Verificamos si estamos corriendo en Capacitor (dispositivo nativo / Android)
  const esNativo = window.location.protocol === 'capacitor:' || (window as any).Capacitor !== undefined;

  if (esNativo) {
    // Si estás en el celular por USB con 'adb reverse' o usando la IP de red local:
    // Opción A (con adb reverse): "http://localhost:5193/api"
    // Opción B (por IP de red Wi-Fi): "http://192.168.100.239:5193/api"
    return "http://localhost:5193/api"; 
  }

  // Si estás en el navegador de la PC con 'npm run dev'
  return "http://localhost:5193/api";
}