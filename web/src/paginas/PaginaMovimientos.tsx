import { FormEvent, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2, Plus, ChevronLeft, ChevronRight, Search, X } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { formatoFecha, formatoMoneda, mesActual, NOMBRES_MES, useCategorias, useCuentas } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { useConfirm } from "@/lib/useConfirm";

interface Movimiento {
  id: number; fecha: string; concepto: string;
  categoriaId: number; nombreCategoria?: string;
  cuentaId?: number | null; nombreCuenta?: string | null;
  monto: number; notas?: string | null;
}

export default function PaginaMovimientos() {
  const cliente = useQueryClient();
  const { confirm, ConfirmDialog } = useConfirm();
  const ahora = mesActual();
  const [anio, setAnio] = useState(ahora.anio);
  const [mes, setMes] = useState(ahora.mes);
  const [filtroTexto, setFiltroTexto] = useState("");
  const [filtroCategoria, setFiltroCategoria] = useState<number | "">("");

  const { data: categorias = [] } = useCategorias();
  const { data: cuentas = [] } = useCuentas();
  const { data: movimientos = [], isLoading } = useQuery({
    queryKey: ["movimientos", anio, mes],
    queryFn: async () => (await api.get<Movimiento[]>(`/api/movimientos?anio=${anio}&mes=${mes}`)).data,
  });

  const [editando, setEditando] = useState<Movimiento | null>(null);
  const [fecha, setFecha] = useState(new Date().toISOString().slice(0, 10));
  const [concepto, setConcepto] = useState("");
  const [categoriaId, setCategoriaId] = useState<number>(0);
  const [cuentaId, setCuentaId] = useState<number | "">("");
  const [monto, setMonto] = useState("0");
  const [notas, setNotas] = useState("");

  function limpiar() {
    setEditando(null); setFecha(new Date().toISOString().slice(0, 10));
    setConcepto(""); setCategoriaId(0); setCuentaId(""); setMonto("0"); setNotas("");
  }
  function editar(m: Movimiento) {
    setEditando(m); setFecha(m.fecha.slice(0, 10)); setConcepto(m.concepto);
    setCategoriaId(m.categoriaId); setCuentaId(m.cuentaId ?? ""); setMonto(String(m.monto)); setNotas(m.notas ?? "");
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const cuerpo = {
        fecha, concepto, categoriaId,
        cuentaId: cuentaId === "" ? null : cuentaId,
        monto: parseFloat(monto) || 0, notas: notas || null,
      };
      if (editando) await api.put(`/api/movimientos/${editando.id}`, { id: editando.id, ...cuerpo });
      else await api.post("/api/movimientos", cuerpo);
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["movimientos"] });
      cliente.invalidateQueries({ queryKey: ["cuentas"] });
      cliente.invalidateQueries({ queryKey: ["patrimonio-actual"] });
      toast.success(editando ? "Movimiento actualizado" : "Movimiento agregado con éxito");
      limpiar();
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.error || "No se pudo guardar el movimiento");
    },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/movimientos/${id}`); },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["movimientos"] });
      cliente.invalidateQueries({ queryKey: ["cuentas"] });
      toast.success("Movimiento eliminado");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.error || "No se pudo eliminar el movimiento");
    },
  });

  function enviar(e: FormEvent) {
    e.preventDefault();
    if (!concepto.trim() || !categoriaId) {
      toast.error("Falta concepto o categoría");
      return;
    }
    guardar.mutate();
  }

  function cambiarMes(delta: number) {
    let m = mes + delta, a = anio;
    if (m === 0) { m = 12; a--; }
    if (m === 13) { m = 1; a++; }
    setMes(m); setAnio(a);
  }

  const movimientosFiltrados = useMemo(() => {
    let lista = movimientos;
    if (filtroTexto.trim()) {
      const q = filtroTexto.toLowerCase();
      lista = lista.filter(m =>
        m.concepto.toLowerCase().includes(q) ||
        (m.nombreCategoria ?? "").toLowerCase().includes(q) ||
        (m.notas ?? "").toLowerCase().includes(q)
      );
    }
    if (filtroCategoria !== "") {
      lista = lista.filter(m => m.categoriaId === filtroCategoria);
    }
    return lista;
  }, [movimientos, filtroTexto, filtroCategoria]);

  const total = movimientosFiltrados.reduce((s, m) => s + m.monto, 0);
  const totalSinFiltro = movimientos.reduce((s, m) => s + m.monto, 0);
  const hayFiltro = filtroTexto.trim() !== "" || filtroCategoria !== "";
  const categoriasNoIngreso = categorias.filter(c => c.tipo !== 4);

  return (
    <>
      <ConfirmDialog />
      <div className="max-w-4xl mx-auto space-y-6">
        <header className="flex items-center justify-between flex-wrap gap-3">
          <h1 className="text-3xl font-bold">🛒 Movimientos</h1>
          <div className="flex items-center gap-2">
            <Button size="icon" variant="outline" onClick={() => cambiarMes(-1)}><ChevronLeft className="w-4 h-4" /></Button>
            <span className="text-sm font-medium min-w-[140px] text-center">{NOMBRES_MES[mes - 1]} {anio}</span>
            <Button size="icon" variant="outline" onClick={() => cambiarMes(1)}><ChevronRight className="w-4 h-4" /></Button>
          </div>
        </header>

      <Card>
        <CardHeader><CardTitle>{editando ? "Editar movimiento" : "Nuevo movimiento"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid sm:grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label>Fecha</Label>
                <Input type="date" required value={fecha} onChange={(e) => setFecha(e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label>Monto</Label>
                <Input type="number" step="0.01" required value={monto} onChange={(e) => setMonto(e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label>Categoría</Label>
                <Select required value={categoriaId} onChange={(e) => setCategoriaId(Number(e.target.value))}>
                  <option value="">— Selecciona —</option>
                  {categoriasNoIngreso.map(c => <option key={c.id} value={c.id}>{c.icono} {c.nombre}</option>)}
                </Select>
              </div>
            </div>
            <div className="grid sm:grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Concepto</Label>
                <Input required value={concepto} onChange={(e) => setConcepto(e.target.value)} placeholder="Comida en restaurante, etc." />
              </div>
              <div className="space-y-1.5">
                <Label>Cuenta (opcional)</Label>
                <Select value={cuentaId} onChange={(e) => setCuentaId(e.target.value === "" ? "" : Number(e.target.value))}>
                  <option value="">— Sin cuenta —</option>
                  {cuentas.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
                </Select>
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Notas (opcional)</Label>
              <Textarea value={notas} onChange={(e) => setNotas(e.target.value)} />
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
        <CardHeader>
          <CardTitle>
            Movimientos del mes ({hayFiltro ? `${movimientosFiltrados.length} de ${movimientos.length}` : movimientos.length})
            {" · "}Total: <span className="text-red-600">{formatoMoneda(total)}</span>
            {hayFiltro && <span className="text-xs text-muted-foreground ml-2">(total mes: {formatoMoneda(totalSinFiltro)})</span>}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {/* Buscador */}
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-2.5 top-2.5 w-4 h-4 text-muted-foreground" />
              <Input
                className="pl-8"
                placeholder="Buscar por concepto, categoría o notas…"
                value={filtroTexto}
                onChange={(e) => setFiltroTexto(e.target.value)}
              />
            </div>
            <Select
              className="w-44"
              value={filtroCategoria}
              onChange={(e) => setFiltroCategoria(e.target.value === "" ? "" : Number(e.target.value))}
            >
              <option value="">Todas las categorías</option>
              {categoriasNoIngreso.map(c => <option key={c.id} value={c.id}>{c.icono} {c.nombre}</option>)}
            </Select>
            {hayFiltro && (
              <Button variant="ghost" size="icon" onClick={() => { setFiltroTexto(""); setFiltroCategoria(""); }}>
                <X className="w-4 h-4" />
              </Button>
            )}
          </div>

          {isLoading ? <p className="text-muted-foreground">Cargando…</p> :
           movimientosFiltrados.length === 0 ? (
             <p className="text-muted-foreground">{hayFiltro ? "Sin resultados para ese filtro." : "Sin movimientos este mes."}</p>
           ) : (
             <ul className="divide-y">
               {movimientosFiltrados.map(m => (
                 <li key={m.id} className="py-3 flex items-start justify-between gap-3">
                   <div className="min-w-0 flex-1">
                     <p className="font-medium truncate">{m.concepto}</p>
                     <p className="text-xs text-muted-foreground">
                       {formatoFecha(m.fecha)} · {m.nombreCategoria}
                       {m.nombreCuenta ? ` · ${m.nombreCuenta}` : ""}
                       {m.notas ? ` · ${m.notas}` : ""}
                     </p>
                   </div>
                   <div className="flex items-center gap-1">
                     <span className="font-semibold text-red-600">{formatoMoneda(m.monto)}</span>
                     <Button size="icon" variant="ghost" onClick={() => editar(m)}><Pencil className="w-4 h-4" /></Button>
                     <Button size="icon" variant="ghost" onClick={async () => {
                       const confirmado = await confirm({
                         title: "Eliminar movimiento",
                         description: "¿Estás seguro de eliminar este movimiento? Esta acción no se puede deshacer.",
                         confirmText: "Eliminar",
                         cancelText: "Cancelar",
                         variant: "destructive"
                       });
                       if (confirmado) eliminar.mutate(m.id);
                     }}>
                       <Trash2 className="w-4 h-4 text-destructive" />
                     </Button>
                   </div>
                 </li>
               ))}
             </ul>
           )}
        </CardContent>
      </Card>
      </div>
    </>
  );
}
