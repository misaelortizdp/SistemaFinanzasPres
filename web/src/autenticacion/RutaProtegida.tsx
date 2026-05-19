import { Navigate, useLocation } from "react-router-dom";
import { usarAutenticacion } from "./ContextoAutenticacion";

export function RutaProtegida({ children }: { children: React.ReactNode }) {
  const { usuario, cargando } = usarAutenticacion();
  const ubicacion = useLocation();

  if (cargando) {
    return <div className="p-8 text-muted-foreground">Cargando…</div>;
  }
  if (!usuario) {
    return <Navigate to="/login" state={{ from: ubicacion }} replace />;
  }
  return <>{children}</>;
}
