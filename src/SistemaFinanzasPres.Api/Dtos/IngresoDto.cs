using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Dtos;

public class IngresoDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }

    [MaxLength(200)]
    public string Concepto { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    [MaxLength(80)]
    public string Fuente { get; set; } = "Salario";

    public int? CuentaId { get; set; }
    public string? NombreCuenta { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }

    public bool EsRecurrente { get; set; }
}
