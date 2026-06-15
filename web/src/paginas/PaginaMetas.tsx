import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2, Plus, PiggyBank } from "lucide-react";
import { api } from "@/lib/api";
import { formatoMoneda, formatoFecha } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";

interface Meta {
  id: number; nombre: string; prioridad: string;
  objetivo: number; acumulado: number;
  fechaLimite?: string | null;
  aporteMensualPlaneado: number;
  activa: boolean; notas?: string | null; orden: number;
}

const PRIORIDADES = ["URGENTE", "ALTA", "MEDIA", "BAJA", "FUTURO"];

export default function PaginaMetas() {
  const cliente = useQueryClient();
  const { data: metas = [], isLoading } = useQuery({
    queryKey: ["metas"],
    queryFn: async () => (await api.get<Meta[]>("/api/metas-ahorro")).data,
  });

  const [editando, setEditando] = useState<Meta | null>(null);
  const [nombre, setNombre] = useState("");
  const [prioridad, setPrioridad] = useState("MEDIA");
  const [objetivo, setObjetivo] = useState("0");
  const [acumulado, setAcumulado] = useState("0");
  const [aporte, setAporte] = useState("0");
  const [conFecha, setConFecha] = useState(false);
  const [fechaLimite, setFechaLimite] = useState(new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10));

  function limpiar() {
    setEditando(null); setNombre(""); setPrioridad("MEDIA");
    setObjetivo("0"); setAcumulado("0"); setAporte("0");
    setConFecha(false);
  }
  function editar(m: Meta) {
    setEditando(m); setNombre(m.nombre); setPrioridad(m.prioridad);
    setObjetivo(String(m.objetivo)); setAcumulado(String(m.acumulado));
    setAporte(String(m.aporteMensualPlaneado));
    setConFecha(!!m.fechaLimite);
    if (m.fechaLimite) setFechaLimite(m.fechaLimite.slice(0, 10));
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const cuerpo = {
        nombre, prioridad,
        objetivo: parseFloat(objetivo) || 0,
        acumulado: parseFloat(acumulado) || 0,
        aporteMensualPlaneado: parseFloat(aporte) || 0,
        fechaLimite: conFecha ? fechaLimite : null,
        activa: true, notas: null, orden: editando?.orden ?? 99,
      };
      if (editando) await api.put(`/api/metas-ahorro/${editando.id}`, { id: editando.id, ...cuerpo });
      else await api.post("/api/metas-ahorro", cuerpo);
    },
    onSuccess: () => { cliente.invalidateQueries({ queryKey: ["metas"] }); limpiar(); },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/metas-ahorro/${id}`); },
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["metas"] }),
  });

  const aportar = useMutation({
    mutationFn: async ({ id, monto }: { id: number; monto: number }) =>
      api.post(`/api/metas-ahorro/${id}/aporte`, { monto }),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["metas"] }),
  });

  function enviar(e: FormEvent) {
    e.preventDefault();
    if (!nombre.trim() || parseFloat(objetivo) <= 0) return alert("Falta nombre o monto objetivo");
    guardar.mutate();
  }

  function aporteRapido(m: Meta) {
    const v = prompt(`Aporte para "${m.nombre}". Restante: ${formatoMoneda(m.objetivo - m.acumulado)}`, "0");
    if (!v) return;
    const monto = parseFloat(v);
    if (!monto || monto <= 0) return alert("Monto inválido");
    aportar.mutate({ id: m.id, monto });
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <header>
        <h1 className="text-3xl font-bold">🎯 Metas de ahorro</h1>
        <p className="text-muted-foreground">Define tus objetivos y mide tu progreso.</p>
      </header>

      <Card>
        <CardHeader><CardTitle>{editando ? "Editar meta" : "Nueva meta"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid sm:grid-cols-2 gap-3">
              <div className="space-y-1.5"><Label>Nombre</Label><Input required value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Vacaciones, Casa, Fondo emergencia..." /></div>
              <div className="space-y-1.5">
                <Label>Prioridad</Label>
                <Select value={prioridad} onChange={(e) => setPrioridad(e.target.value)}>
                  {PRIORIDADES.map(p => <option key={p} value={p}>{p}</option>)}
                </Select>
              </div>
              <div className="space-y-1.5"><Label>Monto objetivo</Label><Input type="number" step="0.01" required value={objetivo} onChange={(e) => setObjetivo(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Ya tienes</Label><Input type="number" step="0.01" value={acumulado} onChange={(e) => setAcumulado(e.target.value)} /></div>
              <div className="space-y-1.5"><Label>Aporte mensual planeado</Label><Input type="number" step="0.01" value={aporte} onChange={(e) => setAporte(e.target.value)} /></div>
              <div className="space-y-1.5">
                <Label className="flex items-center gap-2">
                  <input type="checkbox" checked={conFecha} onChange={(e) => setConFecha(e.target.checked)} />
                  Tiene fecha límite
                </Label>
                <Input type="date" disabled={!conFecha} value={fechaLimite} onChange={(e) => setFechaLimite(e.target.value)} />
              </div>
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
        <CardHeader><CardTitle>Tus metas ({metas.length})</CardTitle></CardHeader>
        <CardContent>
          {isLoading ? <p className="text-muted-foreground">Cargando…</p> :
           metas.length === 0 ? <p className="text-muted-foreground">Sin metas. Crea una arriba.</p> :
           <ul className="space-y-4">
             {metas.map(m => {
               const restante = Math.max(0, m.objetivo - m.acumulado);
               const pct = m.objetivo > 0 ? Math.min(100, (m.acumulado / m.objetivo) * 100) : 0;
               const lograda = m.acumulado >= m.objetivo && m.objetivo > 0;
               return (
                 <li key={m.id} className="border rounded-lg p-3">
                   <div className="flex items-start justify-between gap-2 mb-2">
                     <div>
                       <p className="font-semibold">
                         {m.nombre}
                         <span className="ml-2 text-xs bg-muted px-1.5 py-0.5 rounded">{m.prioridad}</span>
                         {lograda && <span className="ml-2 text-xs bg-emerald-100 text-emerald-800 px-1.5 py-0.5 rounded">✅ Alcanzada</span>}
                       </p>
                       <p className="text-xs text-muted-foreground">
                         {formatoMoneda(m.acumulado)} / {formatoMoneda(m.objetivo)} · Restante: {formatoMoneda(restante)}
                         {m.fechaLimite && ` · Hasta: ${formatoFecha(m.fechaLimite)}`}
                       </p>
                     </div>
                     <div className="flex gap-1">
                       <Button size="sm" variant="outline" onClick={() => aporteRapido(m)} disabled={lograda}>
                         <PiggyBank className="w-3.5 h-3.5 mr-1" />Aportar
                       </Button>
                       <Button size="icon" variant="ghost" onClick={() => editar(m)}><Pencil className="w-4 h-4" /></Button>
                       <Button size="icon" variant="ghost" onClick={() => { if (confirm(`¿Eliminar "${m.nombre}"?`)) eliminar.mutate(m.id); }}>
                         <Trash2 className="w-4 h-4 text-destructive" />
                       </Button>
                     </div>
                   </div>
                   <div className="h-2 bg-muted rounded-full overflow-hidden">
                     <div className={`h-full ${lograda ? "bg-emerald-500" : "bg-blue-500"}`} style={{ width: `${pct}%` }} />
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
