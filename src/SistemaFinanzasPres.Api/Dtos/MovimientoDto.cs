using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Dtos;

public class MovimientoDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }

    [MaxLength(200)]
    public string Concepto { get; set; } = string.Empty;

    public int CategoriaId { get; set; }
    public string? NombreCategoria { get; set; }

    public int? CuentaId { get; set; }
    public string? NombreCuenta { get; set; }

    public decimal Monto { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }
}
