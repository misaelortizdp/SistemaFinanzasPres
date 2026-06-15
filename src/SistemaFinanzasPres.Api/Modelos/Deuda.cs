using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Modelos;

public class Deuda
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    public decimal MontoOriginal { get; set; }
    public decimal SaldoActual { get; set; }

    public decimal TasaInteres { get; set; }
    public decimal PagoMinimo { get; set; }

    public int DiaPago { get; set; } = 1;

    public bool Activa { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow.Date;

    [MaxLength(500)]
    public string? Notas { get; set; }
}
