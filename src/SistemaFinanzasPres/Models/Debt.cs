using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Models;

public class Debt
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public decimal OriginalAmount { get; set; }
    public decimal CurrentBalance { get; set; }

    public decimal InterestRate { get; set; }

    public decimal MinPayment { get; set; }

    public int DueDay { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Today;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
