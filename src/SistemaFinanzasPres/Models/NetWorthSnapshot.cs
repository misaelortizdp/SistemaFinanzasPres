namespace SistemaFinanzasPres.Models;

public class NetWorthSnapshot
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Assets { get; set; }
    public decimal Liabilities { get; set; }
    public decimal NetWorth { get; set; }
    public string? Notes { get; set; }
}
