import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2, Plus, DollarSign } from "lucide-react";
import { api } from "@/lib/api";
import { formatoMoneda } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

interface Deuda {
  id: number; nombre: string;
  montoOriginal: number; saldoActual: number;
  tasaInteres: number; pagoMinimo: number; diaPago: number;
  activa: boolean; notas?: string | null;
}

export default function PaginaDeudas() {
  const cliente = useQueryClient();
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
  const [diaPago, setDiaPago] = useState("1");

  function limpiar() {
    setEditando(null); setNombre(""); setOriginal("0"); setSaldo("0");
    setTasa("0"); setPagoMin("0"); setDiaPago("1");
  }
  function editar(d: Deuda) {
    setEditando(d); setNombre(d.nombre); setOriginal(String(d.montoOriginal));
    setSaldo(String(d.saldoActual)); setTasa(String(d.tasaInteres));
    setPagoMin(String(d.pagoMinimo)); setDiaPago(String(d.diaPago));
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const cuerpo = {
        nombre, montoOriginal: parseFloat(original) || 0,
        saldoActual: parseFloat(saldo) || 0,
        tasaInteres: parseFloat(tasa) || 0,
        pagoMinimo: parseFloat(pagoMin) || 0,
        diaPago: parseInt(diaPago) || 1, activa: true,
      };
      if (editando) await api.put(`/api/deudas/${editando.id}`, { id: editando.id, ...cuerpo });
      else await api.post("/api/deudas", cuerpo);
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["deudas"] });
      cliente.invalidateQueries({ queryKey: ["patrimonio-actual"] });
      limpiar();
    },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/deudas/${id}`); },
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["deudas"] }),
  });

  const pagar = useMutation({
    mutationFn: async ({ id, monto }: { id: number; monto: number }) =>
      api.post(`/api/deudas/${id}/pagos`, { fecha: new Date().toISOString().slice(0, 10), monto, porcionInteres: 0, porcionCapital: 0, notas: null }),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["deudas"] });
      cliente.invalidateQueries({ queryKey: ["patrimonio-actual"] });
    },
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
    if (!monto || monto <= 0) return alert("Monto inválido");
    pagar.mutate({ id: d.id, monto });
  }

  const totalDeuda = deudas.filter(d => d.activa).reduce((s, d) => s + d.saldoActual, 0);

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <header>
        <h1 className="text-3xl font-bold">💳 Deudas</h1>
        <p className="text-muted-foreground">Deuda activa total: <span className="font-semibold text-red-600">{formatoMoneda(totalDeuda)}</span></p>
      </header>

      <Card>
        <CardHeader><CardTitle>{editando ? `Editar: ${editando.nombre}` : "Nueva deuda"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid sm:grid-cols-2 gap-3">
              <div className="space-y-1.5"><Label>Nombre</Label><Input required value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Tarjeta XX, Préstamo personal..." /></div>
              <div className="space-y-1.5"><Label>Monto original</Label><Input type="number" step="0.01" required value={original} onChange={(e) => setOriginal(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Saldo actual</Label><Input type="number" step="0.01" value={saldo} onChange={(e) => setSaldo(e.target.value)} placeholder={editando ? "" : "Si está vacío usa el monto original"} /></div>
              <div className="space-y-1.5"><Label>Tasa interés (% anual)</Label><Input type="number" step="0.01" value={tasa} onChange={(e) => setTasa(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Pago mínimo</Label><Input type="number" step="0.01" value={pagoMin} onChange={(e) => setPagoMin(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Día de pago</Label><Input type="number" min="1" max="28" value={diaPago} onChange={(e) => setDiaPago(e.target.value)} /></div>
            </div>
            <div className="flex gap-2">
              <Button type="submit" disabled={guardar.isPending}>
                <Plus className="w-4 h-4 mr-2" />{editando ? "Guardar" : "Agregar"}
              </Button>
              {editando && <Button type="button" variant="outline" onClick={limpiar}>Cancelar</Button>}
            </div>
          </form>
        </CardContent>
      </Card>

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
                         {formatoMoneda(d.saldoActual)} de {formatoMoneda(d.montoOriginal)} · {d.tasaInteres}% · día {d.diaPago}
                       </p>
                     </div>
                     <div className="flex gap-1">
                       <Button size="sm" variant="outline" onClick={() => pagarRapido(d)} disabled={!d.activa}>
                         <DollarSign className="w-3.5 h-3.5 mr-1" />Pagar
                       </Button>
                       <Button size="icon" variant="ghost" onClick={() => editar(d)}><Pencil className="w-4 h-4" /></Button>
                       <Button size="icon" variant="ghost" onClick={() => { if (confirm(`¿Eliminar "${d.nombre}"?`)) eliminar.mutate(d.id); }}>
                         <Trash2 className="w-4 h-4 text-destructive" />
                       </Button>
                     </div>
                   </div>
                   <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                     <div className="h-full bg-emerald-500" style={{ width: `${pct}%` }} />
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
