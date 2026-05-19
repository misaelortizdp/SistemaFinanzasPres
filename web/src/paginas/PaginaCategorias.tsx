import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Trash2, Pencil, Plus } from "lucide-react";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

type TipoCategoria = 1 | 2 | 3 | 4;
const ETIQUETAS_TIPO: Record<TipoCategoria, string> = {
  1: "Necesidad",
  2: "Deseo",
  3: "Ahorro",
  4: "Ingreso",
};
const COLORES_TIPO: Record<TipoCategoria, string> = {
  1: "bg-red-100 text-red-800",
  2: "bg-purple-100 text-purple-800",
  3: "bg-emerald-100 text-emerald-800",
  4: "bg-blue-100 text-blue-800",
};

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

  const eliminar = useMutation({
    mutationFn: async (id: number) => {
      await api.delete(`/api/categorias/${id}`);
    },
    onSuccess: () => cliente.invalidateQueries({ queryKey: ["categorias"] }),
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

      <Card>
        <CardHeader>
          <CardTitle>Tus categorías ({categorias.length})</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-muted-foreground">Cargando…</p>
          ) : categorias.length === 0 ? (
            <p className="text-muted-foreground">Aún no tienes categorías.</p>
          ) : (
            <ul className="divide-y">
              {categorias.map((c) => (
                <li key={c.id} className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <span className="text-2xl w-8 text-center">{c.icono ?? "•"}</span>
                    <div>
                      <p className="font-medium">{c.nombre}</p>
                      <span className={`inline-block text-xs px-2 py-0.5 rounded-full mt-0.5 ${COLORES_TIPO[c.tipo]}`}>
                        {ETIQUETAS_TIPO[c.tipo]}
                      </span>
                    </div>
                  </div>
                  <div className="flex gap-1">
                    <Button size="icon" variant="ghost" onClick={() => cargarParaEditar(c)}>
                      <Pencil className="w-4 h-4" />
                    </Button>
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={() => {
                        if (confirm(`¿Eliminar "${c.nombre}"?`)) eliminar.mutate(c.id);
                      }}
                    >
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
  );
}
