import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2, Plus, ChevronLeft, ChevronRight } from "lucide-react";
import { api } from "@/lib/api";
import { formatoFecha, formatoMoneda, mesActual, NOMBRES_MES, useCuentas } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { useConfirm } from "@/lib/useConfirm";

interface Ingreso {
  id: number; fecha: string; concepto: string; monto: number;
  fuente: string; cuentaId?: number | null; nombreCuenta?: string | null;
  notas?: string | null; esRecurrente: boolean;
}

export default function PaginaIngresos() {
  const cliente = useQueryClient();
  const { confirm, ConfirmDialog } = useConfirm();
  const ahora = mesActual();
  const [anio, setAnio] = useState(ahora.anio);
  const [mes, setMes] = useState(ahora.mes);

  const { data: cuentas = [] } = useCuentas();
  const { data: ingresos = [], isLoading } = useQuery({
    queryKey: ["ingresos", anio, mes],
    queryFn: async () => (await api.get<Ingreso[]>(`/api/ingresos?anio=${anio}&mes=${mes}`)).data,
  });

  const [editando, setEditando] = useState<Ingreso | null>(null);
  const [fecha, setFecha] = useState(new Date().toISOString().slice(0, 10));
  const [concepto, setConcepto] = useState("");
  const [monto, setMonto] = useState("0");
  const [fuente, setFuente] = useState("Salario");
  const [cuentaId, setCuentaId] = useState<number | "">("");
  const [esRecurrente, setEsRecurrente] = useState(false);

  function limpiar() {
    setEditando(null); setFecha(new Date().toISOString().slice(0, 10));
    setConcepto(""); setMonto("0"); setFuente("Salario"); setCuentaId(""); setEsRecurrente(false);
  }
  function editar(i: Ingreso) {
    setEditando(i); setFecha(i.fecha.slice(0, 10)); setConcepto(i.concepto);
    setMonto(String(i.monto)); setFuente(i.fuente); setCuentaId(i.cuentaId ?? ""); setEsRecurrente(i.esRecurrente);
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const cuerpo = {
        fecha, concepto, monto: parseFloat(monto) || 0, fuente,
        cuentaId: cuentaId === "" ? null : cuentaId,
        esRecurrente, notas: null,
      };
      if (editando) await api.put(`/api/ingresos/${editando.id}`, { id: editando.id, ...cuerpo });
      else await api.post("/api/ingresos", cuerpo);
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["ingresos"] });
      cliente.invalidateQueries({ queryKey: ["cuentas"] });
      cliente.invalidateQueries({ queryKey: ["patrimonio-actual"] });
      limpiar();
    },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/ingresos/${id}`); },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["ingresos"] });
      cliente.invalidateQueries({ queryKey: ["cuentas"] });
    },
  });

  function enviar(e: FormEvent) { e.preventDefault(); if (!concepto.trim()) return; guardar.mutate(); }
  function cambiarMes(delta: number) {
    let m = mes + delta, a = anio;
    if (m === 0) { m = 12; a--; } if (m === 13) { m = 1; a++; }
    setMes(m); setAnio(a);
  }

  const total = ingresos.reduce((s, i) => s + i.monto, 0);

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <ConfirmDialog />
      <header className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-3xl font-bold">💰 Ingresos</h1>
        <div className="flex items-center gap-2">
          <Button size="icon" variant="outline" onClick={() => cambiarMes(-1)}><ChevronLeft className="w-4 h-4" /></Button>
          <span className="text-sm font-medium min-w-[140px] text-center">{NOMBRES_MES[mes - 1]} {anio}</span>
          <Button size="icon" variant="outline" onClick={() => cambiarMes(1)}><ChevronRight className="w-4 h-4" /></Button>
        </div>
      </header>

      <Card>
        <CardHeader><CardTitle>{editando ? "Editar ingreso" : "Nuevo ingreso"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid sm:grid-cols-3 gap-3">
              <div className="space-y-1.5"><Label>Fecha</Label><Input type="date" required value={fecha} onChange={(e) => setFecha(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Monto</Label><Input type="number" step="0.01" required value={monto} onChange={(e) => setMonto(e.target.value)} /></div>
              <div className="space-y-1.5">
                <Label>Cuenta (opcional)</Label>
                <Select value={cuentaId} onChange={(e) => setCuentaId(e.target.value === "" ? "" : Number(e.target.value))}>
                  <option value="">— Sin cuenta —</option>
                  {cuentas.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
                </Select>
              </div>
            </div>
            <div className="grid sm:grid-cols-2 gap-3">
              <div className="space-y-1.5"><Label>Concepto</Label><Input required value={concepto} onChange={(e) => setConcepto(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Fuente</Label><Input value={fuente} onChange={(e) => setFuente(e.target.value)} placeholder="Salario, Freelance, ..." /></div>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={esRecurrente} onChange={(e) => setEsRecurrente(e.target.checked)} />
              Es recurrente (mensual)
            </label>
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
        <CardHeader>
          <CardTitle>Ingresos del mes ({ingresos.length}) · Total: <span className="text-emerald-600">{formatoMoneda(total)}</span></CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? <p className="text-muted-foreground">Cargando…</p> :
           ingresos.length === 0 ? <p className="text-muted-foreground">Sin ingresos este mes.</p> :
           <ul className="divide-y">
             {ingresos.map(i => (
               <li key={i.id} className="py-3 flex items-start justify-between gap-3">
                 <div className="min-w-0 flex-1">
                   <p className="font-medium truncate">{i.concepto} {i.esRecurrente && <span className="text-xs bg-blue-100 text-blue-800 px-1.5 py-0.5 rounded ml-1">🔁</span>}</p>
                   <p className="text-xs text-muted-foreground">
                     {formatoFecha(i.fecha)} · {i.fuente}
                     {i.nombreCuenta ? ` · ${i.nombreCuenta}` : ""}
                   </p>
                 </div>
                 <div className="flex items-center gap-1">
                   <span className="font-semibold text-emerald-600">{formatoMoneda(i.monto)}</span>
                   <Button size="icon" variant="ghost" onClick={() => editar(i)}><Pencil className="w-4 h-4" /></Button>
                   <Button size="icon" variant="ghost" onClick={async () => {
                     const confirmado = await confirm({
                       title: "¿Eliminar ingreso?",
                       description: "¿Estás seguro de que deseas eliminar este ingreso? Esta acción no se puede deshacer.",
                       confirmText: "Eliminar",
                       cancelText: "Cancelar",
                       variant: "destructive"
                     });
                     if (confirmado) eliminar.mutate(i.id);
                   }}>
                     <Trash2 className="w-4 h-4 text-destructive" />
                   </Button>
                 </div>
               </li>
             ))}
           </ul>}
        </CardContent>
      </Card>
    </div>
  );
}
