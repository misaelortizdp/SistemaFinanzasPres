# Sistema Finanzas — Versión Web (monorepo)

Este repo contiene tres proyectos:

| Carpeta | Qué es | Estado |
|---|---|---|
| `src/SistemaFinanzasPres/` | App **MAUI** (Android/Windows) | En mantenimiento |
| `api/` | **Backend** .NET 9 Web API + EF Core + Identity + JWT | Fase 1 |
| `web/` | **Frontend** Vite + React + TS + Tailwind + shadcn/ui | Fase 1 |

## Cómo correr en local

### 1. Backend (puerto 5050)

```bash
cd src/SistemaFinanzasPres.Api
dotnet restore
dotnet run
```

- Swagger UI: http://localhost:5050/swagger
- BD por defecto: SQLite (archivo `finanzas.db` en `api/`)
- Para usar PostgreSQL edita `api/appsettings.json`:
  ```json
  "Database": {
    "Provider": "Postgres",
    "ConnectionString": "Host=localhost;Database=finanzas;Username=postgres;Password=..."
  }
  ```

### 2. Frontend (puerto 5173)

```bash
cd web
npm install
npm run dev
```

- Abre http://localhost:5173
- La URL del API se configura con `VITE_API_URL` (por defecto `http://localhost:5050`).

## Estado de la Fase 1

- ✅ Registro de usuario (POST `/api/autenticacion/registro`)
- ✅ Inicio de sesión con JWT (POST `/api/autenticacion/iniciar-sesion`)
- ✅ Obtener usuario actual (GET `/api/autenticacion/yo`)
- ✅ CRUD de Categorías filtradas por usuario (`/api/categorias`)
- ✅ Sembrado automático de 10 categorías al registrarse
- ✅ Frontend: Login, Registro, Panel, Categorías
- ✅ Rutas protegidas + interceptor de Axios para JWT

## Próximas fases

| Fase | Alcance |
|---|---|
| 2 | Endpoints para: Cuentas, Movimientos, Presupuesto, Deudas, Patrimonio, Metas |
| 3 | Páginas web equivalentes: Dashboard, Presupuesto, Movimientos, Deudas, Patrimonio, Metas, Configuración |
| 4 | Deploy: API en Render/Railway + Frontend en Vercel + Postgres en Neon |
