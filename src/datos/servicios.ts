export interface Servicio {
  id: number;
  titulo: string;
  detalle: string;
  horario: string;
  dias: string;
}

export const LISTA_SERVICIOS: Servicio[] = [
  {
    id: 1,
    titulo: 'Residuos Domiciliarios',
    detalle: 'Sacar los residuos en bolsas cerradas y en el cesto de tu domicilio.',
    horario: '18:00 a 22:00 hs',
    dias: 'Lunes a Viernes'
  },
  {
    id: 2,
    titulo: 'Recolección Diferenciada (Reciclables)',
    detalle: 'Se retira plástico, cartón, vidrio y metal secos y limpios.',
    horario: '08:00 a 12:00 hs',
    dias: 'Martes y Jueves'
  },
  {
    id: 3,
    titulo: 'Residuos Voluminosos y Poda',
    detalle: 'Previa solicitud por la app. Retiro de restos de poda, escombros en bolsas y electrodomésticos en desuso.',
    horario: 'A coordinar',
    dias: 'Previa solicitud'
  }
];

export function obtenerServicios(): Promise<Servicio[]> {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve(LISTA_SERVICIOS);
    }, 1500); 
  });
}