import { FormEvent, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2, Plus } from "lucide-react";
import { api } from "@/lib/api";
import { useCuentas, formatoMoneda, Cuenta } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function PaginaCuentas() {
  const cliente = useQueryClient();
  const { data: cuentas = [], isLoading } = useCuentas();
  const [editando, setEditando] = useState<Cuenta | null>(null);
  const [nombre, setNombre] = useState("");
  const [saldo, setSaldo] = useState("0");

  function limpiar() { setEditando(null); setNombre(""); setSaldo("0"); }
  function editar(c: Cuenta) { setEditando(c); setNombre(c.nombre); setSaldo(String(c.saldo)); }

  const guardar = useMutation({
    mutationFn: async () => {
      const cuerpo = { nombre, saldo: parseFloat(saldo) || 0, orden: editando?.orden ?? 99, activa: true };
      if (editando) await api.put(`/api/cuentas/${editando.id}`, { id: editando.id, ...cuerpo });
      else await api.post("/api/cuentas", cuerpo);
    },
    onSuccess: () => { cliente.invalidateQueries({ queryKey: ["cuentas"] }); limpiar(); },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/cuentas/${id}`); },
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["cuentas"] }),
  });

  function enviar(e: FormEvent) { e.preventDefault(); if (!nombre.trim()) return; guardar.mutate(); }

  const total = cuentas.reduce((s, c) => s + c.saldo, 0);

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <header>
        <h1 className="text-3xl font-bold">🏦 Cuentas</h1>
        <p className="text-muted-foreground">Saldo total: <span className="font-semibold">{formatoMoneda(total)}</span></p>
      </header>

      <Card>
        <CardHeader><CardTitle>{editando ? `Editar: ${editando.nombre}` : "Nueva cuenta"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid sm:grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="n">Nombre</Label>
                <Input id="n" required value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Cuenta bancaria, Efectivo, ..." />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="s">Saldo</Label>
                <Input id="s" type="number" step="0.01" value={saldo} onChange={(e) => setSaldo(e.target.value)} />
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
        <CardHeader><CardTitle>Tus cuentas ({cuentas.length})</CardTitle></CardHeader>
        <CardContent>
          {isLoading ? <p className="text-muted-foreground">Cargando…</p> :
           cuentas.length === 0 ? <p className="text-muted-foreground">Sin cuentas.</p> :
           <ul className="divide-y">
             {cuentas.map((c) => (
               <li key={c.id} className="py-3 flex items-center justify-between">
                 <div>
                   <p className="font-medium">{c.nombre}</p>
                   <p className={`text-sm font-semibold ${c.saldo >= 0 ? "text-emerald-600" : "text-red-600"}`}>
                     {formatoMoneda(c.saldo)}
                   </p>
                 </div>
                 <div className="flex gap-1">
                   <Button size="icon" variant="ghost" onClick={() => editar(c)}><Pencil className="w-4 h-4" /></Button>
                   <Button size="icon" variant="ghost" onClick={() => { if (confirm(`¿Eliminar "${c.nombre}"?`)) eliminar.mutate(c.id); }}>
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
