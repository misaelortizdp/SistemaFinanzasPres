using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Dtos;

public class CuentaDto
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    public decimal Saldo { get; set; }
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
}
