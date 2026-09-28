import { defineStore } from 'pinia';

const CLAVE_TEMA = 'ganica_tema';
const CLAVE_VIBRACION = 'ganica_vibracion';

export const use_tema_store = defineStore('tema', {
  state: () => ({
    oscuro: localStorage.getItem(CLAVE_TEMA) != 'claro',
    vibracion: localStorage.getItem(CLAVE_VIBRACION) != 'no'
  }),
  getters: {
    vibracion_activa(state) {
      return state.vibracion;
    }
  },
  actions: {
    alternar_vibracion() {
      this.vibracion = !this.vibracion;
      localStorage.setItem(CLAVE_VIBRACION, this.vibracion ? 'si' : 'no');
    }
  }
});