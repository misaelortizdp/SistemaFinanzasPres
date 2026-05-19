import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { api, eliminarToken, guardarToken, obtenerToken } from "@/lib/api";

export interface UsuarioActual {
  usuarioId: string;
  nombre: string;
  email: string;
}

interface ContextoAutenticacion {
  usuario: UsuarioActual | null;
  cargando: boolean;
  iniciarSesion: (email: string, contrasena: string) => Promise<void>;
  registrar: (nombre: string, email: string, contrasena: string) => Promise<void>;
  cerrarSesion: () => void;
}

const Contexto = createContext<ContextoAutenticacion | undefined>(undefined);

export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioActual | null>(null);
  const [cargando, setCargando] = useState(true);

  useEffect(() => {
    const token = obtenerToken();
    if (!token) {
      setCargando(false);
      return;
    }
    api
      .get("/api/autenticacion/yo")
      .then((r) =>
        setUsuario({ usuarioId: r.data.usuarioId, nombre: r.data.nombre, email: r.data.email })
      )
      .catch(() => eliminarToken())
      .finally(() => setCargando(false));
  }, []);

  async function iniciarSesion(email: string, contrasena: string) {
    const r = await api.post("/api/autenticacion/iniciar-sesion", { email, contrasena });
    guardarToken(r.data.token);
    setUsuario({ usuarioId: r.data.usuarioId, nombre: r.data.nombre, email: r.data.email });
  }

  async function registrar(nombre: string, email: string, contrasena: string) {
    const r = await api.post("/api/autenticacion/registro", { nombre, email, contrasena });
    guardarToken(r.data.token);
    setUsuario({ usuarioId: r.data.usuarioId, nombre: r.data.nombre, email: r.data.email });
  }

  function cerrarSesion() {
    eliminarToken();
    setUsuario(null);
  }

  return (
    <Contexto.Provider value={{ usuario, cargando, iniciarSesion, registrar, cerrarSesion }}>
      {children}
    </Contexto.Provider>
  );
}

export function usarAutenticacion() {
  const ctx = useContext(Contexto);
  if (!ctx) throw new Error("usarAutenticacion debe usarse dentro de ProveedorAutenticacion");
  return ctx;
}
