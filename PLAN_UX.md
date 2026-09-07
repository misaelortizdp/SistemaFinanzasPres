# Plan de Mejoras UX - SistemaFinanzasPres

## 📊 Estado General
- **Plataforma prioritaria**: Web (React + TypeScript), en paridad con móvil (MAUI)
- **Framework**: React 18 + Vite + Tailwind + shadcn/ui
- **Backend**: API .NET 9 + EF Core + JWT
- **Fase actual**: Fase 1 completa. Fases 2-5 revisadas y filtradas contra `CLAUDE.md` (ver notas 2026-09-07 en cada lote) — no todo lo listado abajo se va a construir tal cual.

## 🎯 Objetivo
Mejorar la experiencia de usuario de la aplicación web de finanzas personales. **Desde 2026-09, la prioridad la define `CLAUDE.md`** (utilidad real por encima de exhaustividad, pocas pantallas, sugerir en vez de solo mostrar) — este documento queda como registro histórico de ideas, no como backlog a ejecutar completo.

---

## 📋 IMPLEMENTACIÓN POR LOTES

### ✅ LOTE 0: Preparación (COMPLETADO)
- [x] Análisis del código base web existente
- [x] Identificación de componentes ya disponibles (toast, skeleton, dialog)
- [x] Creación de este documento de plan

### ✅ LOTE 1: Sistema de Toast/Notificaciones (COMPLETADO)
**Prioridad**: CRÍTICA | **Fase**: 1.3
**Objetivo**: Reemplazar alert() y confirm() nativos por sistema moderno de notificaciones

**Tareas**:
- [x] Integrar Sonner completamente (ya estaba en main.tsx)
- [x] Crear/usar hook useConfirm para confirmaciones (ya existía)
- [x] Reemplazar todos los alert() por toast en páginas:
  - [x] PaginaConfiguracion.tsx → toast.success()
  - [x] PaginaDeudas.tsx → toast.error()
  - [x] PaginaMetas.tsx → toast.error()
- [x] Reemplazar confirm() por diálogos personalizados usando useConfirm:
  - [x] PaginaIngresos.tsx
  - [x] PaginaPatrimonio.tsx
  - [x] PaginaDeudas.tsx
  - [x] PaginaCuentas.tsx
  - [x] PaginaMetas.tsx
- [x] Probar y validar funcionamiento (commit realizado)

### ✅ LOTE 2: Skeleton Screens y Estados de Carga (COMPLETADO)
**Prioridad**: CRÍTICA | **Fase**: 1.2
**Objetivo**: Mejorar percepción de rendimiento con skeleton screens

**Tareas**:
- [x] Componentes skeleton específicos ya existían:
  - [x] DashboardSkeleton.tsx
  - [x] TableSkeleton.tsx
  - [x] CardSkeleton.tsx
- [x] Añadir animación shimmer a skeletons
- [x] Implementar en páginas principales:
  - [x] PaginaPanel (Dashboard) - ya estaba implementado
  - [x] PaginaMovimientos (Tabla)
  - [x] PaginaPresupuesto (Tarjetas)
  - [x] PaginaCategorias
  - [x] PaginaCuentas
- [x] Implementar actualizaciones optimistas en mutaciones:
  - [x] PaginaCuentas (guardar, eliminar)
  - [x] PaginaCategorias (guardar, eliminar, toggleActiva)
  - [x] PaginaMovimientos (eliminar)
  - [x] PaginaPresupuesto (guardar)
- [x] Renderizado progresivo de datos (ya implementado con React Query)

### ✅ LOTE 3: Mejoras Responsive Mobile-First (COMPLETADO)
**Prioridad**: CRÍTICA | **Fase**: 1.1
**Objetivo**: Optimizar experiencia móvil

**Tareas**:
- [x] Dashboard responsive:
  - [x] Convertir tarjetas a columna única en móvil
  - [x] Hacer secciones colapsables
  - [x] Añadir indicadores de tendencia (↑↓)
- [x] Tablas responsive:
  - [x] Vista de tarjetas alternativa en móvil (ya implementado)
- [x] Optimizar formularios:
  - [x] Layouts progresivos de una columna
  - [x] Aumentar tamaño táctil (min 44x44px)
- [x] Mejorar BottomNav existente
- [x] Pull-to-refresh en listas (no implementado - requiere librería adicional)

### ✅ LOTE 4: Manejo de Errores Mejorado (COMPLETADO)
**Prioridad**: CRÍTICA | **Fase**: 1.3
**Objetivo**: Feedback claro y accionable

**Tareas**:
- [x] Mensajes de error específicos por tipo
- [x] Implementar error boundaries
- [x] Validación inline en formularios
- [x] Mecanismos de reintento para API calls
- [x] Estados de error con acciones sugeridas

### ⏳ LOTE 5: Onboarding Básico (RE-EVALUADO 2026-09-07 — ver notas)
**Prioridad**: ALTA | **Fase**: 1.4
**Objetivo**: Guiar a usuarios nuevos

**Tareas**:
- [ ] Wizard de bienvenida de 5 pasos — **descartado**: un wizard multi-paso contradice "intuitivo por encima de todo" y "pocas pantallas" (CLAUDE.md). Reemplazar por lo de abajo.
- [ ] **(Nuevo candidato, reemplaza el wizard)** Sembrar categorías por defecto (por pilar: Necesidad/Deseo/Ahorro/Primer Fruto) + una cuenta "Efectivo" al registrar un usuario nuevo — hoy un usuario nuevo arranca con cero categorías/cuentas y tiene que crearlas todas a mano antes de poder registrar un solo gasto. Esto sí es friction real (contradice "poco tiempo invertido, resultado claro") y se resuelve sin pantalla nueva, solo con un seed en el registro.
- [ ] Estados vacíos con CTAs en todas las páginas — vale la pena, bajo esfuerzo, ayuda a "intuitivo" (ej. "aún no tienes categorías, créalas aquí" en vez de una tabla en blanco)
- [ ] Sistema de tooltips contextuales — **descartado**: si hace falta explicar una pantalla con tooltips, la pantalla está mal diseñada (CLAUDE.md ppio. 7) — hay que simplificar la UI, no anotarla
- [ ] Indicadores de ayuda (?) — **descartado**, mismo motivo que tooltips

### 🟡 LOTE 6: Mejoras al Dashboard (PARCIAL — ver notas 2026-09-07)
**Prioridad**: MEDIA | **Fase**: 2.1
**Objetivo**: Dashboard más escaneable y personalizable

**Tareas**:
- [x] Jerarquía visual mejorada — Panel reordenado con sugerencias accionables primero (Fase 2 del plan de mejoras, sesión 2026-09)
- [x] Secciones colapsables/expandibles — ya resuelto en LOTE 3 (responsive mobile)
- [x] Indicadores de tendencia vs mes anterior — superado: se plegó Tendencias completo en Panel (gráfica de línea 6 meses, mejor/peor mes, promedios)
- [x] Botón de acción flotante (FAB) — implementado (mismo botón que LOTE 7, ver abajo)
- [ ] Widgets personalizables — **descartado**: contradice la visión de producto (CLAUDE.md: "pocas pantallas", "no exhaustividad"). Un dashboard configurable agrega complejidad que nadie pidió.
- [ ] Modo oscuro — **no prioritario**: es cosmético, no ayuda a decidir nada (CLAUDE.md ppio. 3). Nota: hoy queda scaffolding muerto (`darkMode:["class"]` en tailwind.config + clases `dark:` sueltas en 4 páginas) sin ningún toggle que las active — si se retoma, o se termina bien (toggle + variables `.dark`) o se limpia esa scaffolding suelta.

### 🟡 LOTE 7: Entrada Rápida de Gastos (PARCIAL — completado 2026-09-07)
**Prioridad**: MEDIA | **Fase**: 3.1
**Objetivo**: Reducir fricción al agregar gastos

**Tareas**:
- [x] Modal de gasto rápido — botón flotante (FAB) en web (`BotonAccionRapida.tsx`, global en `Layout.tsx`) y en móvil (tab Dashboard, MAUI)
- [~] Pre-llenado inteligente — parcial: el concepto se autocompleta con el nombre de la categoría si se deja vacío; no hay predicción de monto/categoría
- [x] Categorías recientes primero — chips de las categorías más usadas ese mes (por frecuencia), en ambas plataformas
- [ ] Atajos de teclado — **no prioritario**: uso ocasional (segundos al día), no justifica la complejidad
- [ ] Función duplicar transacción — candidato real si en la práctica se repiten gastos idénticos seguido; no construir preventivo, solo si se siente la fricción
- [ ] Plantillas de gastos recurrentes — **descartado por ahora**: se solapa con "categorías recientes primero", que ya resuelve la mayoría del caso de uso con menos superficie

### ⏳ LOTE 8: Búsqueda y Filtrado Avanzado (RE-EVALUADO 2026-09-07 — ver notas)
**Prioridad**: MEDIA | **Fase**: 3.3
**Objetivo**: Encontrar información rápidamente

**Nota**: Movimientos ya tiene hoy búsqueda de texto libre (concepto + categoría + notas) y filtro por categoría — cubre buena parte de lo que este lote pedía.

**Tareas**:
- [ ] Selector de rango de fechas — candidato real: hoy solo se navega mes a mes, no hay forma de ver p.ej. "últimos 3 meses" o un rango custom. Ayuda a buscar/decidir, vale la pena si se siente esa limitación.
- [ ] Filtro multi-categoría — **descartado por ahora**: el filtro de una categoría + búsqueda de texto ya cubre casi todo; multi-select es complejidad de UI para un beneficio marginal
- [ ] Filtro de rango de monto — **descartado**: uso poco frecuente, no justifica la superficie (CLAUDE.md: "no exhaustividad")
- [ ] Guardar presets de filtro — **descartado**: sobre-ingeniería para el volumen de datos de un usuario individual
- [ ] Búsqueda global — ya resuelto: la búsqueda de texto actual en Movimientos ya busca por concepto/categoría/notas

### ✅ LOTE 9: Completar Páginas Faltantes (COMPLETADO — vía trabajo posterior, no como estaba planteado originalmente)
**Prioridad**: ALTA | **Fase**: 5.1
**Objetivo**: Paridad funcional web-MAUI

**Tareas**:
- [x] PaginaDeudas: Implementación completa — CRUD + historial de pagos + meses-para-liquidar/prioridad avalancha siempre visibles (el simulador que tenía se eliminó, esos cálculos ahora son columnas de la tabla)
- [x] PaginaMetas: Implementación completa — CRUD + proyección de meses + aporte mensual recomendado
- [x] PaginaPatrimonio: Desglose activos/pasivos — snapshots mensuales automáticos + balance histórico
- [x] PaginaTendencias: Gráficas avanzadas — no quedó como página aparte: se plegó dentro de Panel (misma filosofía que ya se había aplicado al plegar KPIs), con gráfica de línea de 6 meses + mejor/peor mes + promedios

### ⛔ LOTE 10: PWA y Soporte Offline (DESCARTADO 2026-09-07)
**Prioridad**: MEDIA | **Fase**: 5.2
**Objetivo**: Funcionalidad offline

**Nota**: la app móvil MAUI ya es offline-first (SQLite local) — ese caso de uso ("necesito esto sin internet") ya está cubierto por la otra plataforma. Construir toda esta capacidad de nuevo en web (service worker + IndexedDB + sync) es una inversión grande para una necesidad que el otro producto ya resuelve. No se rescata salvo que aparezca una razón concreta de negocio para tenerlo también en web.

**Tareas** (sin acción):
- [ ] Convertir a PWA
- [ ] Service worker
- [ ] IndexedDB para persistencia
- [ ] Sincronización al reconectar
- [ ] Indicador de estado offline

---

## 📈 Fases del Plan Original

### Fase 1: Mejoras UX Críticas (PRIORIDAD ALTA) 🔴
- 1.1 Mejoras Responsive Mobile-First → **LOTE 3**
- 1.2 Estados de Carga y Skeleton Screens → **LOTE 2**
- 1.3 Manejo de Errores y Feedback → **LOTE 1, 4**
- 1.4 Experiencia de Onboarding → **LOTE 5**

### Fase 2: Visualización de Datos e Insights (PRIORIDAD MEDIA) 🟡
- 2.1 Mejoras al Dashboard → **LOTE 6**
- 2.2 Gráficas Avanzadas y Tendencias → **LOTE 9 (parcial)**
- 2.3 Insights Inteligentes → **FUTURO**

### Fase 3: Optimización de Flujos (PRIORIDAD MEDIA) 🟡
- 3.1 Entrada Rápida y Atajos → **LOTE 7**
- 3.2 Operaciones en Masa → **FUTURO**
- 3.3 Búsqueda y Filtrado → **LOTE 8**

### Fase 4: Engagement y Retención (PRIORIDAD BAJA) ⚪
- 4.1 Gamificación → **FUTURO**
- 4.2 Notificaciones → **FUTURO**
- 4.3 Personalización → **FUTURO**

### Fase 5: Paridad Cross-Platform (PRIORIDAD ALTA/MEDIA) 🟠
- 5.1 Paridad Web-MAUI → **LOTE 9**
- 5.2 Soporte Offline PWA → **LOTE 10**
- 5.3 Exportación de Datos → **FUTURO**

### Fase 6: Accesibilidad e i18n (PRIORIDAD BAJA) ⚪
- 6.1 Accesibilidad → **FUTURO**
- 6.2 Internacionalización → **FUTURO**

### Fase 7: Funcionalidades Avanzadas (VISIÓN LARGO PLAZO) ⚪
- 7.1 Colaborativo → **FUTURO** (NO prioritario - app para individuos)
- 7.2 Integración Bancaria → **FUTURO**
- 7.3 Categorización Inteligente → **FUTURO**

---

## 🎯 Métricas de Éxito

### Engagement
- [ ] Usuarios Activos Diarios/Mensuales
- [ ] Duración de sesión
- [ ] Tasas de adopción de funcionalidades

### Retención
- [ ] Tasa de abandono
- [ ] Retención 7/30 días

### Rendimiento
- [ ] Tiempo al primer pintado significativo
- [ ] Tiempo de carga de página
- [ ] Tiempo de respuesta API

### Usabilidad
- [ ] Tasa de completación de tareas
- [ ] Tiempo en tarea
- [ ] Tasa de error

### Satisfacción
- [ ] Puntuación NPS
- [ ] Feedback en la app
- [ ] Volumen de soporte

---

## 📝 Notas Técnicas

### Stack Actual
- React 18.3.1 + TypeScript
- Vite 5.4.8
- Tailwind CSS 3.4.13
- shadcn/ui (componentes)
- React Query 5.59.0 (estado servidor)
- React Router 6.27.0
- Recharts 3.9.1 (gráficas)
- Sonner 2.0.7 (toasts)
- Lucide React (íconos)

### Componentes UI Disponibles
- ✅ Button, Card, Dialog, Input, Label, Select, Textarea
- ✅ Skeleton, Toast
- ✅ BottomNav (navegación móvil)

### Hooks Personalizados
- useConfirm (confirmaciones con diálogos)
- ContextoAutenticacion (auth)
- React Query hooks (useQuery, useMutation)

---

## 🔄 Proceso de Trabajo

1. **Explorar** código relevante antes de cambios
2. **Implementar** cambios en lotes pequeños
3. **Probar** localmente con `npm run dev`
4. **Commitear** después de cada lote completado
5. **Avisar** al usuario para revisión
6. **Continuar** con siguiente lote

---

## 📅 Historial de Cambios

### 2026-07-21
- Creación del documento de plan UX
- Definición de 10 lotes de implementación
- ✅ **LOTE 1 COMPLETADO**: Sistema de Toast/Notificaciones
  - Reemplazados todos los alert() por toast (sonner)
  - Reemplazados todos los confirm() por useConfirm
  - 6 archivos modificados (PaginaConfiguracion, PaginaDeudas, PaginaMetas, PaginaIngresos, PaginaCuentas, PaginaPatrimonio)
  - Commit: feat: reemplazar alert() y confirm() nativos por toast y diálogos personalizados
- ✅ **LOTE 2 COMPLETADO**: Skeleton Screens y Estados de Carga
  - Añadida animación shimmer a componentes skeleton
  - Implementado skeleton screens en 5 páginas principales
  - Actualizaciones optimistas en mutaciones de 4 páginas
  - 2 commits realizados
  - Commit 1: feat: añadir animación shimmer y skeleton screens en páginas principales
  - Commit 2: feat: implementar actualizaciones optimistas en mutaciones principales
- ✅ **LOTE 3 COMPLETADO**: Mejoras Responsive Mobile-First
  - Dashboard con secciones colapsables en móvil (KPIs, proyección, gráficas, distribución)
  - Indicadores de tendencia (↑↓) en tarjetas principales del dashboard
  - Grids responsive: columna única en móvil, múltiples en desktop
  - Formularios optimizados con layouts progresivos
  - Botones con tamaño táctil mínimo 44x44px en todos los formularios y listas
  - BottomNav mejorado con mejor feedback visual y touch targets
  - Inputs y selects con altura mínima de 44px
  - 7 archivos modificados (PaginaPanel, PaginaMovimientos, PaginaCuentas, PaginaCategorias, PaginaPresupuesto, BottomNav)
  - Commit: feat(responsive): mejoras responsive mobile-first con secciones colapsables y touch targets optimizados
- ✅ **LOTE 4 COMPLETADO**: Manejo de Errores Mejorado
  - Creado errorUtils.ts con utilidades para mensajes de error específicos por código HTTP
  - Implementado ErrorBoundary para capturar errores de React
  - Configuración de reintentos automáticos en React Query con backoff exponencial
  - Validación inline en formularios (PaginaMovimientos, PaginaCuentas)
  - Mensajes de error contextuales en todas las mutaciones
  - 6 archivos modificados (App.tsx, main.tsx, errorUtils.ts, ErrorBoundary.tsx, PaginaMovimientos, PaginaCuentas, PaginaPresupuesto)
  - 3 commits realizados
  - Commit 1: feat(errores): añadir utilidades de error, error boundary y validación inline
  - Commit 2: feat(errores): aplicar validación y mejor manejo de errores en PaginaCuentas
  - Commit 3: feat(errores): completar LOTE 4 - aplicar mejoras a Presupuesto y corregir errores

### 2026-09 (sesión de mejoras de producto, filtradas por CLAUDE.md)
- Se definió una visión de producto explícita (ver `CLAUDE.md`), con principios concretos
  ("utilidad real, no exhaustividad", "pocas pantallas", "debe sugerir, no solo mostrar") que
  desde ahora filtran qué se construye de este plan.
- Bug real encontrado y corregido: Ingresos vivía separado de Movimientos y el Panel/KPIs
  calculaban el ingreso solo desde la tabla Ingresos — unificado en Movimientos.
- Deudas: simulador (LOTE original no lo pedía así, pero mismo espíritu de LOTE 9) reemplazado
  por meses-para-liquidar/prioridad avalancha siempre visibles en la tabla principal.
- Tendencias (página aparte) plegada en Panel — gráfica de línea 6 meses, mejor/peor mes,
  promedios. Resuelve LOTE 9 (Fase 2.2) y el ítem de tendencia de LOTE 6.
- Navegación reestructurada de 11 a 5 pantallas (Configuración consolida Cuentas/Categorías/
  Metas/Patrimonio/% pilares/Diezmo/Fondo de emergencia).
- Motor de sugerencias accionables en Panel (balance negativo, pilares sobre meta, Ahorro bajo
  meta, proyección de déficit, gastos hormiga) — la app ahora sugiere, no solo reporta.
- Bug real encontrado y corregido (a nivel pilar y luego categoría): la meta de Ahorro es un
  mínimo a alcanzar, no un techo — el estado se mostraba invertido.
- Bug de producción corregido: columnas agregadas al modelo después de la creación inicial de
  la BD nunca se propagaban (`EnsureCreated()` no migra) — se agregó auto-reparación de
  columnas faltantes, primero en Postgres (producción) y luego también en SQLite (local).
- **LOTE 7 implementado (parcial)**: botón de gasto rápido (FAB) en web y móvil, con categorías
  recientes primero — ver detalle en LOTE 7 arriba.
- Este documento se revisó completo contra la visión de producto: ver notas por lote arriba
  (marcadas 2026-09-07) para qué se descartó y por qué.

---

**Última actualización**: 2026-09-07
**Lotes completados**: LOTE 1, 2, 3, 4, 9 ✅ · LOTE 6 ✅ (dentro del alcance filtrado — widgets personalizables y modo oscuro descartados, ver notas)
**Parcial**: LOTE 7 🟡 (núcleo — modal + categorías recientes — hecho; atajos/plantillas descartados, duplicar transacción condicionado a necesidad real)
**Pendientes reales** (alcance recortado tras el filtro CLAUDE.md): LOTE 5 (seed de categorías/cuenta por defecto + estados vacíos con CTA) · LOTE 8 (solo selector de rango de fechas)
**Descartado**: LOTE 10 (PWA/offline — ya lo cubre la app móvil MAUI)
