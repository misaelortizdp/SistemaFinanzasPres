using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Dtos;

public class MetaAhorroDto
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Prioridad { get; set; } = "MEDIA";

    public decimal Objetivo { get; set; }
    public decimal Acumulado { get; set; }
    public DateTime? FechaLimite { get; set; }
    public decimal AporteMensualPlaneado { get; set; }
    public bool Activa { get; set; } = true;

    [MaxLength(500)]
    public string? Notas { get; set; }
    public int Orden { get; set; }
}

public class AporteMetaDto
{
    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }
}
