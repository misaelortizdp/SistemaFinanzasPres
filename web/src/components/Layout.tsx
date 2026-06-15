import { ReactNode, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import {
  LayoutDashboard, Wallet, ArrowDownUp, TrendingUp, PieChart,
  CreditCard, Target, Gem, Settings, LogOut, Menu, X, Tags,
} from "lucide-react";
import { usarAutenticacion } from "@/autenticacion/ContextoAutenticacion";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface ItemNav {
  ruta: string;
  etiqueta: string;
  Icono: typeof LayoutDashboard;
}

const ITEMS: ItemNav[] = [
  { ruta: "/",              etiqueta: "Panel",         Icono: LayoutDashboard },
  { ruta: "/movimientos",   etiqueta: "Movimientos",   Icono: ArrowDownUp },
  { ruta: "/ingresos",      etiqueta: "Ingresos",      Icono: TrendingUp },
  { ruta: "/presupuesto",   etiqueta: "Presupuesto",   Icono: PieChart },
  { ruta: "/cuentas",       etiqueta: "Cuentas",       Icono: Wallet },
  { ruta: "/deudas",        etiqueta: "Deudas",        Icono: CreditCard },
  { ruta: "/metas",         etiqueta: "Metas",         Icono: Target },
  { ruta: "/patrimonio",    etiqueta: "Patrimonio",    Icono: Gem },
  { ruta: "/categorias",    etiqueta: "Categorías",    Icono: Tags },
  { ruta: "/configuracion", etiqueta: "Configuración", Icono: Settings },
];

export function Layout({ children }: { children: ReactNode }) {
  const { usuario, cerrarSesion } = usarAutenticacion();
  const navegar = useNavigate();
  const [abierto, setAbierto] = useState(false);

  function salir() {
    cerrarSesion();
    navegar("/login", { replace: true });
  }

  return (
    <div className="min-h-screen flex bg-muted/30">
      {/* Botón menú móvil */}
      <button
        onClick={() => setAbierto(!abierto)}
        className="md:hidden fixed top-3 left-3 z-50 p-2 rounded-md bg-background border shadow"
      >
        {abierto ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
      </button>

      {/* Sidebar */}
      <aside
        className={cn(
          "fixed md:sticky top-0 left-0 h-screen w-64 bg-background border-r flex flex-col z-40 transition-transform",
          abierto ? "translate-x-0" : "-translate-x-full md:translate-x-0"
        )}
      >
        <div className="p-5 border-b">
          <h1 className="text-lg font-bold">💰 Finanzas</h1>
          <p className="text-xs text-muted-foreground mt-1 truncate">{usuario?.email}</p>
        </div>

        <nav className="flex-1 overflow-y-auto p-2 space-y-0.5">
          {ITEMS.map((it) => (
            <NavLink
              key={it.ruta}
              to={it.ruta}
              end={it.ruta === "/"}
              onClick={() => setAbierto(false)}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors",
                  isActive
                    ? "bg-primary text-primary-foreground"
                    : "hover:bg-accent hover:text-accent-foreground"
                )
              }
            >
              <it.Icono className="w-4 h-4" />
              {it.etiqueta}
            </NavLink>
          ))}
        </nav>

        <div className="p-3 border-t">
          <Button onClick={salir} variant="ghost" className="w-full justify-start">
            <LogOut className="w-4 h-4 mr-2" />
            Cerrar sesión
          </Button>
        </div>
      </aside>

      {/* Contenido */}
      <main className="flex-1 min-h-screen p-4 md:p-8 pt-14 md:pt-8">
        {children}
      </main>
    </div>
  );
}
