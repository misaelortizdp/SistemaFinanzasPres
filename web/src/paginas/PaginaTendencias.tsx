import { useQuery } from "@tanstack/react-query";
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from "recharts";
import { api } from "@/lib/api";
import { formatoMoneda, NOMBRES_MES } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface TendenciaMesDetalle {
  anio: number; mes: number;
  ingresos: number; gastos: number;
  ingresoDisponible: number; ahorro: number;
  tasaAhorroPct: number; flecha: string;
}

interface TendenciaDetalle {
  meses: TendenciaMesDetalle[];
  mejorMes?: string | null;
  peorMes?: string | null;
  promedioTasaAhorro: number;
  promedioGastos: number;
}

function compacto(v: number) {
  if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`;
  if (v >= 1_000) return `${(v / 1_000).toFixed(0)}k`;
  return String(v);
}

function etiquetaMes(anioMes: string | null | undefined): string {
  if (!anioMes) return "—";
  const [a, m] = anioMes.split("-");
  return `${NOMBRES_MES[parseInt(m) - 1]} ${a}`;
}

export default function PaginaTendencias() {
  const { data, isLoading } = useQuery<TendenciaDetalle>({
    queryKey: ["panel-tendencias"],
    queryFn: async () => (await api.get("/api/panel/tendencias")).data,
  });

  const datoGrafica = data?.meses.map(m => ({
    nombre: NOMBRES_MES[m.mes - 1].slice(0, 3),
    Ingresos: m.ingresos,
    Gastos: m.gastos,
    Ahorro: m.ahorro,
    "Tasa %": m.tasaAhorroPct,
  })) ?? [];

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <header>
        <h1 className="text-3xl font-bold">📈 Tendencias — últimos 6 meses</h1>
      </header>

      {isLoading ? (
        <p className="text-muted-foreground">Cargando…</p>
      ) : (
        <>
          {/* Resumen global */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <Card>
              <CardContent className="pt-5">
                <p className="text-xs text-muted-foreground">Promedio tasa ahorro</p>
                <p className={`text-2xl font-bold ${(data?.promedioTasaAhorro ?? 0) >= 0 ? "text-emerald-600" : "text-red-600"}`}>
                  {data?.promedioTasaAhorro.toFixed(1) ?? "—"}%
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-5">
                <p className="text-xs text-muted-foreground">Promedio gasto mensual</p>
                <p className="text-2xl font-bold">{data ? formatoMoneda(data.promedioGastos) : "—"}</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-5">
                <p className="text-xs text-muted-foreground">✅ Mejor mes</p>
                <p className="text-lg font-bold text-emerald-600">{etiquetaMes(data?.mejorMes)}</p>
                <p className="text-xs text-muted-foreground">mayor tasa de ahorro</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-5">
                <p className="text-xs text-muted-foreground">⚠ Mes más difícil</p>
                <p className="text-lg font-bold text-red-600">{etiquetaMes(data?.peorMes)}</p>
                <p className="text-xs text-muted-foreground">menor tasa de ahorro</p>
              </CardContent>
            </Card>
          </div>

          {/* Gráfica de líneas */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-base">Ingresos, Gastos y Ahorro</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={260}>
                <LineChart data={datoGrafica}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                  <XAxis dataKey="nombre" tick={{ fontSize: 12 }} />
                  <YAxis tickFormatter={compacto} tick={{ fontSize: 12 }} width={48} />
                  <Tooltip formatter={(v) => formatoMoneda(Number(v ?? 0))} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                  <Legend iconType="circle" iconSize={8} wrapperStyle={{ fontSize: 12 }} />
                  <Line type="monotone" dataKey="Ingresos" stroke="#10b981" strokeWidth={2} dot={{ r: 3 }} />
                  <Line type="monotone" dataKey="Gastos" stroke="#f43f5e" strokeWidth={2} dot={{ r: 3 }} />
                  <Line type="monotone" dataKey="Ahorro" stroke="#6366f1" strokeWidth={2} strokeDasharray="4 2" dot={{ r: 3 }} />
                </LineChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Tabla detalle */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-base">Detalle por mes</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b text-muted-foreground text-xs">
                      <th className="text-left pb-2">Mes</th>
                      <th className="text-right pb-2">Ingresos</th>
                      <th className="text-right pb-2">Gastos</th>
                      <th className="text-right pb-2">Disponible</th>
                      <th className="text-right pb-2">Ahorro</th>
                      <th className="text-right pb-2">Tasa</th>
                      <th className="text-center pb-2">Tendencia</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data?.meses.map((m, i) => {
                      const tasaColor = m.tasaAhorroPct >= 20 ? "text-emerald-600" :
                                        m.tasaAhorroPct >= 10 ? "text-amber-600" : "text-red-600";
                      const flechaColor = m.flecha === "↓" ? "text-emerald-600" :
                                          m.flecha === "↑" ? "text-red-600" : "text-muted-foreground";
                      return (
                        <tr key={i} className="border-b last:border-0">
                          <td className="py-2.5 font-medium">{NOMBRES_MES[m.mes - 1]} {m.anio}</td>
                          <td className="py-2.5 text-right text-emerald-700 dark:text-emerald-400">{formatoMoneda(m.ingresos)}</td>
                          <td className="py-2.5 text-right text-red-600">{formatoMoneda(m.gastos)}</td>
                          <td className="py-2.5 text-right">{formatoMoneda(m.ingresoDisponible)}</td>
                          <td className={`py-2.5 text-right ${m.ahorro >= 0 ? "text-emerald-600" : "text-red-600"}`}>{formatoMoneda(m.ahorro)}</td>
                          <td className={`py-2.5 text-right font-semibold ${tasaColor}`}>{m.tasaAhorroPct.toFixed(1)}%</td>
                          <td className={`py-2.5 text-center text-lg ${flechaColor}`}>{m.flecha}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
