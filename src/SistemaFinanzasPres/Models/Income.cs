using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Models;

public class Income
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(200)]
    public string Concept { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [MaxLength(80)]
    public string Source { get; set; } = "Salario";

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsRecurring { get; set; }
}
