import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { usarAutenticacion } from "@/autenticacion/ContextoAutenticacion";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function PaginaRegistro() {
  const { registrar } = usarAutenticacion();
  const navegar = useNavigate();
  const [nombre, setNombre] = useState("");
  const [email, setEmail] = useState("");
  const [contrasena, setContrasena] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function manejarEnvio(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setEnviando(true);
    try {
      await registrar(nombre, email, contrasena);
      navegar("/", { replace: true });
    } catch (err: any) {
      const data = err?.response?.data;
      const mensaje = data?.error ?? data?.errores?.join(" · ") ?? "No pudimos crear tu cuenta.";
      setError(mensaje);
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-muted/30">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Crear cuenta</CardTitle>
          <CardDescription>Empieza a llevar control de tus finanzas hoy.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={manejarEnvio} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="nombre">Nombre</Label>
              <Input id="nombre" required autoComplete="name"
                value={nombre} onChange={(e) => setNombre(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="email">Correo</Label>
              <Input id="email" type="email" autoComplete="email" required
                value={email} onChange={(e) => setEmail(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="contrasena">Contraseña</Label>
              <Input id="contrasena" type="password" autoComplete="new-password" required minLength={8}
                value={contrasena} onChange={(e) => setContrasena(e.target.value)} />
              <p className="text-xs text-muted-foreground">Mínimo 8 caracteres, con al menos un número y una minúscula.</p>
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <Button type="submit" className="w-full" disabled={enviando}>
              {enviando ? "Creando cuenta…" : "Crear cuenta"}
            </Button>
          </form>
          <p className="text-sm text-muted-foreground mt-4 text-center">
            ¿Ya tienes cuenta?{" "}
            <Link to="/login" className="text-primary hover:underline">Inicia sesión</Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
