import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, Copy } from "lucide-react";
import { api } from "@/lib/api";
import { formatoMoneda, mesActual, NOMBRES_MES, useCategorias } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

interface Linea {
  id: number; categoriaId: number; nombreCategoria?: string;
  anio: number; mes: number; monto: number; ejecutado: number;
}

export default function PaginaPresupuesto() {
  const cliente = useQueryClient();
  const ahora = mesActual();
  const [anio, setAnio] = useState(ahora.anio);
  const [mes, setMes] = useState(ahora.mes);

  const { data: categorias = [] } = useCategorias();
  const { data: lineas = [], isLoading } = useQuery({
    queryKey: ["presupuesto", anio, mes],
    queryFn: async () => (await api.get<Linea[]>(`/api/presupuesto?anio=${anio}&mes=${mes}`)).data,
  });

  const guardar = useMutation({
    mutationFn: async (datos: { categoriaId: number; monto: number }) => {
      await api.put("/api/presupuesto", { categoriaId: datos.categoriaId, anio, mes, monto: datos.monto });
    },
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["presupuesto", anio, mes] }),
  });

  const copiarMesAnterior = useMutation({
    mutationFn: async () => (await api.post(`/api/presupuesto/copiar-mes-anterior?anio=${anio}&mes=${mes}`)).data,
    onSuccess: (datos: any) => {
      cliente.invalidateQueries({ queryKey: ["presupuesto", anio, mes] });
      alert(`Copiadas ${datos?.copiadas ?? 0} categorías del mes anterior.`);
    },
  });

  function cambiarMes(delta: number) {
    let m = mes + delta, a = anio;
    if (m === 0) { m = 12; a--; } if (m === 13) { m = 1; a++; }
    setMes(m); setAnio(a);
  }

  const filas = categorias
    .filter(c => c.tipo !== 4 && c.activa)
    .map(c => {
      const linea = lineas.find(l => l.categoriaId === c.id);
      return {
        categoria: c, linea,
        monto: linea?.monto ?? 0,
        ejecutado: linea?.ejecutado ?? 0,
      };
    });

  const totalPresupuestado = filas.reduce((s, f) => s + f.monto, 0);
  const totalEjecutado = filas.reduce((s, f) => s + f.ejecutado, 0);

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <header className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-3xl font-bold">📊 Presupuesto</h1>
        <div className="flex items-center gap-2">
          <Button size="icon" variant="outline" onClick={() => cambiarMes(-1)}><ChevronLeft className="w-4 h-4" /></Button>
          <span className="text-sm font-medium min-w-[140px] text-center">{NOMBRES_MES[mes - 1]} {anio}</span>
          <Button size="icon" variant="outline" onClick={() => cambiarMes(1)}><ChevronRight className="w-4 h-4" /></Button>
        </div>
      </header>

      <Card>
        <CardContent className="pt-5 flex items-center justify-between flex-wrap gap-3">
          <div>
            <p className="text-sm text-muted-foreground">Total presupuestado / ejecutado</p>
            <p className="text-xl font-bold">{formatoMoneda(totalEjecutado)} / {formatoMoneda(totalPresupuestado)}</p>
          </div>
          <Button variant="outline" onClick={() => {
            if (confirm("¿Importar montos del mes anterior para categorías sin presupuesto?")) copiarMesAnterior.mutate();
          }}>
            <Copy className="w-4 h-4 mr-2" />Copiar mes anterior
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Categorías</CardTitle></CardHeader>
        <CardContent>
          {isLoading ? <p className="text-muted-foreground">Cargando…</p> :
           filas.length === 0 ? <p className="text-muted-foreground">No tienes categorías. Crea algunas en /categorias.</p> :
           <ul className="divide-y">
             {filas.map(({ categoria, monto, ejecutado }) => {
               const pct = monto > 0 ? Math.min(100, (ejecutado / monto) * 100) : 0;
               const excedido = ejecutado > monto && monto > 0;
               return (
                 <li key={categoria.id} className="py-3">
                   <div className="flex items-center justify-between gap-2 mb-1">
                     <div className="flex items-center gap-2 min-w-0">
                       <span className="text-lg">{categoria.icono}</span>
                       <span className="font-medium truncate">{categoria.nombre}</span>
                     </div>
                     <div className="flex items-center gap-2">
                       <span className={`text-sm font-semibold ${excedido ? "text-red-600" : "text-muted-foreground"}`}>
                         {formatoMoneda(ejecutado)}
                       </span>
                       <span className="text-muted-foreground">/</span>
                       <Input
                         type="number"
                         step="1"
                         className="w-28 h-8 text-right"
                         defaultValue={monto}
                         onBlur={(e) => {
                           const val = parseFloat(e.target.value) || 0;
                           if (val !== monto) guardar.mutate({ categoriaId: categoria.id, monto: val });
                         }}
                       />
                     </div>
                   </div>
                   <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                     <div
                       className={`h-full transition-all ${excedido ? "bg-red-500" : pct > 80 ? "bg-amber-500" : "bg-emerald-500"}`}
                       style={{ width: `${pct}%` }}
                     />
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
