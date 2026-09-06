import { NavLink } from "react-router-dom";
import {
  LayoutDashboard, ArrowDownUp, PieChart, Settings
} from "lucide-react";
import { cn } from "@/lib/utils";

const ITEMS = [
  { ruta: "/",            icono: LayoutDashboard, etiqueta: "Panel" },
  { ruta: "/movimientos", icono: ArrowDownUp,     etiqueta: "Movimientos" },
  { ruta: "/presupuesto", icono: PieChart,        etiqueta: "Presupuesto" },
  { ruta: "/configuracion", icono: Settings,      etiqueta: "Config" },
];

export function BottomNav() {
  return (
    <nav className="md:hidden fixed bottom-0 left-0 right-0 bg-background border-t z-50 safe-bottom">
      <div className="flex items-center justify-around h-16 px-2">
        {ITEMS.map((item) => (
          <NavLink
            key={item.ruta}
            to={item.ruta}
            end={item.ruta === "/"}
            className={({ isActive }) =>
              cn(
                "flex flex-col items-center justify-center gap-1 px-3 py-2 flex-1 transition-all min-w-0 rounded-lg",
                "min-h-[44px] active:scale-95",
                isActive
                  ? "text-primary bg-primary/10"
                  : "text-muted-foreground hover:text-foreground hover:bg-accent/50"
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
