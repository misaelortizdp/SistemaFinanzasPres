import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Trash2, Pencil, Plus, EyeOff, Eye } from "lucide-react";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

type TipoCategoria = 1 | 2 | 3 | 4;

const PILARES: { tipo: TipoCategoria; label: string; icono: string; color: string; borde: string }[] = [
  { tipo: 1, label: "Necesidades", icono: "🏠", color: "text-red-700 dark:text-red-400",   borde: "border-red-200 dark:border-red-800" },
  { tipo: 2, label: "Deseos",      icono: "🎮", color: "text-purple-700 dark:text-purple-400", borde: "border-purple-200 dark:border-purple-800" },
  { tipo: 3, label: "Ahorro",      icono: "💰", color: "text-emerald-700 dark:text-emerald-400", borde: "border-emerald-200 dark:border-emerald-800" },
  { tipo: 4, label: "Ingresos",    icono: "💼", color: "text-blue-700 dark:text-blue-400",  borde: "border-blue-200 dark:border-blue-800" },
];

interface Categoria {
  id: number;
  nombre: string;
  tipo: TipoCategoria;
  color?: string | null;
  icono?: string | null;
  orden: number;
  activa: boolean;
}

export default function PaginaCategorias() {
  const cliente = useQueryClient();

  const { data: categorias = [], isLoading } = useQuery({
    queryKey: ["categorias"],
    queryFn: async () => (await api.get<Categoria[]>("/api/categorias")).data,
  });

  const [editando, setEditando] = useState<Categoria | null>(null);
  const [nombre, setNombre] = useState("");
  const [tipo, setTipo] = useState<TipoCategoria>(1);
  const [icono, setIcono] = useState("");

  function limpiarFormulario() {
    setEditando(null);
    setNombre("");
    setTipo(1);
    setIcono("");
  }

  function cargarParaEditar(c: Categoria) {
    setEditando(c);
    setNombre(c.nombre);
    setTipo(c.tipo);
    setIcono(c.icono ?? "");
  }

  const guardar = useMutation({
    mutationFn: async () => {
      const cuerpo = { nombre, tipo, icono: icono || null, orden: editando?.orden ?? 99, activa: true };
      if (editando) {
        await api.put(`/api/categorias/${editando.id}`, { id: editando.id, ...cuerpo });
      } else {
        await api.post("/api/categorias", cuerpo);
      }
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["categorias"] });
      limpiarFormulario();
    },
  });

  const toggleActiva = useMutation({
    mutationFn: async (id: number) => api.patch(`/api/categorias/${id}/toggle-activa`),
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["categorias"] }),
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => {
      await api.delete(`/api/categorias/${id}`);
    },
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["categorias"] }),
    onError: (err: any) => {
      const msg = err?.response?.data?.error ?? "No se pudo eliminar la categoría.";
      alert(msg);
    },
  });

  function manejarEnvio(e: FormEvent) {
    e.preventDefault();
    if (!nombre.trim()) return;
    guardar.mutate();
  }

  return (
    <div className="container mx-auto max-w-3xl py-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">🗂 Categorías</h1>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{editando ? `Editar: ${editando.nombre}` : "Nueva categoría"}</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={manejarEnvio} className="space-y-3">
            <div className="grid sm:grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="nombre">Nombre</Label>
                <Input id="nombre" required value={nombre} onChange={(e) => setNombre(e.target.value)} />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="tipo">Tipo</Label>
                <select
                  id="tipo"
                  value={tipo}
                  onChange={(e) => setTipo(Number(e.target.value) as TipoCategoria)}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                >
                  <option value={1}>Necesidad</option>
                  <option value={2}>Deseo</option>
                  <option value={3}>Ahorro</option>
                  <option value={4}>Ingreso</option>
                </select>
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="icono">Icono / Emoji</Label>
              <Input id="icono" maxLength={4} placeholder="🏠" value={icono} onChange={(e) => setIcono(e.target.value)} />
            </div>
            <div className="flex gap-2">
              <Button type="submit" disabled={guardar.isPending}>
                <Plus className="w-4 h-4 mr-2" />
                {editando ? "Guardar cambios" : "Agregar"}
              </Button>
              {editando && (
                <Button type="button" variant="outline" onClick={limpiarFormulario}>
                  Cancelar
                </Button>
              )}
            </div>
          </form>
        </CardContent>
      </Card>

      {isLoading ? (
        <p className="text-muted-foreground">Cargando…</p>
      ) : categorias.length === 0 ? (
        <p className="text-muted-foreground">Aún no tienes categorías.</p>
      ) : (
        <div className="space-y-4">
          {PILARES.map((pilar) => {
            const grupo = categorias.filter((c) => c.tipo === pilar.tipo);
            if (grupo.length === 0) return null;
            return (
              <Card key={pilar.tipo} className={`border ${pilar.borde}`}>
                <CardHeader className="pb-2 pt-4 px-4">
                  <CardTitle className={`text-sm font-semibold flex items-center gap-2 ${pilar.color}`}>
                    <span className="text-base">{pilar.icono}</span>
                    {pilar.label}
                    <span className="ml-auto font-normal text-muted-foreground">{grupo.length}</span>
                  </CardTitle>
                </CardHeader>
                <CardContent className="px-4 pb-3">
                  <ul className="divide-y">
                    {grupo.map((c) => (
                      <li key={c.id} className={`flex items-center justify-between py-2.5 ${!c.activa ? "opacity-50" : ""}`}>
                        <div className="flex items-center gap-3">
                          <span className="text-xl w-7 text-center">{c.icono ?? "•"}</span>
                          <div>
                            <p className="font-medium text-sm">{c.nombre}</p>
                            {!c.activa && <p className="text-xs text-muted-foreground">Inactiva</p>}
                          </div>
                        </div>
                        <div className="flex gap-1">
                          <Button
                            size="icon"
                            variant="ghost"
                            className="h-8 w-8"
                            title={c.activa ? "Desactivar" : "Activar"}
                            onClick={() => toggleActiva.mutate(c.id)}
                          >
                            {c.activa ? <EyeOff className="w-3.5 h-3.5 text-muted-foreground" /> : <Eye className="w-3.5 h-3.5 text-emerald-600" />}
                          </Button>
                          <Button size="icon" variant="ghost" className="h-8 w-8" onClick={() => cargarParaEditar(c)}>
                            <Pencil className="w-3.5 h-3.5" />
                          </Button>
                          <Button
                            size="icon"
                            variant="ghost"
                            className="h-8 w-8"
                            onClick={() => {
                              if (confirm(`¿Eliminar "${c.nombre}"? Si tiene movimientos, solo podrás desactivarla.`)) eliminar.mutate(c.id);
                            }}
                          >
                            <Trash2 className="w-3.5 h-3.5 text-destructive" />
                          </Button>
                        </div>
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
