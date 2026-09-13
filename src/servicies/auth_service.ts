
import { obtener_api_url } from "@/config/debug";

export interface LoginRequest {
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

export const authService = {
  async login(credenciales: LoginRequest): Promise<AuthResponse> {
    // Usamos la función dinámica para que tome la IP de la red local
    const url = `${obtener_api_url()}/sesion/login`;
    
    const respuesta = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(credenciales),
    });

    if (!respuesta.ok) {
      throw new Error("Credenciales inválidas o error en el servidor");
    }

    const data: AuthResponse = await respuesta.json();
    
    if (data && data.token) {
      localStorage.setItem("token", data.token);
      localStorage.setItem("refreshToken", data.refresh_token);
      
      // Guardamos los datos mapeando correctamente el objeto usuario que viene del backend
      const rolUsuario = data.usuario?.rol || "Sin rol asignado";
      localStorage.setItem("usuario", JSON.stringify({ 
        email: data.usuario.email, 
        rol: rolUsuario 
      }));
    }
    
    return data;
  },

  logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("usuario");
    localStorage.removeItem("ganica_perfil_usuario");
  },

  obtenerToken(): string | null {
    return localStorage.getItem("token");
  },

  estaAutenticado(): boolean {
    return !!this.obtenerToken();
  }
};