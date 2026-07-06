import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
  TrendingUp, TrendingDown, Wallet, Gem, ArrowRight, Flame, CalendarClock,
} from "lucide-react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, PieChart, Pie, Cell, Legend,
} from "recharts";
import { api } from "@/lib/api";
import { usarAutenticacion } from "@/autenticacion/ContextoAutenticacion";
import { formatoMoneda, mesActual, NOMBRES_MES } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface Movimiento { id: number; fecha: string; concepto: string; monto: number; nombreCategoria?: string }
interface PatrimonioActual { totalActivos: number; totalPasivos: number; patrimonioNeto: number }
interface TendenciaMes { anio: number; mes: number; ingresos: number; gastos: number }
interface GastoCategoria { nombre: string; monto: number; color?: string; icono?: string }
interface Distribucion { necesidades: number; deseos: number; ahorro: number; totalIngresos: number; ingresoDisponible: number; diezmoMonto: number }
interface Proyeccion { diasTranscurridos: number; diasTotales: number; gastoActual: number; gastoProyectado: number; tasaQuemaDiaria: number; presupuestoDiarioPermitido: number; ingresoDisponible: number }
interface ResumenPanel {
  tendencia: TendenciaMes[]
  gastosPorCategoria: GastoCategoria[]
  distribucion: Distribucion
  proyeccion: Proyeccion
}
interface Config {
  metaNecesidadesPct: number
  metaDeseosPct: number
  metaAhorroPct: number
}

const PALETA = ["#6366f1","#f43f5e","#f59e0b","#10b981","#3b82f6","#8b5cf6","#ec4899","#14b8a6","#f97316","#84cc16"];

function compacto(v: number) {
  if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`;
  if (v >= 1_000) return `${(v / 1_000).toFixed(0)}k`;
  return String(v);
}

export default function PaginaPanel() {
  const { usuario } = usarAutenticacion();
  const { anio, mes } = mesActual();

  const { data: resumen } = useQuery<ResumenPanel>({
    queryKey: ["panel-resumen"],
    queryFn: async () => (await api.get("/api/panel/resumen")).data,
  });

  const { data: patrimonio } = useQuery<PatrimonioActual>({
    queryKey: ["patrimonio-actual"],
    queryFn: async () => (await api.get("/api/patrimonio/actual")).data,
  });

  const { data: movimientos = [] } = useQuery<Movimiento[]>({
    queryKey: ["movimientos", anio, mes],
    queryFn: async () => (await api.get(`/api/movimientos?anio=${anio}&mes=${mes}`)).data,
  });

  const { data: config } = useQuery<Config>({
    queryKey: ["configuracion"],
    queryFn: async () => (await api.get("/api/configuracion")).data,
  });

  const mesActualTendencia = resumen?.tendencia.find(t => t.anio === anio && t.mes === mes);
  const totalIngresos = mesActualTendencia?.ingresos ?? 0;
  const totalGastos = mesActualTendencia?.gastos ?? 0;
  const balance = totalIngresos - totalGastos;

  const datosBarras = resumen?.tendencia.map(t => ({
    nombre: NOMBRES_MES[t.mes - 1].slice(0, 3),
    Ingresos: t.ingresos,
    Gastos: t.gastos,
  })) ?? [];

  const datosDona = (resumen?.gastosPorCategoria ?? []).map((g, i) => ({
    name: `${g.icono ?? ""} ${g.nombre}`.trim(),
    value: g.monto,
    color: g.color ?? PALETA[i % PALETA.length],
  }));

  const dist = resumen?.distribucion;
  const proy = resumen?.proyeccion;

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <header>
        <h1 className="text-3xl font-bold">Hola, {usuario?.nombre} 👋</h1>
        <p className="text-muted-foreground">{NOMBRES_MES[mes - 1]} {anio}</p>
      </header>

      {/* KPI cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <TarjetaKpi icono={<TrendingUp className="w-5 h-5 text-emerald-600" />} etiqueta="Ingresos" valor={formatoMoneda(totalIngresos)} colorValor="text-emerald-600" />
        <TarjetaKpi icono={<TrendingDown className="w-5 h-5 text-red-600" />} etiqueta="Gastos" valor={formatoMoneda(totalGastos)} colorValor="text-red-600" />
        <TarjetaKpi
          icono={<Wallet className="w-5 h-5" />}
          etiqueta="Balance"
          valor={formatoMoneda(balance)}
          colorValor={balance >= 0 ? "text-emerald-600" : "text-red-600"}
        />
        <TarjetaKpi
          icono={<Gem className="w-5 h-5 text-teal-600" />}
          etiqueta="Patrimonio neto"
          valor={patrimonio ? formatoMoneda(patrimonio.patrimonioNeto) : "—"}
          colorValor={(patrimonio?.patrimonioNeto ?? 0) >= 0 ? "text-emerald-600" : "text-red-600"}
        />
      </div>

      {/* Proyección + diezmo */}
      {proy && proy.ingresoDisponible > 0 && (
        <div className="grid sm:grid-cols-2 gap-3">
          <Card>
            <CardContent className="pt-5 space-y-2">
              <div className="flex items-center gap-2 text-sm font-medium">
                <Flame className="w-4 h-4 text-orange-500" />
                Ritmo de gasto
              </div>
              <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                <span className="text-muted-foreground">Gasto actual</span>
                <span className="font-semibold text-right">{formatoMoneda(proy.gastoActual)}</span>
                <span className="text-muted-foreground">Proyectado al mes</span>
                <span className={`font-semibold text-right ${proy.gastoProyectado > proy.ingresoDisponible ? "text-red-600" : "text-foreground"}`}>
                  {formatoMoneda(proy.gastoProyectado)}
                </span>
                <span className="text-muted-foreground">Quema diaria</span>
                <span className="font-semibold text-right">{formatoMoneda(proy.tasaQuemaDiaria)}/día</span>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-5 space-y-2">
              <div className="flex items-center gap-2 text-sm font-medium">
                <CalendarClock className="w-4 h-4 text-blue-500" />
                Presupuesto diario
              </div>
              <div className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                <span className="text-muted-foreground">Ingreso disponible</span>
                <span className="font-semibold text-right">{formatoMoneda(proy.ingresoDisponible)}</span>
                <span className="text-muted-foreground">Permitido por día</span>
                <span className="font-semibold text-right text-emerald-600">{formatoMoneda(proy.presupuestoDiarioPermitido)}/día</span>
                <span className="text-muted-foreground">Día {proy.diasTranscurridos} de {proy.diasTotales}</span>
                <div className="flex items-center justify-end">
                  <div className="h-1.5 w-20 bg-muted rounded-full overflow-hidden">
                    <div className="h-full bg-blue-400 rounded-full" style={{ width: `${Math.round((proy.diasTranscurridos / proy.diasTotales) * 100)}%` }} />
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Gráficas principales */}
      <div className="grid md:grid-cols-2 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Ingresos vs Gastos — últimos 6 meses</CardTitle>
          </CardHeader>
          <CardContent>
            {datosBarras.every(d => d.Ingresos === 0 && d.Gastos === 0) ? (
              <p className="text-sm text-muted-foreground py-8 text-center">Sin datos suficientes aún.</p>
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={datosBarras} barCategoryGap="30%">
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                  <XAxis dataKey="nombre" tick={{ fontSize: 12 }} />
                  <YAxis tickFormatter={compacto} tick={{ fontSize: 12 }} width={45} />
                  <Tooltip
                    formatter={(v) => formatoMoneda(Number(v ?? 0))}
                    contentStyle={{ fontSize: 12, borderRadius: 8 }}
                  />
                  <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="Ingresos" fill="#10b981" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="Gastos" fill="#f43f5e" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Gastos por categoría — {NOMBRES_MES[mes - 1]}</CardTitle>
          </CardHeader>
          <CardContent>
            {datosDona.length === 0 ? (
              <p className="text-sm text-muted-foreground py-8 text-center">No hay gastos registrados este mes.</p>
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <PieChart>
                  <Pie
                    data={datosDona}
                    cx="50%"
                    cy="50%"
                    innerRadius={55}
                    outerRadius={85}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {datosDona.map((entry, i) => (
                      <Cell key={i} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip
                    formatter={(v) => formatoMoneda(Number(v ?? 0))}
                    contentStyle={{ fontSize: 12, borderRadius: 8 }}
                  />
                  <Legend
                    iconType="circle"
                    iconSize={8}
                    wrapperStyle={{ fontSize: 11 }}
                    formatter={(value) => value.length > 18 ? value.slice(0, 18) + "…" : value}
                  />
                </PieChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Diezmo banner */}
      {dist && dist.diezmoMonto > 0 && (
        <Card className="border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-950/20">
          <CardContent className="pt-4 pb-3 flex items-center justify-between flex-wrap gap-2">
            <p className="text-sm">
              🙏 <span className="font-medium">Diezmo del mes:</span>{" "}
              <span className="font-bold text-amber-700 dark:text-amber-400">{formatoMoneda(dist.diezmoMonto)}</span>
            </p>
            <p className="text-xs text-muted-foreground">
              Ingreso disponible tras diezmo: <span className="font-semibold text-foreground">{formatoMoneda(dist.ingresoDisponible)}</span>
            </p>
          </CardContent>
        </Card>
      )}

      {/* Regla 50/30/20 */}
      {dist && dist.totalIngresos > 0 && (
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">
              Distribución por pilares —&nbsp;
              <span className="font-normal text-muted-foreground text-sm">
                {Math.round((config?.metaNecesidadesPct ?? 0.5) * 100)}/
                {Math.round((config?.metaDeseosPct ?? 0.3) * 100)}/
                {Math.round((config?.metaAhorroPct ?? 0.2) * 100)}
              </span>
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-xs text-muted-foreground -mt-1">
              Ingreso disponible: <span className="font-semibold text-foreground">{formatoMoneda(dist.ingresoDisponible > 0 ? dist.ingresoDisponible : dist.totalIngresos)}</span>
            </p>
            <Barra50 etiqueta="Necesidades" icono="🏠" actual={dist.necesidades} objetivo={(dist.ingresoDisponible > 0 ? dist.ingresoDisponible : dist.totalIngresos) * (config?.metaNecesidadesPct ?? 0.5)} pct={Math.round((config?.metaNecesidadesPct ?? 0.5) * 100)} />
            <Barra50 etiqueta="Deseos"      icono="🎮" actual={dist.deseos}      objetivo={(dist.ingresoDisponible > 0 ? dist.ingresoDisponible : dist.totalIngresos) * (config?.metaDeseosPct ?? 0.3)}      pct={Math.round((config?.metaDeseosPct ?? 0.3) * 100)} />
            <Barra50 etiqueta="Ahorro"      icono="💰" actual={dist.ahorro}      objetivo={(dist.ingresoDisponible > 0 ? dist.ingresoDisponible : dist.totalIngresos) * (config?.metaAhorroPct ?? 0.2)}      pct={Math.round((config?.metaAhorroPct ?? 0.2) * 100)} />
          </CardContent>
        </Card>
      )}

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
                    <p className="text-xs text-muted-foreground">
                      {m.nombreCategoria} · {new Date(m.fecha).toLocaleDateString("es-MX", { day: "2-digit", month: "short" })}
                    </p>
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

function TarjetaKpi({ icono, etiqueta, valor, colorValor }: { icono: React.ReactNode; etiqueta: string; valor: string; colorValor?: string }) {
  return (
    <Card>
      <CardContent className="pt-5">
        <div className="flex items-center gap-2 text-muted-foreground text-xs">{icono} {etiqueta}</div>
        <p className={`text-xl font-bold mt-1 ${colorValor ?? ""}`}>{valor}</p>
      </CardContent>
    </Card>
  );
}

function Barra50({ etiqueta, icono, actual, objetivo, pct }: { etiqueta: string; icono: string; actual: number; objetivo: number; pct: number }) {
  const porcentaje = objetivo > 0 ? Math.min(100, (actual / objetivo) * 100) : 0;
  const excedido = actual > objetivo;
  const cerca = !excedido && porcentaje >= 80;

  return (
    <div className="space-y-1">
      <div className="flex justify-between text-sm">
        <span className="font-medium">{icono} {etiqueta} <span className="text-muted-foreground font-normal">(meta {pct}%)</span></span>
        <span className={excedido ? "text-red-600 font-semibold" : cerca ? "text-amber-600 font-semibold" : "text-muted-foreground"}>
          {formatoMoneda(actual)} / {formatoMoneda(objetivo)}
        </span>
      </div>
      <div className="h-2 bg-muted rounded-full overflow-hidden">
        <div
          className={`h-full transition-all rounded-full ${excedido ? "bg-red-500" : cerca ? "bg-amber-500" : "bg-emerald-500"}`}
          style={{ width: `${porcentaje}%` }}
        />
      </div>
      <p className="text-xs text-muted-foreground">
        {excedido
          ? `Excediste el límite por ${formatoMoneda(actual - objetivo)}`
          : `Disponible: ${formatoMoneda(objetivo - actual)}`}
      </p>
    </div>
  );
}
