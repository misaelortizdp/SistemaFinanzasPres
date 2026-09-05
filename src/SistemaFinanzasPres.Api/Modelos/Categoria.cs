using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Modelos;

public enum TipoCategoria
{
    Necesidad = 1,
    Deseo = 2,
    Ahorro = 3,
    Ingreso = 4,
    Deuda = 5,
}

public class Categoria
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; } = TipoCategoria.Necesidad;

    [MaxLength(20)]
    public string? Color { get; set; }

    [MaxLength(20)]
    public string? Icono { get; set; }

    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
}
