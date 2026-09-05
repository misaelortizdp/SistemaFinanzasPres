import { FormEvent, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Pencil, Trash2, Plus } from "lucide-react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { useCuentas, formatoMoneda, Cuenta } from "@/lib/hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useConfirm } from "@/lib/useConfirm";
import { Skeleton } from "@/components/ui/skeleton";
import { obtenerMensajeError, validaciones } from "@/lib/errorUtils";

export default function PaginaCuentas() {
  const cliente = useQueryClient();
  const { confirm, ConfirmDialog } = useConfirm();
  const { data: cuentas = [], isLoading } = useCuentas();
  const [editando, setEditando] = useState<Cuenta | null>(null);
  const [nombre, setNombre] = useState("");
  const [saldo, setSaldo] = useState("0");
  const [errores, setErrores] = useState<Record<string, string>>({});

  function limpiar() { 
    setEditando(null); 
    setNombre(""); 
    setSaldo("0"); 
    setErrores({});
  }
  
  function editar(c: Cuenta) { 
    setEditando(c); 
    setNombre(c.nombre); 
    setSaldo(String(c.saldo));
    setErrores({});
  }
  
  // Validar campo individual
  function validarCampo(campo: string, valor: any): string | null {
    switch (campo) {
      case "nombre":
        return validaciones.requerido(valor, "nombre") || validaciones.longitudMinima(valor, 2, "nombre");
      case "saldo":
        return validaciones.montoValido(valor, "saldo");
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
      const cuerpo = { nombre, saldo: parseFloat(saldo) || 0, orden: editando?.orden ?? 99, activa: true };
      if (editando) await api.put(`/api/cuentas/${editando.id}`, { id: editando.id, ...cuerpo });
      else await api.post("/api/cuentas", cuerpo);
    },
    onMutate: async () => {
      // Cancelar queries pendientes
      await cliente.cancelQueries({ queryKey: ["cuentas"] });
      
      // Snapshot del estado anterior
      const anterior = cliente.getQueryData(["cuentas"]);
      
      // Actualización optimista
      if (editando) {
        cliente.setQueryData(["cuentas"], (old: Cuenta[] = []) =>
          old.map(c => c.id === editando.id ? { ...c, nombre, saldo: parseFloat(saldo) || 0 } : c)
        );
      }
      
      return { anterior };
    },
    onSuccess: () => { 
      cliente.invalidateQueries({ queryKey: ["cuentas"] }); 
      toast.success(editando ? "Cuenta actualizada" : "Cuenta creada con éxito");
      limpiar(); 
    },
    onError: (error, _variables, context) => {
      // Revertir al estado anterior en caso de error
      if (context?.anterior) {
        cliente.setQueryData(["cuentas"], context.anterior);
      }
      const mensaje = obtenerMensajeError(error, "No se pudo guardar la cuenta");
      toast.error(mensaje);
    },
  });

  const eliminar = useMutation({
    mutationFn: async (id: number) => { await api.delete(`/api/cuentas/${id}`); },
    onMutate: async (id: number) => {
      await cliente.cancelQueries({ queryKey: ["cuentas"] });
      const anterior = cliente.getQueryData(["cuentas"]);
      
      // Actualización optimista: eliminar de la lista
      cliente.setQueryData(["cuentas"], (old: Cuenta[] = []) => old.filter(c => c.id !== id));
      
      return { anterior };
    },
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: ["cuentas"] });
      toast.success("Cuenta eliminada");
    },
    onError: (error, _variables, context) => {
      if (context?.anterior) {
        cliente.setQueryData(["cuentas"], context.anterior);
      }
      const mensaje = obtenerMensajeError(error, "No se pudo eliminar la cuenta");
      toast.error(mensaje);
    },
  });

  function enviar(e: FormEvent) { 
    e.preventDefault(); 
    
    // Validar todos los campos
    const nuevosErrores: Record<string, string> = {};
    
    const errorNombre = validarCampo("nombre", nombre);
    if (errorNombre) nuevosErrores.nombre = errorNombre;
    
    const errorSaldo = validarCampo("saldo", saldo);
    if (errorSaldo) nuevosErrores.saldo = errorSaldo;
    
    if (Object.keys(nuevosErrores).length > 0) {
      setErrores(nuevosErrores);
      toast.error("Por favor, corrige los errores del formulario");
      return;
    }
    
    guardar.mutate(); 
  }

  const total = cuentas.reduce((s, c) => s + c.saldo, 0);

  return (
    <div className="space-y-6">
      <ConfirmDialog />
      <p className="text-muted-foreground">Saldo total: <span className="font-semibold">{formatoMoneda(total)}</span></p>

      <Card>
        <CardHeader><CardTitle>{editando ? `Editar: ${editando.nombre}` : "Nueva cuenta"}</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={enviar} className="space-y-3">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="n">Nombre</Label>
                <Input 
                  id="n" 
                  value={nombre} 
                  onChange={(e) => manejarCampo("nombre", e.target.value, setNombre)} 
                  placeholder="Cuenta bancaria, Efectivo, ..." 
                  className={`h-11 ${errores.nombre ? "border-red-500" : ""}`} 
                />
                {errores.nombre && <p className="text-xs text-red-600">{errores.nombre}</p>}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="s">Saldo</Label>
                <Input 
                  id="s" 
                  type="number" 
                  step="0.01" 
                  value={saldo} 
                  onChange={(e) => manejarCampo("saldo", e.target.value, setSaldo)} 
                  className={`h-11 ${errores.saldo ? "border-red-500" : ""}`} 
                />
                {errores.saldo && <p className="text-xs text-red-600">{errores.saldo}</p>}
              </div>
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
        <CardHeader><CardTitle>Tus cuentas ({cuentas.length})</CardTitle></CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="py-3 flex items-center justify-between">
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-4 w-20" />
                  </div>
                  <div className="flex gap-1">
                    <Skeleton className="h-9 w-9 rounded" />
                    <Skeleton className="h-9 w-9 rounded" />
                  </div>
                </div>
              ))}
            </div>
          ) : cuentas.length === 0 ? <p className="text-muted-foreground">Sin cuentas.</p> :
           <ul className="divide-y">
             {cuentas.map((c) => (
               <li key={c.id} className="py-3 flex items-center justify-between">
                 <div>
                   <p className="font-medium">{c.nombre}</p>
                   <p className={`text-sm font-semibold ${c.saldo >= 0 ? "text-emerald-600" : "text-red-600"}`}>
                     {formatoMoneda(c.saldo)}
                   </p>
                 </div>
                <div className="flex gap-1 shrink-0">
                  <Button size="icon" variant="ghost" onClick={() => editar(c)} className="h-11 w-11"><Pencil className="w-4 h-4" /></Button>
                   <Button size="icon" variant="ghost" onClick={async () => {
                     const confirmado = await confirm({
                       title: "¿Eliminar cuenta?",
                       description: `¿Estás seguro de que deseas eliminar la cuenta "${c.nombre}"? Esta acción no se puede deshacer.`,
                       confirmText: "Eliminar",
                       cancelText: "Cancelar",
                       variant: "destructive"
                     });
                     if (confirmado) eliminar.mutate(c.id);
                  }} className="h-11 w-11">
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
