import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { TrendingUp, TrendingDown, Wallet, Gem, ArrowRight } from "lucide-react";
import { api } from "@/lib/api";
import { usarAutenticacion } from "@/autenticacion/ContextoAutenticacion";
import { formatoMoneda, mesActual, NOMBRES_MES } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface Movimiento { id: number; fecha: string; concepto: string; monto: number; nombreCategoria?: string }
interface Ingreso { id: number; monto: number }
interface PatrimonioActual { totalActivos: number; totalPasivos: number; patrimonioNeto: number }

export default function PaginaPanel() {
  const { usuario } = usarAutenticacion();
  const { anio, mes } = mesActual();

  const { data: movimientos = [] } = useQuery({
    queryKey: ["movimientos", anio, mes],
    queryFn: async () => (await api.get<Movimiento[]>(`/api/movimientos?anio=${anio}&mes=${mes}`)).data,
  });
  const { data: ingresos = [] } = useQuery({
    queryKey: ["ingresos", anio, mes],
    queryFn: async () => (await api.get<Ingreso[]>(`/api/ingresos?anio=${anio}&mes=${mes}`)).data,
  });
  const { data: patrimonio } = useQuery({
    queryKey: ["patrimonio-actual"],
    queryFn: async () => (await api.get<PatrimonioActual>("/api/patrimonio/actual")).data,
  });

  const totalIngresos = ingresos.reduce((s, i) => s + i.monto, 0);
  const totalGastos = movimientos.reduce((s, m) => s + m.monto, 0);
  const balance = totalIngresos - totalGastos;

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <header>
        <h1 className="text-3xl font-bold">Hola, {usuario?.nombre} 👋</h1>
        <p className="text-muted-foreground">{NOMBRES_MES[mes - 1]} {anio}</p>
      </header>

      {/* Tarjetas resumen */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <TarjetaResumen icono={<TrendingUp className="w-5 h-5 text-emerald-600" />} etiqueta="Ingresos" valor={formatoMoneda(totalIngresos)} colorValor="text-emerald-600" />
        <TarjetaResumen icono={<TrendingDown className="w-5 h-5 text-red-600" />} etiqueta="Gastos" valor={formatoMoneda(totalGastos)} colorValor="text-red-600" />
        <TarjetaResumen
          icono={<Wallet className="w-5 h-5" />}
          etiqueta="Balance"
          valor={formatoMoneda(balance)}
          colorValor={balance >= 0 ? "text-emerald-600" : "text-red-600"}
        />
        <TarjetaResumen
          icono={<Gem className="w-5 h-5 text-teal-600" />}
          etiqueta="Patrimonio neto"
          valor={patrimonio ? formatoMoneda(patrimonio.patrimonioNeto) : "—"}
          colorValor={(patrimonio?.patrimonioNeto ?? 0) >= 0 ? "text-emerald-600" : "text-red-600"}
        />
      </div>

      {/* Movimientos recientes */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <CardTitle className="text-base">Últimos movimientos</CardTitle>
          <Link to="/movimientos" className="text-sm text-primary hover:underline inline-flex items-center gap-1">
            Ver todos <ArrowRight className="w-3 h-3" />
          </Link>
        </CardHeader>
        <CardContent>
          {movimientos.length === 0 ? (
            <p className="text-sm text-muted-foreground">Aún no hay movimientos este mes.</p>
          ) : (
            <ul className="divide-y">
              {movimientos.slice(0, 8).map((m) => (
                <li key={m.id} className="py-2.5 flex items-center justify-between">
                  <div>
                    <p className="font-medium text-sm">{m.concepto}</p>
                    <p className="text-xs text-muted-foreground">{m.nombreCategoria} · {new Date(m.fecha).toLocaleDateString("es-MX", { day: "2-digit", month: "short" })}</p>
                  </div>
                  <span className="font-semibold text-red-600">{formatoMoneda(m.monto)}</span>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function TarjetaResumen({ icono, etiqueta, valor, colorValor }: { icono: React.ReactNode; etiqueta: string; valor: string; colorValor?: string }) {
  return (
    <Card>
      <CardContent className="pt-5">
        <div className="flex items-center gap-2 text-muted-foreground text-xs">
          {icono} {etiqueta}
        </div>
        <p className={`text-xl font-bold mt-1 ${colorValor ?? ""}`}>{valor}</p>
      </CardContent>
    </Card>
  );
}
