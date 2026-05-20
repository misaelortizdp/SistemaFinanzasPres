using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Datos;

public class BaseDatosContexto : IdentityDbContext<Usuario>
{
    public BaseDatosContexto(DbContextOptions<BaseDatosContexto> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Categoria>(e =>
        {
            e.HasIndex(c => new { c.UsuarioId, c.Nombre });
            e.HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
