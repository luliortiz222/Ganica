const PREFIJO = "ganica_offline_";

function guardar_copia(endpoint: string, datos: any): void {
  localStorage.setItem(
    `${PREFIJO}${endpoint}`,
    JSON.stringify(datos)
  );
}

function obtener_copia<T = any>(endpoint: string): T | null {
  const datos = localStorage.getItem(`${PREFIJO}${endpoint}`);

  if (!datos) {
    return null;
  }

  try {
    return JSON.parse(datos) as T;
  } catch {
    return null;
  }
}

function eliminar_copia(endpoint: string): void {
  localStorage.removeItem(`${PREFIJO}${endpoint}`);
}

export const offline_store = {
  guardar_copia,
  obtener_copia,
  eliminar_copia
};