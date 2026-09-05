using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Dtos;

public class DeudaDto
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    public decimal MontoOriginal { get; set; }
    public decimal SaldoActual { get; set; }
    public decimal TasaInteres { get; set; }
    public decimal PagoMinimo { get; set; }
    public decimal AbonoExtra { get; set; }
    public int DiaPago { get; set; } = 1;
    public bool Activa { get; set; } = true;

    [MaxLength(500)]
    public string? Notas { get; set; }

    // Informativo — la categoría se gestiona sola, el cliente no la envía.
    public int? CategoriaId { get; set; }

    // Calculados en Listar() — el cliente no los envía al crear/editar.
    public int? MesesParaLiquidar { get; set; }
    public int? PrioridadAvalancha { get; set; }
}

public class PagoDeudaDto
{
    public int Id { get; set; }
    public int DeudaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public decimal PorcionInteres { get; set; }
    public decimal PorcionCapital { get; set; }

    [MaxLength(300)]
    public string? Notas { get; set; }
}
