const CLAVE_COLA = "ganica_offline_queue";

export interface OperacionPendiente {
  id: string;
  endpoint: string;
  options: RequestInit;
  creadoEn: string;
}

function obtener_cola(): OperacionPendiente[] {
  const datos = localStorage.getItem(CLAVE_COLA);

  if (!datos) {
    return [];
  }

  try {
    return JSON.parse(datos) as OperacionPendiente[];
  } catch {
    return [];
  }
}

function guardar_cola(cola: OperacionPendiente[]): void {
  localStorage.setItem(
    CLAVE_COLA,
    JSON.stringify(cola)
  );
}

function agregar_operacion(
  endpoint: string,
  options: RequestInit
): string {

  const cola = obtener_cola();

  const operacion: OperacionPendiente = {
    id: `${Date.now()}-${Math.random()}`,
    endpoint,
    options,
    creadoEn: new Date().toISOString()
  };

  cola.push(operacion);

  guardar_cola(cola);

  return operacion.id;
}

function eliminar_operacion(id: string): void {
  const cola = obtener_cola();

  const nueva_cola = cola.filter(
    operacion => operacion.id !== id
  );

  guardar_cola(nueva_cola);
}

export const offline_queue = {
  obtener_cola,
  agregar_operacion,
  eliminar_operacion
};