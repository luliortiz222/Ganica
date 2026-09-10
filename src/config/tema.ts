const TEMA_KEY = 'ganica_tema_oscuro';

export function obtenerTemaGuardado(): boolean {
  const guardado = localStorage.getItem(TEMA_KEY);
  if (guardado !== null) {
    return JSON.parse(guardado);
  }
  return false;
}

export function aplicarTema(esOscuro: boolean): void {
  document.body.classList.toggle('dark', esOscuro);
  document.documentElement.classList.toggle('dark', esOscuro);
  document.documentElement.classList.toggle('ion-palette-dark', esOscuro);
  localStorage.setItem(TEMA_KEY, JSON.stringify(esOscuro));
}

export function inicializarTema(): boolean {
  const esOscuro = obtenerTemaGuardado();
  aplicarTema(esOscuro);
  return esOscuro;
}