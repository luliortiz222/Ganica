
import { obtener_api_url } from '@/config/debug';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegistroRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  refresh_token: string;
  expira_en: string;
  usuario: {
    id: number;
    email: string;
    rol: string;
  };
}

/**
 * Notifica a los componentes que cambió la sesión.
 */
const notificarCambioSesion = () => {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(
      new Event('sesion-actualizada')
    );
  }
};

export const authService = {
  /**
   * Iniciar sesión.
   */
  async login(
    credenciales: LoginRequest
  ): Promise<AuthResponse> {
    const url = `${obtener_api_url()}/sesion/login`;

    console.log(
      'EMAIL ENVIADO AL LOGIN:',
      credenciales.email
    );

    const respuesta = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credenciales),
    });

    if (!respuesta.ok) {
      const errorApi = await respuesta.json().catch(() => null);

      console.error(
        'ERROR DE LOGIN:',
        errorApi?.mensaje || respuesta.statusText
      );

      throw new Error(
        errorApi?.mensaje ||
        'Credenciales inválidas o error en el servidor'
      );
    }

    const data: AuthResponse = await respuesta.json();

    if (!data?.token || !data?.usuario) {
      throw new Error(
        'La respuesta del servidor no contiene una sesión válida.'
      );
    }

    console.log(
      'USUARIO DEVUELTO POR LA API:',
      data.usuario
    );

    const rolUsuario = String(
      data.usuario.rol ?? ''
    ).trim().toLowerCase();

    localStorage.setItem('token', data.token);

    if (data.refresh_token) {
      localStorage.setItem(
        'refreshToken',
        data.refresh_token
      );
    } else {
      localStorage.removeItem('refreshToken');
    }

    localStorage.setItem(
      'usuario',
      JSON.stringify({
        id: data.usuario.id,
        email: data.usuario.email,
        rol: rolUsuario,
      })
    );

    console.log(
      'USUARIO GUARDADO EN LOCALSTORAGE:',
      localStorage.getItem('usuario')
    );

    notificarCambioSesion();

    return data;
  },

  /**
   * Iniciar sesión mediante Google.
   */
  async loginGoogle(idToken: string): Promise<AuthResponse> {
    const url = `${obtener_api_url()}/sesion/google`;

    const respuesta = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ idToken }),
    });

    const data = await respuesta.json().catch(() => null);

    if (!respuesta.ok) {
      throw new Error(
        data?.mensaje ||
        'No se pudo iniciar sesión con Google.'
      );
    }

    if (!data?.token || !data?.usuario) {
      throw new Error(
        'La respuesta del servidor no contiene una sesión válida.'
      );
    }

    const rolUsuario = String(
      data.usuario.rol ?? ''
    ).trim().toLowerCase();

    localStorage.setItem('token', data.token);

    if (data.refresh_token) {
      localStorage.setItem(
        'refreshToken',
        data.refresh_token
      );
    } else {
      localStorage.removeItem('refreshToken');
    }

    localStorage.setItem(
      'usuario',
      JSON.stringify({
        id: data.usuario.id,
        email: data.usuario.email,
        rol: rolUsuario,
      })
    );

    notificarCambioSesion();

    return data;
  },

  /**
   * Registrar una cuenta nueva.
   */
  async registrar(datos: RegistroRequest) {
    const url = `${obtener_api_url()}/sesion/registro`;

    const respuesta = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(datos),
    });

    const data = await respuesta.json();

    if (!respuesta.ok) {
      throw new Error(
        data?.mensaje || 'No se pudo crear la cuenta'
      );
    }

    return data;
  },

  /**
   * Cerrar sesión y limpiar los datos locales.
   */
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('usuario');
    localStorage.removeItem('ganica_perfil_usuario');

    notificarCambioSesion();
  },

  /**
   * Obtener el token almacenado.
   */
  obtenerToken(): string | null {
    return localStorage.getItem('token');
  },

  /**
   * Comprobar si hay una sesión local.
   */
  estaAutenticado(): boolean {
    const token = this.obtenerToken();
    const usuarioGuardado = localStorage.getItem('usuario');

    if (!token || !usuarioGuardado) {
      return false;
    }

    try {
      const usuario = JSON.parse(usuarioGuardado);

      return Boolean(
        usuario &&
        usuario.id &&
        usuario.email
      );
    } catch {
      return false;
    }
  },
};
