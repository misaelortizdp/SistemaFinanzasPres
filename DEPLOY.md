# Guía de Deploy — Fase 4

Stack gratis: **Neon (Postgres) + Fly.io (API) + Vercel (frontend)**.

Sigue los pasos en orden. Toma ~30-45 minutos la primera vez.

---

## Paso 1 — Crear base de datos en Neon

1. Ve a https://neon.tech → **Sign up** (puedes usar GitHub).
2. Crea un proyecto:
   - Nombre: `sistemafinanzaspres`
   - Postgres version: 16 (la última estable)
   - Región: la más cercana a ti (ej: `US East (Ohio)` o `AWS us-east-2`)
3. Cuando termine, vas a ver el **Connection string**. Cópialo. Se ve así:
   ```
   postgresql://usuario:password@ep-xxx-xxx.us-east-2.aws.neon.tech/neondb?sslmode=require
   ```
4. Conviértelo al formato que usa Npgsql (.NET):
   ```
   Host=ep-xxx-xxx.us-east-2.aws.neon.tech;Database=neondb;Username=usuario;Password=password;SSL Mode=Require;Trust Server Certificate=true
   ```
   Guárdalo en un editor de texto. Lo vas a usar en el Paso 2.

---

## Paso 2 — Deploy de la API en Fly.io

### 2.1 Instalar `flyctl`

En PowerShell (Windows):
```powershell
iwr https://fly.io/install.ps1 -useb | iex
```
Cierra y reabre PowerShell para que tome el PATH.

Verifica:
```powershell
flyctl version
```

### 2.2 Crear cuenta

```powershell
flyctl auth signup
```
Se abre el navegador. Termina el signup (requiere tarjeta para verificar, **no cobran si estás en tier gratis**).

### 2.3 Genera una clave JWT segura

En PowerShell:
```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { [byte](Get-Random -Min 0 -Max 256) }))
```
Copia el resultado (será un string de ~64 caracteres). Es tu `Jwt:Key` de producción.

### 2.4 Lanzar la app

⚠ Cambia primero el nombre en `src/SistemaFinanzasPres.Api/fly.toml`. El campo `app` debe ser único en todo Fly.io. Sugerencia: `finanzas-tunombre-api`.

```powershell
cd src\SistemaFinanzasPres.Api
flyctl launch --no-deploy
```
- Cuando te pregunte "Would you like to copy its configuration to the new app?" → **Yes**.
- Cuando te pregunte por base de datos / Redis → **No** (usamos Neon).
- Cuando te pregunte si quieres deployar ahora → **No** (faltan secrets).

### 2.5 Configura los secrets

Reemplaza `<...>` con los valores reales:

```powershell
flyctl secrets set `
  Database__Provider="Postgres" `
  Database__ConnectionString="Host=ep-small-morning-ajw5kf1q.c-3.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_jpDh21GcdQEe;SSL Mode=Require;Trust Server Certificate=true" `
  Jwt__Key="tsBwWIpsew0NIgjw5wvd5hyN95OyUSHb24icrUTRoR/tspuRE6pi33xAikGVPsko" `
  Jwt__Issuer="SistemaFinanzasPres" `
  Jwt__Audience="SistemaFinanzasPresClients" `
  Cors__AllowedOriginsCsv="*temporal*"
```

(De momento puedes poner `Cors__AllowedOriginsCsv="*temporal*"` y lo actualizas después del Paso 3.)

### 2.6 Deploy

```powershell
flyctl deploy
```
Tarda 2-5 minutos. Cuando termine te muestra la URL, algo como:
```
https://finanzas-tunombre-api.fly.dev
```
Visita esa URL + `/swagger` para verificar que la API responde.

---

## Paso 3 — Deploy del frontend en Vercel

1. Ve a https://vercel.com → **Sign up with GitHub**.
2. **Add New → Project** → elige el repo `sistemafinanzaspres`.
3. Configuración importante:
   - **Root Directory**: clic en **Edit** → escribe `web` → Continue.
   - Vercel detecta Vite automáticamente.
4. **Environment Variables**:
   - Key: `VITE_API_URL`
   - Value: `https://finanzas-tunombre-api.fly.dev` (la URL del paso 2.6)
5. **Deploy**. Espera 1-2 min.
6. Te da una URL: `https://sistemafinanzaspres-xyz.vercel.app`

---

## Paso 4 — Actualizar CORS en la API

Ya que tienes la URL real de Vercel, actualiza el secret:

```powershell
cd src\SistemaFinanzasPres.Api
flyctl secrets set Cors__AllowedOriginsCsv="https://sistema-finanzas-pres.vercel.app/"
```

La API se reinicia sola en ~30s.

---

## Paso 5 — Probar

1. Abre tu URL de Vercel.
2. Regístrate con un correo nuevo.
3. Si te lleva al panel y ves tus categorías sembradas → **🎉 deploy funcionó**.

---

## Costos esperados (mientras te mantengas en tier gratis)

- **Neon**: $0 mientras no superes 0.5GB. Auto-suspende tras 5min inactiva (primer query ~500ms).
- **Fly.io**: $0 con 1 máquina shared-cpu 256MB. Configurada para auto-detenerse cuando no hay tráfico.
- **Vercel**: $0 para proyectos personales.

## Comandos útiles después

```powershell
# Ver logs de la API en tiempo real
flyctl logs

# Ver estado
flyctl status

# Redeploy después de cambios en código
git push   # Vercel deploya solo
cd src\SistemaFinanzasPres.Api && flyctl deploy   # API la deployas tú
```

## Si algo falla

| Síntoma | Causa probable | Fix |
|---|---|---|
| API timeout / 502 | Máquina dormida (primer request) | Espera 30s y reintenta |
| 401 al loguearte | Jwt:Key diferente entre dev y prod | Comprueba secret en Fly |
| CORS error en navegador | URL de Vercel no en `AllowedOriginsCsv` | Actualiza secret y `flyctl deploy` |
| Postgres timeout en queries | Neon auto-suspended | El primer query la despierta, espera 1s |
| Error 500 en cualquier endpoint | Probablemente connection string mal | `flyctl logs` para ver el error exacto |
