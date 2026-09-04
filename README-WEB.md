# Sistema Finanzas — Versión Web (monorepo)

Este repo contiene tres proyectos:

| Carpeta | Qué es | Estado |
|---|---|---|
| `src/SistemaFinanzasPres/` | App **MAUI** (Android/Windows) | En mantenimiento — no tiene las features agregadas durante la auditoría de paridad (ver abajo) |
| `src/SistemaFinanzasPres.Api/` | **Backend** .NET 9 Web API + EF Core + Identity + JWT | En producción |
| `web/` | **Frontend** Vite + React + TS + Tailwind + shadcn/ui | En producción |

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

## Estado actual

Las 4 fases originales (esqueleto, entidades, páginas, deploy) están completas, y después
se hizo una auditoría de paridad contra la app móvil que cerró 14 gaps de features. Módulos
implementados end-to-end (API + página web), todos filtrados por usuario/multitenant:

- ✅ Autenticación: registro, login JWT, usuario actual, rutas protegidas + interceptor de Axios
- ✅ Categorías: CRUD, agrupadas por pilar (Necesidad / Deseo / Ahorro / Ingreso), activar/desactivar, guard de borrado si tienen movimientos
- ✅ Cuentas, Movimientos (gastos + ingresos unificados, con búsqueda y filtros), Ingresos, Presupuesto (por pilares, editable, con estados 🟢🟡🔴⬜)
- ✅ Deudas: CRUD + historial de pagos + simulador Avalancha/Bola de nieve con pago extra opcional
- ✅ Metas de ahorro: CRUD, proyección de meses para alcanzarla, aporte mensual recomendado
- ✅ Patrimonio: snapshots mensuales automáticos + balance histórico
- ✅ Tendencias: tabla de 6 meses con flechas de variación y mejor/peor mes
- ✅ Configuración: salario, otros ingresos, % diezmo con calculadora en vivo, metas % por pilar, meses de fondo de emergencia
- ✅ Panel (dashboard): ingreso disponible post-diezmo, proyección de gasto (ritmo diario / quema diaria), distribución 50/30/20, tasa de ahorro, barra de fondo de emergencia y deuda total — los KPIs que antes vivían en una página aparte (`/kpis`) ahora están integrados aquí

Deploy: API en Fly.io + Frontend en Vercel + Postgres en Neon — ver [`DEPLOY.md`](DEPLOY.md).

## Pendientes conocidos

- No hay suite de tests automatizados (ni backend ni frontend).
- No hay CI configurado (sin workflows en `.github/`).
- La app MAUI no recibió las 14 features de la auditoría de paridad — solo vive en la web.
- El bundle de producción del frontend pasa de 500kB; candidato a code-splitting si crece más.
