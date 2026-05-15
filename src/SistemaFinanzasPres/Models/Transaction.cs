using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Models;

public class Transaction
{
    public int Id { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(200)]
    public string Concept { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
