using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Datos;

public class BaseDatosContexto : IdentityDbContext<Usuario>
{
    public BaseDatosContexto(DbContextOptions<BaseDatosContexto> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<Ingreso> Ingresos => Set<Ingreso>();
    public DbSet<LineaPresupuesto> LineasPresupuesto => Set<LineaPresupuesto>();
    public DbSet<Deuda> Deudas => Set<Deuda>();
    public DbSet<PagoDeuda> PagosDeuda => Set<PagoDeuda>();
    public DbSet<MetaAhorro> MetasAhorro => Set<MetaAhorro>();
    public DbSet<SnapshotPatrimonial> SnapshotsPatrimoniales => Set<SnapshotPatrimonial>();
    public DbSet<ConfigUsuario> ConfigUsuarios => Set<ConfigUsuario>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        // SQLite no ordena decimal nativamente; convertimos a double para que
        // los ORDER BY funcionen. Postgres soporta decimal pero la conversión
        // también funciona ahí — precisión suficiente para finanzas personales.
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Categoria>(e =>
        {
            e.HasIndex(c => new { c.UsuarioId, c.Nombre });
            e.HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Cuenta>(e =>
        {
            e.HasIndex(c => c.UsuarioId);
            e.HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Movimiento>(e =>
        {
            e.HasIndex(m => new { m.UsuarioId, m.Fecha });
            e.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Categoria).WithMany().HasForeignKey(m => m.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Cuenta).WithMany().HasForeignKey(m => m.CuentaId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Ingreso>(e =>
        {
            e.HasIndex(i => new { i.UsuarioId, i.Fecha });
            e.HasOne(i => i.Usuario).WithMany().HasForeignKey(i => i.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Cuenta).WithMany().HasForeignKey(i => i.CuentaId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<LineaPresupuesto>(e =>
        {
            e.HasIndex(l => new { l.UsuarioId, l.Anio, l.Mes, l.CategoriaId }).IsUnique();
            e.HasOne(l => l.Usuario).WithMany().HasForeignKey(l => l.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.Categoria).WithMany().HasForeignKey(l => l.CategoriaId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Deuda>(e =>
        {
            e.HasIndex(d => d.UsuarioId);
            e.HasOne(d => d.Usuario).WithMany().HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PagoDeuda>(e =>
        {
            e.HasIndex(p => p.DeudaId);
            e.HasOne(p => p.Deuda).WithMany().HasForeignKey(p => p.DeudaId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MetaAhorro>(e =>
        {
            e.HasIndex(m => m.UsuarioId);
            e.HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SnapshotPatrimonial>(e =>
        {
            e.HasIndex(s => new { s.UsuarioId, s.Fecha });
            e.HasOne(s => s.Usuario).WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ConfigUsuario>(e =>
        {
            e.HasIndex(c => c.UsuarioId).IsUnique();
            e.HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
