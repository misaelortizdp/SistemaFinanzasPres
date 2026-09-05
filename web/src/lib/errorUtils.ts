import { AxiosError } from "axios";

export interface ApiError {
  message: string;
  code?: string;
  field?: string;
  details?: string;
}

/**
 * Extrae y formatea mensajes de error desde respuestas de API
 */
export function obtenerMensajeError(error: unknown, fallback = "Ocurrió un error inesperado"): string {
  if (!error) return fallback;

  // Error de Axios
  if (error instanceof AxiosError) {
    const status = error.response?.status;
    const data = error.response?.data;

    // Mensajes específicos por código de estado
    if (status === 400) {
      return data?.error || data?.message || "Los datos enviados no son válidos";
    }
    if (status === 401) {
      return "Tu sesión ha expirado. Por favor, inicia sesión nuevamente";
    }
    if (status === 403) {
      return "No tienes permiso para realizar esta acción";
    }
    if (status === 404) {
      return "El recurso solicitado no existe";
    }
    if (status === 409) {
      return data?.error || "Ya existe un registro con estos datos";
    }
    if (status === 422) {
      return data?.error || "Los datos no cumplen las reglas de validación";
    }
    if (status === 500) {
      return "Error en el servidor. Por favor, intenta nuevamente";
    }
    if (status === 503) {
      return "El servicio no está disponible. Por favor, intenta más tarde";
    }

    // Mensajes de error de red
    if (error.code === "ERR_NETWORK") {
      return "No se pudo conectar al servidor. Verifica tu conexión a internet";
    }
    if (error.code === "ECONNABORTED" || error.message?.includes("timeout")) {
      return "La solicitud tardó demasiado. Por favor, intenta nuevamente";
    }

    // Mensaje del servidor si existe
    if (data?.error) return data.error;
    if (data?.message) return data.message;
  }

  // Error genérico de JavaScript
  if (error instanceof Error) {
    return error.message || fallback;
  }

  // String de error
  if (typeof error === "string") {
    return error;
  }

  return fallback;
}

/**
 * Determina si un error es recuperable (se puede reintentar)
 */
export function esErrorRecuperable(error: unknown): boolean {
  if (error instanceof AxiosError) {
    const status = error.response?.status;
    
    // Errores de red son recuperables
    if (error.code === "ERR_NETWORK" || error.code === "ECONNABORTED") {
      return true;
    }
    
    // Algunos códigos HTTP son recuperables
    if (status === 408 || status === 429 || status === 503 || status === 504) {
      return true;
    }
    
    // Errores del servidor (5xx) pueden ser temporales
    if (status && status >= 500) {
      return true;
    }
  }
  
  return false;
}

/**
 * Obtiene sugerencias de acción según el tipo de error
 */
export function obtenerAccionSugerida(error: unknown): string | null {
  if (error instanceof AxiosError) {
    const status = error.response?.status;
    
    if (status === 401) {
      return "Inicia sesión nuevamente";
    }
    if (status === 403) {
      return "Contacta al administrador si crees que deberías tener acceso";
    }
    if (status === 404) {
      return "Verifica que el registro aún existe";
    }
    if (status === 409) {
      return "Verifica los datos e intenta con valores diferentes";
    }
    if (error.code === "ERR_NETWORK") {
      return "Verifica tu conexión a internet e intenta nuevamente";
    }
    if (error.code === "ECONNABORTED") {
      return "Intenta nuevamente en unos momentos";
    }
    if (status && status >= 500) {
      return "Intenta nuevamente en unos minutos";
    }
  }
  
  return null;
}

/**
 * Validaciones comunes para formularios
 */
export const validaciones = {
  requerido: (valor: string | number | null | undefined, campo = "campo"): string | null => {
    if (valor === null || valor === undefined || String(valor).trim() === "") {
      return `El ${campo} es obligatorio`;
    }
    return null;
  },
  
  montoPositivo: (monto: string | number, campo = "monto"): string | null => {
    const valor = typeof monto === "string" ? parseFloat(monto) : monto;
    if (isNaN(valor) || valor <= 0) {
      return `El ${campo} debe ser mayor a cero`;
    }
    return null;
  },
  
  montoValido: (monto: string | number, campo = "monto"): string | null => {
    const valor = typeof monto === "string" ? parseFloat(monto) : monto;
    if (isNaN(valor)) {
      return `El ${campo} debe ser un número válido`;
    }
    return null;
  },
  
  longitudMinima: (valor: string, min: number, campo = "campo"): string | null => {
    if (valor.trim().length < min) {
      return `El ${campo} debe tener al menos ${min} caracteres`;
    }
    return null;
  },
  
  longitudMaxima: (valor: string, max: number, campo = "campo"): string | null => {
    if (valor.trim().length > max) {
      return `El ${campo} no debe exceder ${max} caracteres`;
    }
    return null;
  },
  
  email: (email: string): string | null => {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!regex.test(email)) {
      return "Ingresa un correo electrónico válido";
    }
    return null;
  },
  
  fechaValida: (fecha: string, campo = "fecha"): string | null => {
    const fechaObj = new Date(fecha);
    if (isNaN(fechaObj.getTime())) {
      return `La ${campo} no es válida`;
    }
    return null;
  },
  
  fechaNoFutura: (fecha: string, campo = "fecha"): string | null => {
    const fechaObj = new Date(fecha);
    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);
    if (fechaObj > hoy) {
      return `La ${campo} no puede ser futura`;
    }
    return null;
  },
};
