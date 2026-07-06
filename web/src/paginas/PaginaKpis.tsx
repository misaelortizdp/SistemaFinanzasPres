import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { formatoMoneda, mesActual, NOMBRES_MES } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface Kpis {
  tasaAhorroPct: number; ahorroMensual: number;
  totalIngresos: number; totalGastos: number;
  ingresoDisponible: number; diezmoMonto: number;
  pctNecesidades: number; pctDeseos: number; pctAhorro: number;
  fondoEmergenciaActual: number; fondoEmergenciaMeta: number; fondoEmergenciaMeses: number;
  totalDeudas: number; deudasActivas: number;
}

interface Config {
  metaNecesidadesPct: number; metaDeseosPct: number; metaAhorroPct: number;
}

function Kpi({ etiqueta, valor, sub, color }: { etiqueta: string; valor: string; sub?: string; color?: string }) {
  return (
    <Card>
      <CardContent className="pt-5">
        <p className="text-xs text-muted-foreground mb-1">{etiqueta}</p>
        <p className={`text-2xl font-bold ${color ?? ""}`}>{valor}</p>
        {sub && <p className="text-xs text-muted-foreground mt-0.5">{sub}</p>}
      </CardContent>
    </Card>
  );
}

function BarraMeta({ etiqueta, pctReal, pctMeta, color }: { etiqueta: string; pctReal: number; pctMeta: number; color: string }) {
  const excedido = pctReal > pctMeta;
  const pctVisual = Math.min(100, (pctReal / pctMeta) * 100);
  return (
    <div className="space-y-1">
      <div className="flex justify-between text-sm">
        <span>{etiqueta}</span>
        <span className={excedido ? "text-red-600 font-semibold" : "text-muted-foreground"}>
          {pctReal.toFixed(1)}% <span className="text-xs">(meta {pctMeta}%)</span>
        </span>
      </div>
      <div className="h-2 bg-muted rounded-full overflow-hidden">
        <div
          className={`h-full rounded-full transition-all ${excedido ? "bg-red-500" : color}`}
          style={{ width: `${pctVisual}%` }}
        />
      </div>
    </div>
  );
}

export default function PaginaKpis() {
  const { anio, mes } = mesActual();

  const { data: kpis, isLoading } = useQuery<Kpis>({
    queryKey: ["panel-kpis"],
    queryFn: async () => (await api.get("/api/panel/kpis")).data,
  });

  const { data: config } = useQuery<Config>({
    queryKey: ["configuracion"],
    queryFn: async () => (await api.get("/api/configuracion")).data,
  });

  if (isLoading) return <div className="max-w-4xl mx-auto p-6"><p className="text-muted-foreground">Cargando KPIs…</p></div>;

  const fondoPct = kpis && kpis.fondoEmergenciaMeta > 0
    ? Math.min(100, (kpis.fondoEmergenciaActual / kpis.fondoEmergenciaMeta) * 100)
    : 0;

  const metaNec = Math.round((config?.metaNecesidadesPct ?? 0.5) * 100);
  const metaDes = Math.round((config?.metaDeseosPct ?? 0.3) * 100);
  const metaAhor = Math.round((config?.metaAhorroPct ?? 0.2) * 100);

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <header>
        <h1 className="text-3xl font-bold">📊 KPIs financieros</h1>
        <p className="text-muted-foreground">{NOMBRES_MES[mes - 1]} {anio}</p>
      </header>

      {/* KPIs principales */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <Kpi
          etiqueta="Tasa de ahorro"
          valor={kpis ? `${kpis.tasaAhorroPct.toFixed(1)}%` : "—"}
          color={kpis && kpis.tasaAhorroPct >= metaAhor ? "text-emerald-600" : "text-red-600"}
          sub={`meta: ${metaAhor}%`}
        />
        <Kpi
          etiqueta="Ahorro neto del mes"
          valor={kpis ? formatoMoneda(kpis.ahorroMensual) : "—"}
          color={kpis && kpis.ahorroMensual >= 0 ? "text-emerald-600" : "text-red-600"}
        />
        <Kpi
          etiqueta="Total ingresos"
          valor={kpis ? formatoMoneda(kpis.totalIngresos) : "—"}
        />
        <Kpi
          etiqueta="Total gastos"
          valor={kpis ? formatoMoneda(kpis.totalGastos) : "—"}
          color="text-red-600"
        />
      </div>

      {/* Diezmo e ingreso disponible */}
      {kpis && kpis.diezmoMonto > 0 && (
        <div className="grid grid-cols-2 gap-3">
          <Kpi etiqueta="🙏 Diezmo" valor={formatoMoneda(kpis.diezmoMonto)} color="text-amber-600" />
          <Kpi etiqueta="Ingreso disponible" valor={formatoMoneda(kpis.ingresoDisponible)} />
        </div>
      )}

      {/* Distribución por pilares */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Distribución por pilares</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <BarraMeta etiqueta="🏠 Necesidades" pctReal={kpis?.pctNecesidades ?? 0} pctMeta={metaNec} color="bg-red-400" />
          <BarraMeta etiqueta="🎮 Deseos"      pctReal={kpis?.pctDeseos ?? 0}      pctMeta={metaDes} color="bg-purple-400" />
          <BarraMeta etiqueta="💰 Ahorro"      pctReal={kpis?.pctAhorro ?? 0}      pctMeta={metaAhor} color="bg-emerald-400" />
        </CardContent>
      </Card>

      {/* Fondo de emergencia */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">🛡 Fondo de emergencia</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex justify-between text-sm">
            <span className="text-muted-foreground">Actual en cuentas</span>
            <span className="font-semibold">{kpis ? formatoMoneda(kpis.fondoEmergenciaActual) : "—"}</span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-muted-foreground">Meta ({kpis?.fondoEmergenciaMeses ?? 4} meses de necesidades)</span>
            <span className="font-semibold">{kpis ? formatoMoneda(kpis.fondoEmergenciaMeta) : "—"}</span>
          </div>
          <div className="h-3 bg-muted rounded-full overflow-hidden">
            <div
              className={`h-full rounded-full transition-all ${fondoPct >= 100 ? "bg-emerald-500" : fondoPct >= 50 ? "bg-amber-500" : "bg-red-500"}`}
              style={{ width: `${fondoPct}%` }}
            />
          </div>
          <p className="text-xs text-muted-foreground">{fondoPct.toFixed(0)}% de la meta alcanzado</p>
        </CardContent>
      </Card>

      {/* Deudas */}
      <div className="grid grid-cols-2 gap-3">
        <Kpi
          etiqueta="💳 Deuda total activa"
          valor={kpis ? formatoMoneda(kpis.totalDeudas) : "—"}
          color={kpis && kpis.totalDeudas > 0 ? "text-red-600" : "text-emerald-600"}
          sub={`${kpis?.deudasActivas ?? 0} deudas activas`}
        />
        <Kpi
          etiqueta="Deuda vs ingresos"
          valor={kpis && kpis.totalIngresos > 0 ? `${((kpis.totalDeudas / kpis.totalIngresos) * 100).toFixed(1)}%` : "—"}
          sub="ratio deuda/ingreso mensual"
        />
      </div>
    </div>
  );
}
