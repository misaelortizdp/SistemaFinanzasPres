using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Modelos;

public class LineaPresupuesto
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public int Anio { get; set; }
    public int Mes { get; set; }

    public decimal Monto { get; set; }
}
