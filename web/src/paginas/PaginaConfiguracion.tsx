import { FormEvent, useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Save } from "lucide-react";
import { api } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface Config {
  diezmoPct: number;
  metaNecesidadesPct: number; metaDeseosPct: number; metaAhorroPct: number;
  fondoEmergenciaMeses: number;
  metaAhorroMinimoPct: number; metaAhorroOptimoPct: number;
  categoriaDiezmoId?: number | null;
  snapshotAutomatico: boolean; snapshotDia: number;
}

const VACIO: Config = {
  diezmoPct: 0.10,
  metaNecesidadesPct: 0.50, metaDeseosPct: 0.30, metaAhorroPct: 0.20,
  fondoEmergenciaMeses: 4,
  metaAhorroMinimoPct: 0.20, metaAhorroOptimoPct: 0.30,
  snapshotAutomatico: true, snapshotDia: 1,
};

export default function PaginaConfiguracion() {
  const cliente = useQueryClient();
  const { data } = useQuery({
    queryKey: ["configuracion"],
    queryFn: async () => (await api.get<Config>("/api/configuracion")).data,
  });

  const [cfg, setCfg] = useState<Config>(VACIO);
  useEffect(() => { if (data) setCfg(data); }, [data]);

  const guardar = useMutation({
    mutationFn: async () => api.put("/api/configuracion", cfg),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["configuracion"] });
      alert("Configuración guardada ✓");
    },
  });

  function enviar(e: FormEvent) { e.preventDefault(); guardar.mutate(); }

  const sumaPct = Math.round((cfg.metaNecesidadesPct + cfg.metaDeseosPct + cfg.metaAhorroPct) * 100);
  const sumaOk = sumaPct === 100;

  function pct(v: number) { return (v * 100).toFixed(0); }
  function pctNum(v: string) { return Math.max(0, parseFloat(v) || 0) / 100; }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <header><h1 className="text-3xl font-bold">⚙️ Configuración</h1></header>

      <form onSubmit={enviar} className="space-y-6">
        <Card>
          <CardHeader><CardTitle>Regla 50/30/20</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="grid grid-cols-3 gap-3">
              <div className="space-y-1.5"><Label>Necesidades %</Label><Input type="number" value={pct(cfg.metaNecesidadesPct)} onChange={(e) => setCfg({ ...cfg, metaNecesidadesPct: pctNum(e.target.value) })} /></div>
              <div className="space-y-1.5"><Label>Deseos %</Label><Input type="number" value={pct(cfg.metaDeseosPct)} onChange={(e) => setCfg({ ...cfg, metaDeseosPct: pctNum(e.target.value) })} /></div>
              <div className="space-y-1.5"><Label>Ahorro %</Label><Input type="number" value={pct(cfg.metaAhorroPct)} onChange={(e) => setCfg({ ...cfg, metaAhorroPct: pctNum(e.target.value) })} /></div>
            </div>
            <p className={`text-sm font-medium ${sumaOk ? "text-emerald-600" : "text-red-600"}`}>
              Suma: {sumaPct}% {sumaOk ? "✅" : "⚠ debe ser 100%"}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Diezmo / aporte fijo</CardTitle></CardHeader>
          <CardContent>
            <div className="space-y-1.5">
              <Label>Porcentaje sobre ingresos %</Label>
              <Input type="number" value={pct(cfg.diezmoPct)} onChange={(e) => setCfg({ ...cfg, diezmoPct: pctNum(e.target.value) })} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Fondo de emergencia</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label>Meta en meses de gastos fijos</Label>
              <Input type="number" min="1" max="12" value={cfg.fondoEmergenciaMeses} onChange={(e) => setCfg({ ...cfg, fondoEmergenciaMeses: parseInt(e.target.value) || 4 })} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5"><Label>Ahorro mensual mínimo %</Label><Input type="number" value={pct(cfg.metaAhorroMinimoPct)} onChange={(e) => setCfg({ ...cfg, metaAhorroMinimoPct: pctNum(e.target.value) })} /></div>
              <div className="space-y-1.5"><Label>Ahorro mensual óptimo %</Label><Input type="number" value={pct(cfg.metaAhorroOptimoPct)} onChange={(e) => setCfg({ ...cfg, metaAhorroOptimoPct: pctNum(e.target.value) })} /></div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>💎 Snapshot patrimonial automático</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={cfg.snapshotAutomatico} onChange={(e) => setCfg({ ...cfg, snapshotAutomatico: e.target.checked })} />
              Tomar snapshot automáticamente cada mes
            </label>
            <div className="space-y-1.5">
              <Label>Día del mes (1-28)</Label>
              <Input type="number" min="1" max="28" value={cfg.snapshotDia} onChange={(e) => setCfg({ ...cfg, snapshotDia: parseInt(e.target.value) || 1 })} disabled={!cfg.snapshotAutomatico} />
            </div>
          </CardContent>
        </Card>

        <Button type="submit" disabled={guardar.isPending}>
          <Save className="w-4 h-4 mr-2" />Guardar configuración
        </Button>
      </form>
    </div>
  );
}
