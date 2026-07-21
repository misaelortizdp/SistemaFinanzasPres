import { NavLink } from "react-router-dom";
import {
  LayoutDashboard, ArrowDownUp, PieChart, Wallet, Settings
} from "lucide-react";
import { cn } from "@/lib/utils";

const ITEMS = [
  { ruta: "/",            icono: LayoutDashboard, etiqueta: "Panel" },
  { ruta: "/movimientos", icono: ArrowDownUp,     etiqueta: "Movimientos" },
  { ruta: "/presupuesto", icono: PieChart,        etiqueta: "Presupuesto" },
  { ruta: "/cuentas",     icono: Wallet,          etiqueta: "Cuentas" },
  { ruta: "/configuracion", icono: Settings,      etiqueta: "Config" },
];

export function BottomNav() {
  return (
    <nav className="md:hidden fixed bottom-0 left-0 right-0 bg-background border-t z-50 safe-bottom">
      <div className="flex items-center justify-around h-16">
        {ITEMS.map((item) => (
          <NavLink
            key={item.ruta}
            to={item.ruta}
            end={item.ruta === "/"}
            className={({ isActive }) =>
              cn(
                "flex flex-col items-center justify-center gap-1 px-3 py-2 flex-1 transition-colors min-w-0",
                isActive
                  ? "text-primary"
                  : "text-muted-foreground"
              )
            }
          >
            {({ isActive }) => (
              <>
                <item.icono className={cn("w-5 h-5", isActive && "fill-current")} />
                <span className="text-[10px] font-medium truncate w-full text-center">
                  {item.etiqueta}
                </span>
              </>
            )}
          </NavLink>
        ))}
      </div>
    </nav>
  );
}
