# SistemaFinanzasPres

App de **finanzas personales multiplataforma** (Android + Windows) basada en el
framework **50/30/20** + un "Primer Fruto" (diezmo/donación) opcional pre-descontado
del ingreso. iOS/macOS son agregables después si llegas a tener un Mac.

Stack:
- **.NET 9 + .NET MAUI** (un solo código C#/XAML para móvil y escritorio)
- **Entity Framework Core + SQLite** local en el dispositivo
- **CommunityToolkit.Mvvm** (MVVM con `ObservableObject`/`RelayCommand`)

## Funcionalidades v1

- Dashboard mensual: ingreso, gastado, disponible, tasa de ahorro y resumen 50/30/20.
- **Gastos** (CRUD): registro por fecha, concepto, categoría, cuenta y notas. Búsqueda + filtros.
- **Presupuesto mensual** editable por categoría con estados (🟢 OK, 🟡 Alerta, 🔴 Excedido, ⬜ Sin gastos).
- **Categorías** dinámicas: crear, renombrar, cambiar de pilar, activar/desactivar, eliminar.
- **Cuentas**: efectivo, banco, tarjeta, etc. con saldos editables.
- **KPIs**: tasa de ahorro, % necesidades, % deseos, gastos hormiga, fondo de emergencia.
- **Configuración**: salario, otros ingresos, metas % por pilar, meses de fondo de emergencia.
- Navegación entre meses (◀ ▶) y "Copiar mes anterior" para no reescribir presupuestos.

Todo es **dinámico** — los pilares (Primer Fruto / Necesidad / Deseo / Ahorro) están fijos
porque son el framework, pero las categorías, montos, cuentas, salario y porcentajes los
defines tú desde la app.

## Cómo correrlo

### Requisitos
- **.NET 9 SDK**
- Workload de MAUI:
  ```
  dotnet workload install maui
  ```
- Android: Android SDK 24+ (lo instala Visual Studio o `dotnet workload`).
- Windows: Visual Studio 2022 con cargas "Desarrollo de .NET Multi-platform App UI" + "Desarrollo de escritorio para Windows".

### Restaurar y compilar

```bash
dotnet restore
dotnet build src/SistemaFinanzasPres/SistemaFinanzasPres.csproj -f net9.0-windows10.0.19041.0
```

### Ejecutar
- **Visual Studio 2022**: abre `SistemaFinanzasPres.sln`, elige el framework de destino y pulsa Run.
- **Android emulador** desde CLI:
  ```bash
  dotnet build -t:Run -f net9.0-android
  ```
- **Windows**: F5 en VS.

## Arquitectura

```
src/SistemaFinanzasPres/
  Models/        Entidades EF: Category, BudgetItem, Transaction, Account, AppConfig, SavingsGoal
  Data/          AppDbContext (SQLite) + DbSeeder (valores genéricos iniciales)
  Services/      BudgetService (50/30/20), KpiService, MonthService (mes activo)
  ViewModels/    MVVM con CommunityToolkit
  Views/         XAML por pantalla
  Converters/    Status→Color, Pillar→Color
  Resources/     Colores, Estilos, App icon, Splash
  Platforms/     Android, Windows
```

La base de datos se crea en `FileSystem.AppDataDirectory/finanzas.db3` la primera vez que se inicia la app.
Para reiniciar datos: desinstalar la app o borrar ese archivo.

## Roadmap (sugerencias para v1.1+)

- Gráficos (CommunityToolkit `LineChartView` o `Microcharts`).
- Importar/exportar CSV / Excel.
- Recordatorios y notificaciones de presupuesto.
- Multi-usuario / sincronización opcional con API .NET + SQL Server.
- Plantillas de gastos recurrentes (renta, suscripciones).
- Foto de recibo en el gasto.
- Backup en la nube (Google Drive / iCloud / OneDrive).
