import { Link } from "react-router-dom";
import { usarAutenticacion } from "@/autenticacion/ContextoAutenticacion";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function PaginaPanel() {
  const { usuario, cerrarSesion } = usarAutenticacion();

  return (
    <div className="container mx-auto max-w-4xl py-8 space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Hola, {usuario?.nombre} 👋</h1>
          <p className="text-muted-foreground">{usuario?.email}</p>
        </div>
        <Button variant="outline" onClick={cerrarSesion}>Cerrar sesión</Button>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Bienvenido al esqueleto web</CardTitle>
          <CardDescription>
            Versión Fase 1: autenticación + categorías funcionando end-to-end.
            En las próximas fases agregaremos presupuesto, movimientos, deudas, patrimonio y metas.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex gap-2">
          <Button asChild>
            <Link to="/categorias">🗂 Gestionar categorías</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
