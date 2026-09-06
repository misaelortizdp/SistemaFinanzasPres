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
import { SkeletonTable } from "@/components/ui/skeleton";
import { obtenerMensajeError, validaciones } from "@/lib/errorUtils";

const TIPO_INGRESO = 4;

interface Movimiento {
  id: number; fecha: string; concepto: string;
  categoriaId: number; nombreCategoria?: string; tipoCategoria?: number;
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
  
  // Estados para validación inline
  const [errores, setErrores] = useState<Record<string, string>>({});

  function limpiar() {
    setEditando(null); setFecha(new Date().toISOString().slice(0, 10));
    setConcepto(""); setCategoriaId(0); setCuentaId(""); setMonto("0"); setNotas("");
    setErrores({});
  }
  
  function editar(m: Movimiento) {
    setEditando(m); setFecha(m.fecha.slice(0, 10)); setConcepto(m.concepto);
    setCategoriaId(m.categoriaId); setCuentaId(m.cuentaId ?? ""); setMonto(String(m.monto)); setNotas(m.notas ?? "");
    setErrores({});
  }
  
  // Validar campo individual
  function validarCampo(campo: string, valor: any): string | null {
    switch (campo) {
      case "concepto":
        return validaciones.requerido(valor, "concepto") || validaciones.longitudMinima(valor, 3, "concepto");
      case "monto":
        return validaciones.montoPositivo(valor, "monto");
      case "categoriaId":
        return !valor || valor === 0 ? "Selecciona una categoría" : null;
      case "fecha":
        return validaciones.fechaValida(valor, "fecha");
      default:
        return null;
    }
  }
  
  // Manejar cambio de campo con validación
  function manejarCampo(campo: string, valor: any, setter: (v: any) => void) {
    setter(valor);
    const error = validarCampo(campo, valor);
    setErrores(prev => ({
      ...prev,
      [campo]: error || ""
    }));
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
      const mensaje = obtenerMensajeError(error, "No se pudo guardar el movimiento");
      toast.error(mensaje);
    },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/movimientos/${id}`); },
    onMutate: async (id: number) => {
      await cliente.cancelQueries({ queryKey: ["movimientos", anio, mes] });
      const anterior = cliente.getQueryData(["movimientos", anio, mes]);
      
      // Actualización optimista: eliminar de la lista
      cliente.setQueryData(["movimientos", anio, mes], (old: Movimiento[] = []) => old.filter(m => m.id !== id));
      
      return { anterior };
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["movimientos"] });
      cliente.invalidateQueries({ queryKey: ["cuentas"] });
      toast.success("Movimiento eliminado");
    },
    onError: (error: any, _variables, context) => {
      if (context?.anterior) {
        cliente.setQueryData(["movimientos", anio, mes], context.anterior);
      }
      const mensaje = obtenerMensajeError(error, "No se pudo eliminar el movimiento");
      toast.error(mensaje);
    },
  });

  function enviar(e: FormEvent) {
    e.preventDefault();
    
    // Validar todos los campos
    const nuevosErrores: Record<string, string> = {};
    
    const errorConcepto = validarCampo("concepto", concepto);
    if (errorConcepto) nuevosErrores.concepto = errorConcepto;
    
    const errorMonto = validarCampo("monto", monto);
    if (errorMonto) nuevosErrores.monto = errorMonto;
    
    const errorCategoria = validarCampo("categoriaId", categoriaId);
    if (errorCategoria) nuevosErrores.categoriaId = errorCategoria;
    
    const errorFecha = validarCampo("fecha", fecha);
    if (errorFecha) nuevosErrores.fecha = errorFecha;
    
    if (Object.keys(nuevosErrores).length > 0) {
      setErrores(nuevosErrores);
      toast.error("Por favor, corrige los errores del formulario");
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

  const ingresos = movimientosFiltrados.filter(m => m.tipoCategoria === TIPO_INGRESO).reduce((s, m) => s + m.monto, 0);
  const gastos = movimientosFiltrados.filter(m => m.tipoCategoria !== TIPO_INGRESO).reduce((s, m) => s + m.monto, 0);
  const hayFiltro = filtroTexto.trim() !== "" || filtroCategoria !== "";

  return (
    <>
      <ConfirmDialog />
      <div className="max-w-4xl mx-auto space-y-6">
        <header className="flex items-center justify-between flex-wrap gap-3">
          <h1 className="text-3xl font-bold">🛒 Movimientos</h1>
          <div className="flex items-center gap-2">
            <Button size="icon" variant="outline" onClick={() => cambiarMes(-1)} className="h-11 w-11"><ChevronLeft className="w-4 h-4" /></Button>
            <span className="text-sm font-medium min-w-[140px] text-center">{NOMBRES_MES[mes - 1]} {anio}</span>
            <Button size="icon" variant="outline" onClick={() => cambiarMes(1)} className="h-11 w-11"><ChevronRight className="w-4 h-4" /></Button>
          </div>
        </header>

      <Card>
        <CardHeader><CardTitle>{editando ? "Editar movimiento" : "Nuevo movimiento"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div className="space-y-1.5">
                <Label>Fecha</Label>
                <Input 
                  type="date" 
                  value={fecha} 
                  onChange={(e) => manejarCampo("fecha", e.target.value, setFecha)} 
                  className={`h-11 ${errores.fecha ? "border-red-500" : ""}`} 
                />
                {errores.fecha && <p className="text-xs text-red-600">{errores.fecha}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Monto</Label>
                <Input 
                  type="number" 
                  step="0.01" 
                  value={monto} 
                  onChange={(e) => manejarCampo("monto", e.target.value, setMonto)} 
                  className={`h-11 ${errores.monto ? "border-red-500" : ""}`} 
                />
                {errores.monto && <p className="text-xs text-red-600">{errores.monto}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Categoría</Label>
                <Select 
                  value={categoriaId} 
                  onChange={(e) => manejarCampo("categoriaId", Number(e.target.value), setCategoriaId)} 
                  className={`h-11 ${errores.categoriaId ? "border-red-500" : ""}`}
                >
                  <option value="">— Selecciona —</option>
                  {categorias.map(c => <option key={c.id} value={c.id}>{c.icono} {c.nombre}</option>)}
                </Select>
                {errores.categoriaId && <p className="text-xs text-red-600">{errores.categoriaId}</p>}
              </div>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label>Concepto</Label>
                <Input 
                  value={concepto} 
                  onChange={(e) => manejarCampo("concepto", e.target.value, setConcepto)} 
                  placeholder="Comida en restaurante, etc." 
                  className={`h-11 ${errores.concepto ? "border-red-500" : ""}`} 
                />
                {errores.concepto && <p className="text-xs text-red-600">{errores.concepto}</p>}
              </div>
              <div className="space-y-1.5">
                <Label>Cuenta (opcional)</Label>
                <Select value={cuentaId} onChange={(e) => setCuentaId(e.target.value === "" ? "" : Number(e.target.value))} className="h-11">
                  <option value="">— Sin cuenta —</option>
                  {cuentas.map(c => <option key={c.id} value={c.id}>{c.nombre}</option>)}
                </Select>
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Notas (opcional)</Label>
              <Textarea value={notas} onChange={(e) => setNotas(e.target.value)} className="min-h-[44px]" />
            </div>
            <div className="flex gap-2">
              <Button type="submit" disabled={guardar.isPending} className="h-11 px-6">
                <Plus className="w-4 h-4 mr-2" />{editando ? "Guardar" : "Agregar"}
              </Button>
              {editando && <Button type="button" variant="outline" onClick={limpiar} className="h-11 px-6">Cancelar</Button>}
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex flex-wrap items-baseline gap-x-2">
            <span>Movimientos del mes ({hayFiltro ? `${movimientosFiltrados.length} de ${movimientos.length}` : movimientos.length})</span>
            <span className="text-sm font-normal">
              Ingresos: <span className="text-emerald-600 font-semibold">{formatoMoneda(ingresos)}</span>
              {" · "}Gastos: <span className="text-red-600 font-semibold">{formatoMoneda(gastos)}</span>
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {/* Buscador */}
          <div className="flex gap-2 flex-wrap">
            <div className="relative flex-1 min-w-[200px]">
              <Search className="absolute left-2.5 top-3 w-4 h-4 text-muted-foreground" />
              <Input
                className="pl-8 h-11"
                placeholder="Buscar por concepto, categoría o notas…"
                value={filtroTexto}
                onChange={(e) => setFiltroTexto(e.target.value)}
              />
            </div>
            <Select
              className="w-full sm:w-48 h-11"
              value={filtroCategoria}
              onChange={(e) => setFiltroCategoria(e.target.value === "" ? "" : Number(e.target.value))}
            >
              <option value="">Todas las categorías</option>
              {categorias.map(c => <option key={c.id} value={c.id}>{c.icono} {c.nombre}</option>)}
            </Select>
            {hayFiltro && (
              <Button variant="ghost" size="icon" onClick={() => { setFiltroTexto(""); setFiltroCategoria(""); }} className="h-11 w-11">
                <X className="w-4 h-4" />
              </Button>
            )}
          </div>

          {isLoading ? <SkeletonTable rows={8} /> :
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
                   <div className="flex items-center gap-1 shrink-0">
                     <span className={`font-semibold mr-1 ${m.tipoCategoria === TIPO_INGRESO ? "text-emerald-600" : "text-red-600"}`}>
                       {m.tipoCategoria === TIPO_INGRESO ? "+" : "-"}{formatoMoneda(m.monto)}
                     </span>
                     <Button size="icon" variant="ghost" onClick={() => editar(m)} className="h-11 w-11"><Pencil className="w-4 h-4" /></Button>
                     <Button size="icon" variant="ghost" onClick={async () => {
                       const confirmado = await confirm({
                         title: "Eliminar movimiento",
                         description: "¿Estás seguro de eliminar este movimiento? Esta acción no se puede deshacer.",
                         confirmText: "Eliminar",
                         cancelText: "Cancelar",
                         variant: "destructive"
                       });
                       if (confirmado) eliminar.mutate(m.id);
                     }} className="h-11 w-11">
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
