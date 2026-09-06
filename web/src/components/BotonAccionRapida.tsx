import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { useCategorias, mesActual } from "@/lib/hooks";
import { cn } from "@/lib/utils";
import { obtenerMensajeError } from "@/lib/errorUtils";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";

const TIPO_INGRESO = 4;

export function BotonAccionRapida() {
  const cliente = useQueryClient();
  const { anio, mes } = mesActual();
  const [abierto, setAbierto] = useState(false);
  const [monto, setMonto] = useState("");
  const [categoriaId, setCategoriaId] = useState<number | null>(null);
  const [concepto, setConcepto] = useState("");

  const { data: categorias = [] } = useCategorias();

  // Mismo queryKey que Panel/Movimientos — si ya se cargó este mes, se reusa
  // del caché sin otro request; si no, se pide solo al abrir el modal.
  const { data: movimientosMes = [] } = useQuery({
    queryKey: ["movimientos", anio, mes],
    queryFn: async () => (await api.get<{ categoriaId: number }[]>(`/api/movimientos?anio=${anio}&mes=${mes}`)).data,
    enabled: abierto,
  });

  const categoriasGasto = useMemo(
    () => categorias.filter((c) => c.tipo !== TIPO_INGRESO && c.activa),
    [categorias]
  );

  // Categorías más usadas este mes primero — "categorías recientes primero".
  const recientes = useMemo(() => {
    const conteo = new Map<number, number>();
    for (const m of movimientosMes) conteo.set(m.categoriaId, (conteo.get(m.categoriaId) ?? 0) + 1);
    return [...categoriasGasto]
      .filter((c) => conteo.has(c.id))
      .sort((a, b) => (conteo.get(b.id) ?? 0) - (conteo.get(a.id) ?? 0))
      .slice(0, 6);
  }, [movimientosMes, categoriasGasto]);

  function limpiar() {
    setMonto("");
    setCategoriaId(null);
    setConcepto("");
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const categoria = categorias.find((c) => c.id === categoriaId);
      await api.post("/api/movimientos", {
        fecha: new Date().toISOString().slice(0, 10),
        concepto: concepto.trim() || categoria?.nombre || "Gasto",
        categoriaId,
        monto: parseFloat(monto) || 0,
        notas: null,
      });
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["movimientos"] });
      cliente.invalidateQueries({ queryKey: ["panel-resumen"] });
      cliente.invalidateQueries({ queryKey: ["panel-kpis"] });
      cliente.invalidateQueries({ queryKey: ["panel-tendencias"] });
      cliente.invalidateQueries({ queryKey: ["presupuesto"] });
      toast.success("Gasto registrado");
      limpiar();
      setAbierto(false);
    },
    onError: (error) => toast.error(obtenerMensajeError(error, "No se pudo registrar el gasto")),
  });

  function enviar() {
    const montoNum = parseFloat(monto);
    if (!montoNum || montoNum <= 0) { toast.error("Ingresa un monto válido"); return; }
    if (!categoriaId) { toast.error("Elige una categoría"); return; }
    guardar.mutate();
  }

  return (
    <>
      <button
        onClick={() => setAbierto(true)}
        title="Registrar gasto rápido"
        className="fixed right-4 bottom-20 md:bottom-6 md:right-8 z-40 w-14 h-14 rounded-full bg-primary text-primary-foreground shadow-lg flex items-center justify-center hover:opacity-90 active:scale-95 transition"
      >
        <Plus className="w-6 h-6" />
      </button>

      <Dialog open={abierto} onOpenChange={(v) => { setAbierto(v); if (!v) limpiar(); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>💸 Gasto rápido</DialogTitle>
          </DialogHeader>
          <div className="px-6 pb-2 space-y-4">
            <div className="space-y-1.5">
              <Label>Monto</Label>
              <Input
                type="number" step="1" inputMode="decimal" autoFocus
                className="h-12 text-lg"
                value={monto}
                onChange={(e) => setMonto(e.target.value)}
                placeholder="0"
              />
            </div>

            <div className="space-y-1.5">
              <Label>Categoría</Label>
              {recientes.length > 0 && (
                <div className="flex flex-wrap gap-1.5 mb-2">
                  {recientes.map((c) => (
                    <button
                      key={c.id}
                      type="button"
                      onClick={() => setCategoriaId(c.id)}
                      className={cn(
                        "px-3 py-1.5 rounded-full text-sm border transition-colors",
                        categoriaId === c.id
                          ? "bg-primary text-primary-foreground border-primary"
                          : "bg-muted hover:bg-accent border-transparent"
                      )}
                    >
                      {c.icono} {c.nombre}
                    </button>
                  ))}
                </div>
              )}
              <Select value={categoriaId ?? ""} onChange={(e) => setCategoriaId(Number(e.target.value) || null)}>
                <option value="">{recientes.length > 0 ? "Otra categoría…" : "Elige una categoría"}</option>
                {categoriasGasto.map((c) => (
                  <option key={c.id} value={c.id}>{c.icono} {c.nombre}</option>
                ))}
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label>Concepto (opcional)</Label>
              <Input value={concepto} onChange={(e) => setConcepto(e.target.value)} placeholder="Ej. Almuerzo" />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setAbierto(false)}>Cancelar</Button>
            <Button onClick={enviar} disabled={guardar.isPending}>
              {guardar.isPending ? "Guardando…" : "Guardar"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
