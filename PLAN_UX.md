# Plan de Mejoras UX - SistemaFinanzasPres

## 📊 Estado General
- **Plataforma prioritaria**: Web (React + TypeScript)
- **Framework**: React 18 + Vite + Tailwind + shadcn/ui
- **Backend**: API .NET 9 + EF Core + JWT
- **Fase actual**: Fase 1 - Mejoras UX Críticas

## 🎯 Objetivo
Mejorar la experiencia de usuario de la aplicación web de finanzas personales, implementando mejoras en 7 fases priorizadas.

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

### ⏳ LOTE 3: Mejoras Responsive Mobile-First
**Prioridad**: CRÍTICA | **Fase**: 1.1
**Objetivo**: Optimizar experiencia móvil

**Tareas**:
- [ ] Dashboard responsive:
  - [ ] Convertir tarjetas a columna única en móvil
  - [ ] Hacer secciones colapsables
  - [ ] Añadir indicadores de tendencia (↑↓)
- [ ] Tablas responsive:
  - [ ] Scroll horizontal con columnas fijas
  - [ ] Vista de tarjetas alternativa en móvil
- [ ] Optimizar formularios:
  - [ ] Layouts progresivos de una columna
  - [ ] Aumentar tamaño táctil (min 44x44px)
- [ ] Mejorar BottomNav existente
- [ ] Pull-to-refresh en listas

### ⏳ LOTE 4: Manejo de Errores Mejorado
**Prioridad**: CRÍTICA | **Fase**: 1.3
**Objetivo**: Feedback claro y accionable

**Tareas**:
- [ ] Mensajes de error específicos por tipo
- [ ] Implementar error boundaries
- [ ] Validación inline en formularios
- [ ] Mecanismos de reintento para API calls
- [ ] Estados de error con acciones sugeridas

### ⏳ LOTE 5: Onboarding Básico
**Prioridad**: ALTA | **Fase**: 1.4
**Objetivo**: Guiar a usuarios nuevos

**Tareas**:
- [ ] Crear wizard de bienvenida:
  - [ ] Paso 1: Configuración de perfil
  - [ ] Paso 2: Porcentajes 50/30/20
  - [ ] Paso 3: Categorías
  - [ ] Paso 4: Primera cuenta
  - [ ] Paso 5: Tour del dashboard
- [ ] Estados vacíos con CTAs en todas las páginas
- [ ] Sistema de tooltips contextuales
- [ ] Indicadores de ayuda (?)

### ⏳ LOTE 6: Mejoras al Dashboard
**Prioridad**: MEDIA | **Fase**: 2.1
**Objetivo**: Dashboard más escaneable y personalizable

**Tareas**:
- [ ] Jerarquía visual mejorada
- [ ] Secciones colapsables/expandibles
- [ ] Indicadores de tendencia vs mes anterior
- [ ] Botón de acción flotante (FAB)
- [ ] Widgets personalizables
- [ ] Modo oscuro

### ⏳ LOTE 7: Entrada Rápida de Gastos
**Prioridad**: MEDIA | **Fase**: 3.1
**Objetivo**: Reducir fricción al agregar gastos

**Tareas**:
- [ ] Modal de gasto rápido
- [ ] Pre-llenado inteligente
- [ ] Categorías recientes primero
- [ ] Atajos de teclado
- [ ] Función duplicar transacción
- [ ] Plantillas de gastos recurrentes

### ⏳ LOTE 8: Búsqueda y Filtrado Avanzado
**Prioridad**: MEDIA | **Fase**: 3.3
**Objetivo**: Encontrar información rápidamente

**Tareas**:
- [ ] Selector de rango de fechas
- [ ] Filtro multi-categoría
- [ ] Filtro de rango de monto
- [ ] Guardar presets de filtro
- [ ] Búsqueda global

### ⏳ LOTE 9: Completar Páginas Faltantes
**Prioridad**: ALTA | **Fase**: 5.1
**Objetivo**: Paridad funcional web-MAUI

**Tareas**:
- [ ] PaginaDeudas: Implementación completa
- [ ] PaginaMetas: Implementación completa
- [ ] PaginaPatrimonio: Desglose activos/pasivos
- [ ] PaginaTendencias: Gráficas avanzadas

### ⏳ LOTE 10: PWA y Soporte Offline
**Prioridad**: MEDIA | **Fase**: 5.2
**Objetivo**: Funcionalidad offline

**Tareas**:
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

---

**Última actualización**: 2026-07-21  
**Lote completado**: LOTE 2 ✅  
**Lote actual**: LOTE 3 (Mejoras Responsive Mobile-First)  
**Progreso general**: 2/10 lotes (20%)
