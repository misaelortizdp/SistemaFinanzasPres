import { Routes, Route, Navigate } from "react-router-dom";
import { RutaProtegida } from "@/autenticacion/RutaProtegida";
import { Layout } from "@/components/Layout";
import { ErrorBoundary } from "@/components/ErrorBoundary";
import PaginaInicioSesion from "@/paginas/PaginaInicioSesion";
import PaginaRegistro from "@/paginas/PaginaRegistro";
import PaginaPanel from "@/paginas/PaginaPanel";
import PaginaCategorias from "@/paginas/PaginaCategorias";
import PaginaCuentas from "@/paginas/PaginaCuentas";
import PaginaMovimientos from "@/paginas/PaginaMovimientos";
import PaginaPresupuesto from "@/paginas/PaginaPresupuesto";
import PaginaDeudas from "@/paginas/PaginaDeudas";
import PaginaMetas from "@/paginas/PaginaMetas";
import PaginaPatrimonio from "@/paginas/PaginaPatrimonio";
import PaginaConfiguracion from "@/paginas/PaginaConfiguracion";
import PaginaTendencias from "@/paginas/PaginaTendencias";

function Protegida({ children }: { children: React.ReactNode }) {
  return (
    <RutaProtegida>
      <Layout>{children}</Layout>
    </RutaProtegida>
  );
}

export default function App() {
  return (
    <ErrorBoundary>
      <Routes>
        <Route path="/login" element={<PaginaInicioSesion />} />
        <Route path="/registro" element={<PaginaRegistro />} />

        <Route path="/"              element={<Protegida><PaginaPanel /></Protegida>} />
        <Route path="/movimientos"   element={<Protegida><PaginaMovimientos /></Protegida>} />
        <Route path="/presupuesto"   element={<Protegida><PaginaPresupuesto /></Protegida>} />
        <Route path="/cuentas"       element={<Protegida><PaginaCuentas /></Protegida>} />
        <Route path="/deudas"        element={<Protegida><PaginaDeudas /></Protegida>} />
        <Route path="/metas"         element={<Protegida><PaginaMetas /></Protegida>} />
        <Route path="/patrimonio"    element={<Protegida><PaginaPatrimonio /></Protegida>} />
        <Route path="/categorias"    element={<Protegida><PaginaCategorias /></Protegida>} />
        <Route path="/configuracion" element={<Protegida><PaginaConfiguracion /></Protegida>} />
        <Route path="/tendencias"    element={<Protegida><PaginaTendencias /></Protegida>} />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </ErrorBoundary>
  );
}
