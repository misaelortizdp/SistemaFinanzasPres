using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Models;

public class DebtPayment
{
    public int Id { get; set; }

    public int DebtId { get; set; }
    public Debt? Debt { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }
    public decimal InterestPortion { get; set; }
    public decimal PrincipalPortion { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }
}
