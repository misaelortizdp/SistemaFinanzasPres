using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Models;

public class SavingsGoal
{
    public int Id { get; set; }

    public int Step { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Priority { get; set; } = string.Empty;

    public decimal Target { get; set; }
    public decimal Achieved { get; set; }

    public DateTime? Deadline { get; set; }
    public decimal MonthlyPlanned { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int SortOrder { get; set; }
}
