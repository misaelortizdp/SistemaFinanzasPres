using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Modelos;
using SistemaFinanzasPres.Api.Servicios;

// Permite que DateTime con Kind=Unspecified llegue desde JSON sin error en Npgsql.
// Necesario porque System.Text.Json deserializa fechas como Unspecified por defecto.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos ────────────────────────────────────────────────
var proveedor = builder.Configuration["Database:Provider"] ?? "Sqlite";
var cadenaConexion = builder.Configuration["Database:ConnectionString"]
                     ?? "Data Source=finanzas.db";

builder.Services.AddDbContext<BaseDatosContexto>(opciones =>
{
    if (string.Equals(proveedor, "Postgres", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(proveedor, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        opciones.UseNpgsql(cadenaConexion);
    }
    else
    {
        opciones.UseSqlite(cadenaConexion);
    }
});

// ── Identidad ────────────────────────────────────────────────────
builder.Services.AddIdentityCore<Usuario>(opciones =>
    {
        opciones.Password.RequiredLength = 8;
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireLowercase = true;
        opciones.Password.RequireUppercase = false;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BaseDatosContexto>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ── JWT ──────────────────────────────────────────────────────────
var llaveJwt = builder.Configuration["Jwt:Key"]
               ?? throw new InvalidOperationException("Falta Jwt:Key en configuración.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(llaveJwt)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────────
// Acepta tanto array en appsettings ("Cors:AllowedOrigins") como string
// separado por comas en env var ("Cors__AllowedOriginsCsv=https://a,https://b").
var csv = builder.Configuration["Cors:AllowedOriginsCsv"];
var origenesPermitidos = !string.IsNullOrWhiteSpace(csv)
    ? csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray()
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(politica =>
    {
        politica.WithOrigins(origenesPermitidos)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

// ── Servicios propios ────────────────────────────────────────────
builder.Services.AddScoped<IServicioJwt, ServicioJwt>();

// ── Controllers + Swagger ────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo { Title = "Sistema Finanzas API", Version = "v1" });
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el JWT aquí (sin 'Bearer ').",
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Migrar BD al arrancar ────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var bd = scope.ServiceProvider.GetRequiredService<BaseDatosContexto>();
    bd.Database.EnsureCreated();
    await EnsureColumnasPostgresAsync(bd, proveedor);
    await MigrarIngresosAMovimientosAsync(bd);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// El handler de excepciones va DESPUÉS de UseCors() y re-aplica los headers
// manualmente porque UseExceptionHandler limpia la respuesta antes de escribir.
app.UseExceptionHandler(err => err.Run(async ctx =>
{
    var origin = ctx.Request.Headers["Origin"].FirstOrDefault();
    if (!string.IsNullOrEmpty(origin))
    {
        ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
        ctx.Response.Headers["Vary"] = "Origin";
        ctx.Response.Headers["Access-Control-Allow-Credentials"] = "true";
    }
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsync("{\"error\":\"Error interno del servidor.\"}");
}));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// EnsureCreated() solo crea el esquema completo si la base de datos no tiene NINGUNA
// tabla — en una base ya inicializada, agregar una columna a un modelo (ej. Deuda.AbonoExtra,
// ConfigUsuario.MetaDeudaPct) nunca se propaga sola y la columna queda faltante para siempre.
// Esto lo corrige de forma segura e idempotente: ADD COLUMN IF NOT EXISTS no falla si la
// columna ya existe, así que es seguro correrlo en cada arranque. Solo aplica a Postgres —
// SQLite local se recrea desde cero (EnsureCreated) con el modelo completo cada vez que se
// borra el archivo .db, así que nunca sufre este desfase.
static async Task EnsureColumnasPostgresAsync(BaseDatosContexto bd, string proveedor)
{
    if (!string.Equals(proveedor, "Postgres", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(proveedor, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
        return;

    string[] alteraciones =
    {
        // Categorias
        "ALTER TABLE \"Categorias\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"Categorias\" ADD COLUMN IF NOT EXISTS \"Nombre\" character varying(80) NOT NULL DEFAULT ''",
        "ALTER TABLE \"Categorias\" ADD COLUMN IF NOT EXISTS \"Tipo\" integer NOT NULL DEFAULT 1",
        "ALTER TABLE \"Categorias\" ADD COLUMN IF NOT EXISTS \"Color\" character varying(20)",
        "ALTER TABLE \"Categorias\" ADD COLUMN IF NOT EXISTS \"Icono\" character varying(20)",
        "ALTER TABLE \"Categorias\" ADD COLUMN IF NOT EXISTS \"Orden\" integer NOT NULL DEFAULT 0",
        "ALTER TABLE \"Categorias\" ADD COLUMN IF NOT EXISTS \"Activa\" boolean NOT NULL DEFAULT true",

        // Cuentas
        "ALTER TABLE \"Cuentas\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"Cuentas\" ADD COLUMN IF NOT EXISTS \"Nombre\" character varying(80) NOT NULL DEFAULT ''",
        "ALTER TABLE \"Cuentas\" ADD COLUMN IF NOT EXISTS \"Saldo\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Cuentas\" ADD COLUMN IF NOT EXISTS \"Orden\" integer NOT NULL DEFAULT 0",
        "ALTER TABLE \"Cuentas\" ADD COLUMN IF NOT EXISTS \"Activa\" boolean NOT NULL DEFAULT true",

        // Movimientos
        "ALTER TABLE \"Movimientos\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"Movimientos\" ADD COLUMN IF NOT EXISTS \"Fecha\" timestamp without time zone NOT NULL DEFAULT now()",
        "ALTER TABLE \"Movimientos\" ADD COLUMN IF NOT EXISTS \"Concepto\" character varying(200) NOT NULL DEFAULT ''",
        "ALTER TABLE \"Movimientos\" ADD COLUMN IF NOT EXISTS \"CategoriaId\" integer NOT NULL DEFAULT 0",
        "ALTER TABLE \"Movimientos\" ADD COLUMN IF NOT EXISTS \"CuentaId\" integer",
        "ALTER TABLE \"Movimientos\" ADD COLUMN IF NOT EXISTS \"Monto\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Movimientos\" ADD COLUMN IF NOT EXISTS \"Notas\" character varying(500)",

        // Ingresos (tabla legada — se mantiene solo para la migración de una sola vez)
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"Fecha\" timestamp without time zone NOT NULL DEFAULT now()",
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"Concepto\" character varying(200) NOT NULL DEFAULT ''",
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"Monto\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"Fuente\" character varying(80) NOT NULL DEFAULT 'Salario'",
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"CuentaId\" integer",
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"Notas\" character varying(500)",
        "ALTER TABLE \"Ingresos\" ADD COLUMN IF NOT EXISTS \"EsRecurrente\" boolean NOT NULL DEFAULT false",

        // LineasPresupuesto
        "ALTER TABLE \"LineasPresupuesto\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"LineasPresupuesto\" ADD COLUMN IF NOT EXISTS \"CategoriaId\" integer NOT NULL DEFAULT 0",
        "ALTER TABLE \"LineasPresupuesto\" ADD COLUMN IF NOT EXISTS \"Anio\" integer NOT NULL DEFAULT 0",
        "ALTER TABLE \"LineasPresupuesto\" ADD COLUMN IF NOT EXISTS \"Mes\" integer NOT NULL DEFAULT 0",
        "ALTER TABLE \"LineasPresupuesto\" ADD COLUMN IF NOT EXISTS \"Monto\" double precision NOT NULL DEFAULT 0",

        // Deudas
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"Nombre\" character varying(120) NOT NULL DEFAULT ''",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"MontoOriginal\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"SaldoActual\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"TasaInteres\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"PagoMinimo\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"AbonoExtra\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"DiaPago\" integer NOT NULL DEFAULT 1",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"Activa\" boolean NOT NULL DEFAULT true",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"FechaCreacion\" timestamp without time zone NOT NULL DEFAULT now()",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"Notas\" character varying(500)",
        "ALTER TABLE \"Deudas\" ADD COLUMN IF NOT EXISTS \"CategoriaId\" integer",

        // PagosDeuda
        "ALTER TABLE \"PagosDeuda\" ADD COLUMN IF NOT EXISTS \"DeudaId\" integer NOT NULL DEFAULT 0",
        "ALTER TABLE \"PagosDeuda\" ADD COLUMN IF NOT EXISTS \"Fecha\" timestamp without time zone NOT NULL DEFAULT now()",
        "ALTER TABLE \"PagosDeuda\" ADD COLUMN IF NOT EXISTS \"Monto\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"PagosDeuda\" ADD COLUMN IF NOT EXISTS \"PorcionInteres\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"PagosDeuda\" ADD COLUMN IF NOT EXISTS \"PorcionCapital\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"PagosDeuda\" ADD COLUMN IF NOT EXISTS \"Notas\" character varying(300)",

        // MetasAhorro
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"Nombre\" character varying(80) NOT NULL DEFAULT ''",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"Prioridad\" character varying(40) NOT NULL DEFAULT 'MEDIA'",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"Objetivo\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"Acumulado\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"FechaLimite\" timestamp without time zone",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"AporteMensualPlaneado\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"Activa\" boolean NOT NULL DEFAULT true",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"Notas\" character varying(500)",
        "ALTER TABLE \"MetasAhorro\" ADD COLUMN IF NOT EXISTS \"Orden\" integer NOT NULL DEFAULT 0",

        // SnapshotsPatrimoniales
        "ALTER TABLE \"SnapshotsPatrimoniales\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"SnapshotsPatrimoniales\" ADD COLUMN IF NOT EXISTS \"Fecha\" timestamp without time zone NOT NULL DEFAULT now()",
        "ALTER TABLE \"SnapshotsPatrimoniales\" ADD COLUMN IF NOT EXISTS \"Activos\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"SnapshotsPatrimoniales\" ADD COLUMN IF NOT EXISTS \"Pasivos\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"SnapshotsPatrimoniales\" ADD COLUMN IF NOT EXISTS \"PatrimonioNeto\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"SnapshotsPatrimoniales\" ADD COLUMN IF NOT EXISTS \"Notas\" character varying(300)",

        // ConfigUsuarios
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"UsuarioId\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"DiezmoPct\" double precision NOT NULL DEFAULT 0.10",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"MetaNecesidadesPct\" double precision NOT NULL DEFAULT 0.50",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"MetaDeseosPct\" double precision NOT NULL DEFAULT 0.30",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"MetaDeudaPct\" double precision NOT NULL DEFAULT 0",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"MetaAhorroPct\" double precision NOT NULL DEFAULT 0.20",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"FondoEmergenciaMeses\" integer NOT NULL DEFAULT 4",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"MetaAhorroMinimoPct\" double precision NOT NULL DEFAULT 0.20",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"MetaAhorroOptimoPct\" double precision NOT NULL DEFAULT 0.30",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"CategoriaDiezmoId\" integer",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"SnapshotAutomatico\" boolean NOT NULL DEFAULT true",
        "ALTER TABLE \"ConfigUsuarios\" ADD COLUMN IF NOT EXISTS \"SnapshotDia\" integer NOT NULL DEFAULT 1",

        // AspNetUsers (Usuario agrega estos 2 campos sobre IdentityUser)
        "ALTER TABLE \"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"Nombre\" text NOT NULL DEFAULT ''",
        "ALTER TABLE \"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"FechaRegistro\" timestamp without time zone NOT NULL DEFAULT now()",
    };

    foreach (var sql in alteraciones)
    {
        try
        {
            await bd.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EnsureColumnasPostgres] No se pudo ejecutar '{sql}': {ex.Message}");
        }
    }
}

// Migración de una sola vez: Ingreso (tabla separada, ya no se usa) → Movimiento
// con categoría de pilar Ingreso. Idempotente — una vez migradas, la tabla Ingresos
// queda vacía y esto no vuelve a hacer nada en arranques posteriores. El saldo de
// Cuenta no se toca aquí: ya quedó reflejado cuando el Ingreso original se creó.
static async Task MigrarIngresosAMovimientosAsync(BaseDatosContexto bd)
{
    var ingresosPendientes = await bd.Ingresos.ToListAsync();
    if (ingresosPendientes.Count == 0) return;

    var categoriasIngreso = await bd.Categorias
        .Where(c => c.Tipo == TipoCategoria.Ingreso)
        .ToListAsync();

    foreach (var grupo in ingresosPendientes.GroupBy(i => i.UsuarioId))
    {
        var categoriasUsuario = categoriasIngreso.Where(c => c.UsuarioId == grupo.Key).ToList();
        var categoriaOtros = categoriasUsuario.FirstOrDefault(c => c.Nombre == "Otros")
            ?? categoriasUsuario.FirstOrDefault();
        if (categoriaOtros == null) continue; // usuario sin categoría de pilar Ingreso — no debería pasar

        foreach (var ingreso in grupo)
        {
            var categoria = categoriasUsuario.FirstOrDefault(c =>
                string.Equals(c.Nombre, ingreso.Fuente, StringComparison.OrdinalIgnoreCase)) ?? categoriaOtros;

            bd.Movimientos.Add(new Movimiento
            {
                UsuarioId = ingreso.UsuarioId,
                Fecha = ingreso.Fecha,
                Concepto = ingreso.Concepto,
                CategoriaId = categoria.Id,
                CuentaId = ingreso.CuentaId,
                Monto = ingreso.Monto,
                Notas = ingreso.Notas,
            });
        }
    }

    bd.Ingresos.RemoveRange(ingresosPendientes);
    await bd.SaveChangesAsync();
}
