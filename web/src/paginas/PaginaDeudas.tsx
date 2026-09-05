import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2, Plus, DollarSign, History, X } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { formatoMoneda, formatoFecha } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useConfirm } from "@/lib/useConfirm";

interface Deuda {
  id: number; nombre: string;
  montoOriginal: number; saldoActual: number;
  tasaInteres: number; pagoMinimo: number; abonoExtra: number; diaPago: number;
  activa: boolean; notas?: string | null; categoriaId?: number | null;
  mesesParaLiquidar: number | null; prioridadAvalancha: number | null;
}

interface PagoDeuda {
  id: number; deudaId: number; fecha: string;
  monto: number; porcionInteres: number; porcionCapital: number; notas?: string | null;
}

export default function PaginaDeudas() {
  const cliente = useQueryClient();
  const { confirm, ConfirmDialog } = useConfirm();
  const { data: deudas = [], isLoading } = useQuery({
    queryKey: ["deudas"],
    queryFn: async () => (await api.get<Deuda[]>("/api/deudas")).data,
  });

  const [editando, setEditando] = useState<Deuda | null>(null);
  const [nombre, setNombre] = useState("");
  const [original, setOriginal] = useState("0");
  const [saldo, setSaldo] = useState("0");
  const [tasa, setTasa] = useState("0");
  const [pagoMin, setPagoMin] = useState("0");
  const [abonoExtra, setAbonoExtra] = useState("0");
  const [diaPago, setDiaPago] = useState("1");

  const [verPagos, setVerPagos] = useState<Deuda | null>(null);

  function limpiar() {
    setEditando(null); setNombre(""); setOriginal("0"); setSaldo("0");
    setTasa("0"); setPagoMin("0"); setAbonoExtra("0"); setDiaPago("1");
  }
  function editar(d: Deuda) {
    setEditando(d); setNombre(d.nombre); setOriginal(String(d.montoOriginal));
    setSaldo(String(d.saldoActual)); setTasa(String(d.tasaInteres));
    setPagoMin(String(d.pagoMinimo)); setAbonoExtra(String(d.abonoExtra));
    setDiaPago(String(d.diaPago));
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const cuerpo = {
        nombre, montoOriginal: parseFloat(original) || 0,
        saldoActual: parseFloat(saldo) || 0,
        tasaInteres: parseFloat(tasa) || 0,
        pagoMinimo: parseFloat(pagoMin) || 0,
        abonoExtra: parseFloat(abonoExtra) || 0,
        diaPago: parseInt(diaPago) || 1, activa: true,
      };
      if (editando) await api.put(`/api/deudas/${editando.id}`, { id: editando.id, ...cuerpo });
      else await api.post("/api/deudas", cuerpo);
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["deudas"] });
      cliente.invalidateQueries({ queryKey: ["patrimonio-actual"] });
      cliente.invalidateQueries({ queryKey: ["presupuesto"] });
      cliente.invalidateQueries({ queryKey: ["categorias"] });
      limpiar();
    },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/deudas/${id}`); },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["deudas"] });
      cliente.invalidateQueries({ queryKey: ["presupuesto"] });
      cliente.invalidateQueries({ queryKey: ["categorias"] });
    },
  });

  const pagar = useMutation({
    mutationFn: async ({ id, monto }: { id: number; monto: number }) =>
      api.post(`/api/deudas/${id}/pagos`, { fecha: new Date().toISOString().slice(0, 10), monto, porcionInteres: 0, porcionCapital: 0, notas: null }),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["deudas"] });
      cliente.invalidateQueries({ queryKey: ["patrimonio-actual"] });
      if (verPagos) cliente.invalidateQueries({ queryKey: ["pagos-deuda", verPagos.id] });
    },
  });

  const { data: pagos = [] } = useQuery<PagoDeuda[]>({
    queryKey: ["pagos-deuda", verPagos?.id],
    queryFn: async () => (await api.get<PagoDeuda[]>(`/api/deudas/${verPagos!.id}/pagos`)).data,
    enabled: !!verPagos,
  });

  function enviar(e: FormEvent) {
    e.preventDefault();
    if (!nombre.trim()) return;
    guardar.mutate();
  }

  function pagarRapido(d: Deuda) {
    const v = prompt(`Monto del pago a "${d.nombre}":`, String(d.pagoMinimo || ""));
    if (!v) return;
    const monto = parseFloat(v);
    if (!monto || monto <= 0) {
      toast.error("El monto ingresado no es válido");
      return;
    }
    pagar.mutate({ id: d.id, monto });
  }

  const activas = deudas.filter(d => d.activa);
  const totalDeuda = activas.reduce((s, d) => s + d.saldoActual, 0);
  const totalPagoMin = activas.reduce((s, d) => s + d.pagoMinimo, 0);
  const tasaPonderada = activas.length > 0 && totalDeuda > 0
    ? activas.reduce((s, d) => s + d.tasaInteres * d.saldoActual, 0) / totalDeuda
    : 0;
  const mesesComputables = activas.map(d => d.mesesParaLiquidar).filter((m): m is number => m != null);
  const mesesLibertad = mesesComputables.length > 0 ? Math.max(...mesesComputables) : null;

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <ConfirmDialog />
      <header>
        <h1 className="text-3xl font-bold">💳 Deudas</h1>
      </header>

      {/* Resumen */}
      {activas.length > 0 && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Card><CardContent className="pt-4"><p className="text-xs text-muted-foreground">Deuda total</p><p className="text-lg font-bold text-red-600">{formatoMoneda(totalDeuda)}</p></CardContent></Card>
          <Card><CardContent className="pt-4"><p className="text-xs text-muted-foreground">Pago mínimo total</p><p className="text-lg font-bold">{formatoMoneda(totalPagoMin)}/mes</p></CardContent></Card>
          <Card><CardContent className="pt-4"><p className="text-xs text-muted-foreground">Tasa promedio ponderada</p><p className="text-lg font-bold">{tasaPonderada.toFixed(1)}%</p></CardContent></Card>
          <Card><CardContent className="pt-4"><p className="text-xs text-muted-foreground">Libre de deudas en</p><p className="text-lg font-bold">{mesesLibertad != null ? `${mesesLibertad} meses` : "—"}</p></CardContent></Card>
        </div>
      )}

      {/* Formulario */}
      <Card>
        <CardHeader><CardTitle>{editando ? `Editar: ${editando.nombre}` : "Nueva deuda"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid sm:grid-cols-2 gap-3">
              <div className="space-y-1.5"><Label>Nombre</Label><Input required value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Tarjeta XX, Préstamo personal..." /></div>
              <div className="space-y-1.5"><Label>Monto original</Label><Input type="number" step="0.01" required value={original} onChange={(e) => setOriginal(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Saldo actual</Label><Input type="number" step="0.01" value={saldo} onChange={(e) => setSaldo(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Tasa interés (% anual)</Label><Input type="number" step="0.01" value={tasa} onChange={(e) => setTasa(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Pago mínimo</Label><Input type="number" step="0.01" value={pagoMin} onChange={(e) => setPagoMin(e.target.value)} /></div>
              <div className="space-y-1.5">
                <Label>Abono extra</Label>
                <Input type="number" step="0.01" value={abonoExtra} onChange={(e) => setAbonoExtra(e.target.value)} />
              </div>
              <div className="space-y-1.5"><Label>Día de pago</Label><Input type="number" min="1" max="28" value={diaPago} onChange={(e) => setDiaPago(e.target.value)} /></div>
            </div>
            <p className="text-xs text-muted-foreground -mt-1">
              💡 Pago mínimo + abono extra es lo que se presupuesta cada mes para esta deuda — se refleja solo en Presupuesto, no hace falta cargarlo ahí a mano.
            </p>
            <div className="flex gap-2">
              <Button type="submit" disabled={guardar.isPending}>
                <Plus className="w-4 h-4 mr-2" />{editando ? "Guardar" : "Agregar"}
              </Button>
              {editando && <Button type="button" variant="outline" onClick={limpiar}>Cancelar</Button>}
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Lista de deudas */}
      <Card>
        <CardHeader><CardTitle>Tus deudas ({deudas.length})</CardTitle></CardHeader>
        <CardContent>
          {isLoading ? <p className="text-muted-foreground">Cargando…</p> :
           deudas.length === 0 ? <p className="text-muted-foreground">🎉 Sin deudas registradas.</p> :
           <ul className="divide-y">
             {deudas.map(d => {
               const pct = d.montoOriginal > 0 ? ((d.montoOriginal - d.saldoActual) / d.montoOriginal) * 100 : 0;
               return (
                 <li key={d.id} className="py-3">
                   <div className="flex items-start justify-between gap-2 mb-2">
                     <div>
                       <p className="font-medium">{d.nombre} {!d.activa && <span className="text-xs bg-emerald-100 text-emerald-800 px-1.5 py-0.5 rounded">Pagada</span>}</p>
                       <p className="text-xs text-muted-foreground">
                         {formatoMoneda(d.saldoActual)} de {formatoMoneda(d.montoOriginal)} · {d.tasaInteres}% anual · día {d.diaPago}
                       </p>
                       {d.activa && (
                         <p className="text-xs text-muted-foreground">
                           📋 Presupuesto mensual: <span className="font-medium text-foreground">{formatoMoneda(d.pagoMinimo + d.abonoExtra)}</span>
                           {d.abonoExtra > 0 && ` (${formatoMoneda(d.pagoMinimo)} mínimo + ${formatoMoneda(d.abonoExtra)} extra)`}
                         </p>
                       )}
                       {d.activa && d.prioridadAvalancha != null && (
                         <p className={d.mesesParaLiquidar != null ? "text-xs text-muted-foreground" : "text-xs text-amber-600"}>
                           🎯 Prioridad #{d.prioridadAvalancha}
                           {d.mesesParaLiquidar != null
                             ? ` · ${d.mesesParaLiquidar} meses para liquidar`
                             : " · con el pago actual no baja — sube el pago mínimo o el abono extra"}
                         </p>
                       )}
                     </div>
                     <div className="flex gap-1 flex-wrap justify-end">
                       <Button size="sm" variant="outline" onClick={() => pagarRapido(d)} disabled={!d.activa}>
                         <DollarSign className="w-3.5 h-3.5 mr-1" />Pagar
                       </Button>
                       <Button size="icon" variant="ghost" title="Historial de pagos" onClick={() => setVerPagos(verPagos?.id === d.id ? null : d)}>
                         <History className="w-4 h-4" />
                       </Button>
                       <Button size="icon" variant="ghost" onClick={() => editar(d)}><Pencil className="w-4 h-4" /></Button>
                       <Button size="icon" variant="ghost" onClick={async () => {
                         const confirmado = await confirm({
                           title: "¿Eliminar deuda?",
                           description: `¿Estás seguro de que deseas eliminar "${d.nombre}"? Esta acción no se puede deshacer.`,
                           confirmText: "Eliminar",
                           cancelText: "Cancelar",
                           variant: "destructive"
                         });
                         if (confirmado) eliminar.mutate(d.id);
                       }}>
                         <Trash2 className="w-4 h-4 text-destructive" />
                       </Button>
                     </div>
                   </div>
                   <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                     <div className="h-full bg-emerald-500" style={{ width: `${pct}%` }} />
                   </div>

                   {/* Historial de pagos inline */}
                   {verPagos?.id === d.id && (
                     <div className="mt-3 border rounded-md p-3 bg-muted/30">
                       <div className="flex items-center justify-between mb-2">
                         <p className="text-sm font-medium">Historial de pagos</p>
                         <Button size="icon" variant="ghost" className="h-6 w-6" onClick={() => setVerPagos(null)}><X className="w-3.5 h-3.5" /></Button>
                       </div>
                       {pagos.length === 0 ? (
                         <p className="text-xs text-muted-foreground">Sin pagos registrados.</p>
                       ) : (
                         <ul className="divide-y divide-border text-xs">
                           {pagos.map(p => (
                             <li key={p.id} className="py-1.5 flex justify-between">
                               <span className="text-muted-foreground">{formatoFecha(p.fecha)}</span>
                               <span>Capital: {formatoMoneda(p.porcionCapital)} · Interés: {formatoMoneda(p.porcionInteres)}</span>
                               <span className="font-semibold">{formatoMoneda(p.monto)}</span>
                             </li>
                           ))}
                         </ul>
                       )}
                     </div>
                   )}
                 </li>
               );
             })}
           </ul>}
        </CardContent>
      </Card>
    </div>
  );
}
