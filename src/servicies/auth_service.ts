import { obtener_api_url } from "@/config/debug";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  email: string;
  rol: string;
}

export const authService = {
 async login(credenciales: LoginRequest): Promise<AuthResponse> {
    // Apuntamos directamente a la URL de tu API de .NET
    const url = "http://localhost:5193/api/sesion/login";
    
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
      localStorage.setItem("refreshToken", data.refreshToken);
      localStorage.setItem("usuario", JSON.stringify({ email: data.email, rol: data.rol }));
    }
    
    return data;
  },

  logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("usuario");
  },

  obtenerToken(): string | null {
    return localStorage.getItem("token");
  },

  estaAutenticado(): boolean {
    return !!this.obtenerToken();
  }
};