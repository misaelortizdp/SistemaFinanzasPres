using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Modelos;

public class Ingreso
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;

    [MaxLength(200)]
    public string Concepto { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    [MaxLength(80)]
    public string Fuente { get; set; } = "Salario";

    public int? CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }

    public bool EsRecurrente { get; set; }
}
