using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Models;

public class Account
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
