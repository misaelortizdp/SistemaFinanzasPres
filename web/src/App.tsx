import { Routes, Route, Navigate } from "react-router-dom";
import { RutaProtegida } from "@/autenticacion/RutaProtegida";
import PaginaInicioSesion from "@/paginas/PaginaInicioSesion";
import PaginaRegistro from "@/paginas/PaginaRegistro";
import PaginaPanel from "@/paginas/PaginaPanel";
import PaginaCategorias from "@/paginas/PaginaCategorias";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<PaginaInicioSesion />} />
      <Route path="/registro" element={<PaginaRegistro />} />
      <Route
        path="/"
        element={
          <RutaProtegida>
            <PaginaPanel />
          </RutaProtegida>
        }
      />
      <Route
        path="/categorias"
        element={
          <RutaProtegida>
            <PaginaCategorias />
          </RutaProtegida>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
