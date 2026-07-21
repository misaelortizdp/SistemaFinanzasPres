import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, Copy } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { formatoMoneda, mesActual, NOMBRES_MES, useCategorias } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useConfirm } from "@/lib/useConfirm";
import { Skeleton } from "@/components/ui/skeleton";

interface Linea {
  id: number; categoriaId: number; nombreCategoria?: string;
  anio: number; mes: number; monto: number; ejecutado: number;
}

const PILARES = [
  { tipo: 1 as const, label: "Necesidades", icono: "🏠", color: "text-red-700 dark:text-red-400",     borde: "border-red-200 dark:border-red-800",     barra: "bg-red-500" },
  { tipo: 2 as const, label: "Deseos",      icono: "🎮", color: "text-purple-700 dark:text-purple-400", borde: "border-purple-200 dark:border-purple-800", barra: "bg-purple-500" },
  { tipo: 3 as const, label: "Ahorro",      icono: "💰", color: "text-emerald-700 dark:text-emerald-400", borde: "border-emerald-200 dark:border-emerald-800", barra: "bg-emerald-500" },
];

export default function PaginaPresupuesto() {
  const cliente = useQueryClient();
  const { confirm, ConfirmDialog } = useConfirm();
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
    onMutate: async (datos: { categoriaId: number; monto: number }) => {
      await cliente.cancelQueries({ queryKey: ["presupuesto", anio, mes] });
      const anterior = cliente.getQueryData(["presupuesto", anio, mes]);
      
      // Actualización optimista
      cliente.setQueryData(["presupuesto", anio, mes], (old: Linea[] = []) => {
        const existe = old.find(l => l.categoriaId === datos.categoriaId);
        if (existe) {
          return old.map(l => l.categoriaId === datos.categoriaId ? { ...l, monto: datos.monto } : l);
        }
        return old;
      });
      
      return { anterior };
    },
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["presupuesto", anio, mes] }),
    onError: (_error, _variables, context) => {
      if (context?.anterior) {
        cliente.setQueryData(["presupuesto", anio, mes], context.anterior);
      }
    },
  });

  const copiarMesAnterior = useMutation({
    mutationFn: async () => (await api.post(`/api/presupuesto/copiar-mes-anterior?anio=${anio}&mes=${mes}`)).data,
    onSuccess: (datos: any) => {
      cliente.invalidateQueries({ queryKey: ["presupuesto", anio, mes] });
      toast.success(`Copiadas ${datos?.copiadas ?? 0} categorías del mes anterior`);
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.error || "No se pudo copiar el mes anterior");
    },
  });

  function cambiarMes(delta: number) {
    let m = mes + delta, a = anio;
    if (m === 0) { m = 12; a--; } if (m === 13) { m = 1; a++; }
    setMes(m); setAnio(a);
  }

  // Construir filas enriquecidas con tipo de categoría
  const filas = categorias
    .filter(c => c.tipo !== 4 && c.activa)
    .map(c => {
      const linea = lineas.find(l => l.categoriaId === c.id);
      return { categoria: c, monto: linea?.monto ?? 0, ejecutado: linea?.ejecutado ?? 0 };
    });

  const totalPresupuestado = filas.reduce((s, f) => s + f.monto, 0);
  const totalEjecutado = filas.reduce((s, f) => s + f.ejecutado, 0);

  return (
    <>
      <ConfirmDialog />
      <div className="max-w-5xl mx-auto space-y-6">
        <header className="flex items-center justify-between flex-wrap gap-3">
          <h1 className="text-3xl font-bold">📊 Presupuesto</h1>
          <div className="flex items-center gap-2">
            <Button size="icon" variant="outline" onClick={() => cambiarMes(-1)} className="h-11 w-11"><ChevronLeft className="w-4 h-4" /></Button>
            <span className="text-sm font-medium min-w-[140px] text-center">{NOMBRES_MES[mes - 1]} {anio}</span>
            <Button size="icon" variant="outline" onClick={() => cambiarMes(1)} className="h-11 w-11"><ChevronRight className="w-4 h-4" /></Button>
          </div>
        </header>

      {/* Resumen global */}
      <Card>
        <CardContent className="pt-5 flex items-center justify-between flex-wrap gap-3">
          <div>
            <p className="text-sm text-muted-foreground">Total ejecutado / presupuestado</p>
            <p className="text-xl font-bold">{formatoMoneda(totalEjecutado)} / {formatoMoneda(totalPresupuestado)}</p>
          </div>
          <Button variant="outline" onClick={async () => {
            const confirmado = await confirm({
              title: "Copiar mes anterior",
              description: "¿Importar montos del mes anterior para categorías sin presupuesto?",
              confirmText: "Copiar",
              cancelText: "Cancelar"
            });
            if (confirmado) copiarMesAnterior.mutate();
          }}>
            <Copy className="w-4 h-4 mr-2" />Copiar mes anterior
          </Button>
        </CardContent>
      </Card>

      {/* Pilares */}
      {isLoading ? (
        <div className="space-y-4">
          {[1, 2, 3].map((i) => (
            <Card key={i}>
              <CardContent className="pt-5 space-y-3">
                <div className="flex items-center justify-between">
                  <Skeleton className="h-5 w-32" />
                  <Skeleton className="h-4 w-24" />
                </div>
                <Skeleton className="h-2 w-full" />
                {[1, 2, 3].map((j) => (
                  <div key={j} className="space-y-2">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Skeleton className="h-4 w-4 rounded-full" />
                        <Skeleton className="h-4 w-28" />
                      </div>
                      <div className="flex items-center gap-2">
                        <Skeleton className="h-4 w-16" />
                        <Skeleton className="h-8 w-28" />
                      </div>
                    </div>
                    <Skeleton className="h-1 w-full" />
                  </div>
                ))}
              </CardContent>
            </Card>
          ))}
        </div>
      ) : filas.length === 0 ? (
        <p className="text-muted-foreground">No tienes categorías. Crea algunas en Categorías.</p>
      ) : (
        <div className="space-y-4">
          {PILARES.map((pilar) => {
            const grupo = filas.filter(f => f.categoria.tipo === pilar.tipo);
            if (grupo.length === 0) return null;

            const pilarPresupuestado = grupo.reduce((s, f) => s + f.monto, 0);
            const pilarEjecutado = grupo.reduce((s, f) => s + f.ejecutado, 0);
            const pilarPct = pilarPresupuestado > 0 ? Math.min(100, (pilarEjecutado / pilarPresupuestado) * 100) : 0;
            const pilarExcedido = pilarEjecutado > pilarPresupuestado && pilarPresupuestado > 0;

            return (
              <Card key={pilar.tipo} className={`border ${pilar.borde}`}>
                <CardHeader className="pb-2 pt-4 px-4">
                  <CardTitle className={`text-sm font-semibold flex items-center gap-2 ${pilar.color}`}>
                    <span className="text-base">{pilar.icono}</span>
                    {pilar.label}
                    <span className="ml-auto font-normal text-muted-foreground text-xs">
                      {formatoMoneda(pilarEjecutado)} / {formatoMoneda(pilarPresupuestado)}
                    </span>
                  </CardTitle>
                  {/* Barra de progreso del pilar */}
                  <div className="h-1.5 bg-muted rounded-full overflow-hidden mt-1.5">
                    <div
                      className={`h-full transition-all rounded-full ${pilarExcedido ? "bg-red-500" : pilarPct > 80 ? "bg-amber-500" : pilar.barra}`}
                      style={{ width: `${pilarPct}%` }}
                    />
                  </div>
                </CardHeader>
                <CardContent className="px-4 pb-3">
                  <ul className="divide-y">
                    {grupo.map(({ categoria, monto, ejecutado }) => {
                      const pct = monto > 0 ? Math.min(100, (ejecutado / monto) * 100) : 0;
                      const excedido = ejecutado > monto && monto > 0;
                      return (
                        <li key={categoria.id} className="py-2.5">
                          <div className="flex items-center justify-between gap-2 mb-1">
                            <div className="flex items-center gap-2 min-w-0">
                              <span className="text-base">{categoria.icono}</span>
                              <span className="font-medium text-sm truncate">{categoria.nombre}</span>
                            </div>
                            <div className="flex items-center gap-2">
                              <span className={`text-sm font-semibold ${excedido ? "text-red-600" : "text-muted-foreground"}`}>
                                {formatoMoneda(ejecutado)}
                              </span>
                              <span className="text-muted-foreground text-xs">/</span>
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
                          <div className="h-1 bg-muted rounded-full overflow-hidden">
                            <div
                              className={`h-full transition-all ${excedido ? "bg-red-500" : pct > 80 ? "bg-amber-500" : "bg-emerald-500"}`}
                              style={{ width: `${pct}%` }}
                            />
                          </div>
                        </li>
                      );
                    })}
                  </ul>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
      </div>
    </>
  );
}
