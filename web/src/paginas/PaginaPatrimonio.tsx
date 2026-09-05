import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Camera, Trash2 } from "lucide-react";
import { api } from "@/lib/api";
import { formatoMoneda, formatoFecha } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useConfirm } from "@/lib/useConfirm";

interface Linea { nombre: string; monto: number; }
interface PatrimonioActual {
  activos: Linea[]; pasivos: Linea[];
  totalActivos: number; totalPasivos: number; patrimonioNeto: number;
}
interface Snapshot {
  id: number; fecha: string; activos: number; pasivos: number;
  patrimonioNeto: number; notas?: string | null;
}

export default function PaginaPatrimonio() {
  const cliente = useQueryClient();
  const { confirm, ConfirmDialog } = useConfirm();
  const { data: actual } = useQuery({
    queryKey: ["patrimonio-actual"],
    queryFn: async () => (await api.get<PatrimonioActual>("/api/patrimonio/actual")).data,
  });
  const { data: snapshots = [] } = useQuery({
    queryKey: ["patrimonio-snapshots"],
    queryFn: async () => (await api.get<Snapshot[]>("/api/patrimonio/snapshots")).data,
  });

  const tomarSnapshot = useMutation({
    mutationFn: async (notas: string | null) => api.post("/api/patrimonio/snapshots", { notas }),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["patrimonio-snapshots"] }),
  });
  const eliminar = useMutation({
    mutationFn: async (id: number) => api.delete(`/api/patrimonio/snapshots/${id}`),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["patrimonio-snapshots"] }),
  });

  function nuevoSnapshot() {
    const notas = prompt("Nota opcional para el snapshot (ej: 'corte mensual')", "");
    if (notas === null) return;
    tomarSnapshot.mutate(notas.trim() || null);
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <ConfirmDialog />
      <header>
        <h1 className="text-3xl font-bold">💎 Patrimonio</h1>
      </header>

      {/* Patrimonio neto */}
      <Card>
        <CardContent className="pt-5">
          <p className="text-sm text-muted-foreground">Patrimonio neto</p>
          <p className={`text-4xl font-bold mt-1 ${(actual?.patrimonioNeto ?? 0) >= 0 ? "text-emerald-600" : "text-red-600"}`}>
            {actual ? formatoMoneda(actual.patrimonioNeto) : "—"}
          </p>
          <div className="grid grid-cols-2 gap-4 mt-4 pt-4 border-t">
            <div>
              <p className="text-xs text-muted-foreground">🟢 Activos</p>
              <p className="text-xl font-bold text-emerald-600">{actual ? formatoMoneda(actual.totalActivos) : "—"}</p>
            </div>
            <div>
              <p className="text-xs text-muted-foreground">🔴 Pasivos</p>
              <p className="text-xl font-bold text-red-600">{actual ? formatoMoneda(actual.totalPasivos) : "—"}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Button onClick={nuevoSnapshot} disabled={tomarSnapshot.isPending}>
        <Camera className="w-4 h-4 mr-2" />Tomar snapshot ahora
      </Button>

      {/* Desglose */}
      <div className="grid md:grid-cols-2 gap-4">
        <Card>
          <CardHeader><CardTitle className="text-base">🟢 Activos</CardTitle></CardHeader>
          <CardContent>
            {!actual || actual.activos.length === 0 ?
              <p className="text-muted-foreground text-sm">Sin cuentas activas.</p> :
              <ul className="divide-y text-sm">
                {actual.activos.map((a, i) => (
                  <li key={i} className="py-2 flex justify-between">
                    <span>{a.nombre}</span>
                    <span className="font-semibold text-emerald-600">{formatoMoneda(a.monto)}</span>
                  </li>
                ))}
              </ul>}
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle className="text-base">🔴 Pasivos</CardTitle></CardHeader>
          <CardContent>
            {!actual || actual.pasivos.length === 0 ?
              <p className="text-muted-foreground text-sm">🎉 Sin deudas.</p> :
              <ul className="divide-y text-sm">
                {actual.pasivos.map((p, i) => (
                  <li key={i} className="py-2 flex justify-between">
                    <span>{p.nombre}</span>
                    <span className="font-semibold text-red-600">{formatoMoneda(p.monto)}</span>
                  </li>
                ))}
              </ul>}
          </CardContent>
        </Card>
      </div>

      {/* Historial */}
      <Card>
        <CardHeader><CardTitle>📅 Historial de snapshots ({snapshots.length})</CardTitle></CardHeader>
        <CardContent>
          {snapshots.length === 0 ?
            <p className="text-muted-foreground text-sm">Sin snapshots aún. Toma uno para empezar a medir tu evolución.</p> :
            <ul className="divide-y">
              {snapshots.map((s, i) => {
                const ant = snapshots[i + 1];
                const delta = ant ? s.patrimonioNeto - ant.patrimonioNeto : null;
                return (
                  <li key={s.id} className="py-3 flex items-start justify-between">
                    <div>
                      <p className="font-medium text-sm">{formatoFecha(s.fecha)}</p>
                      <p className="text-xs text-muted-foreground">
                        Activos {formatoMoneda(s.activos)} · Pasivos {formatoMoneda(s.pasivos)}
                      </p>
                      {s.notas && <p className="text-xs text-muted-foreground italic mt-0.5">{s.notas}</p>}
                    </div>
                    <div className="text-right">
                      <p className="font-semibold">{formatoMoneda(s.patrimonioNeto)}</p>
                      {delta !== null && (
                        <p className={`text-xs ${delta >= 0 ? "text-emerald-600" : "text-red-600"}`}>
                          {delta >= 0 ? "▲ +" : "▼ "}{formatoMoneda(delta)}
                        </p>
                      )}
                      <Button size="icon" variant="ghost" className="h-6 w-6 mt-1"
                        onClick={async () => {
                          const confirmado = await confirm({
                            title: "¿Eliminar snapshot?",
                            description: `¿Estás seguro de que deseas eliminar el snapshot del ${formatoFecha(s.fecha)}? Esta acción no se puede deshacer.`,
                            confirmText: "Eliminar",
                            cancelText: "Cancelar",
                            variant: "destructive"
                          });
                          if (confirmado) eliminar.mutate(s.id);
                        }}>
                        <Trash2 className="w-3 h-3 text-destructive" />
                      </Button>
                    </div>
                  </li>
                );
              })}
            </ul>}
        </CardContent>
      </Card>
    </div>
  );
}
