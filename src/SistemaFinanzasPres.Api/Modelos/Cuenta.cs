using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Modelos;

public class Cuenta
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    public decimal Saldo { get; set; }

    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
}
