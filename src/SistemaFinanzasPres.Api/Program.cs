// Permite que DateTime con Kind=Unspecified llegue desde JSON sin error en Npgsql.
// Necesario porque System.Text.Json deserializa fechas como Unspecified por defecto.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Modelos;
using SistemaFinanzasPres.Api.Servicios;

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
