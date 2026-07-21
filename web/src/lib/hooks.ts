import { useQuery } from "@tanstack/react-query";
import { api } from "./api";

export interface Categoria {
  id: number;
  nombre: string;
  tipo: 1 | 2 | 3 | 4;
  color?: string | null;
  icono?: string | null;
  orden: number;
  activa: boolean;
}

export interface Cuenta {
  id: number;
  nombre: string;
  saldo: number;
  orden: number;
  activa: boolean;
}

export function useCategorias() {
  return useQuery({
    queryKey: ["categorias"],
    queryFn: async () => (await api.get<Categoria[]>("/api/categorias")).data,
  });
}

export function useCuentas() {
  return useQuery({
    queryKey: ["cuentas"],
    queryFn: async () => (await api.get<Cuenta[]>("/api/cuentas")).data,
  });
}

export function formatoMoneda(v: number, fraccion = 0) {
  return new Intl.NumberFormat("es-CO", {
    style: "currency",
    currency: "COP",
    maximumFractionDigits: fraccion,
  }).format(v);
}

export function formatoFecha(iso: string | Date) {
  const d = typeof iso === "string" ? new Date(iso) : iso;
  return d.toLocaleDateString("es-CO", { day: "2-digit", month: "short", year: "numeric" });
}

export function mesActual() {
  const d = new Date();
  return { anio: d.getFullYear(), mes: d.getMonth() + 1 };
}

export const NOMBRES_MES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
];
