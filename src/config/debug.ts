
export function obtener_api_url() {
  // Detectar si estamos ejecutando Gánica en Android
  const esNativo =
    window.location.protocol === 'capacitor:' ||
    (window as any).Capacitor !== undefined;

  if (esNativo) {
    // Android: conectarse a la API de la computadora
    // Ambos dispositivos deben estar conectados a la misma Wi-Fi.
    return 'http://192.168.100.239:5193/api';
  }

  // Navegador de la PC
  return 'http://localhost:5193/api';
}
