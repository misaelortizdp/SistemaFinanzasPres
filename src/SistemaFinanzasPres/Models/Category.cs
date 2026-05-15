using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Models;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public Pillar Pillar { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
