import axios from "axios";

const baseURL = import.meta.env.VITE_API_URL ?? "http://localhost:5050";

export const api = axios.create({ baseURL });

const LLAVE_TOKEN = "sfp_token";

export function obtenerToken(): string | null {
  return localStorage.getItem(LLAVE_TOKEN);
}

export function guardarToken(token: string) {
  localStorage.setItem(LLAVE_TOKEN, token);
}

export function eliminarToken() {
  localStorage.removeItem(LLAVE_TOKEN);
}

api.interceptors.request.use((config) => {
  const token = obtenerToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error?.response?.status === 401) {
      eliminarToken();
      if (window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  }
);
