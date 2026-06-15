using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Modelos;

public class PagoDeuda
{
    public int Id { get; set; }

    public int DeudaId { get; set; }
    public Deuda? Deuda { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;

    public decimal Monto { get; set; }
    public decimal PorcionInteres { get; set; }
    public decimal PorcionCapital { get; set; }

    [MaxLength(300)]
    public string? Notas { get; set; }
}
