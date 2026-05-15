namespace SistemaFinanzasPres.Models;

public class BudgetItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public decimal Amount { get; set; }
}
