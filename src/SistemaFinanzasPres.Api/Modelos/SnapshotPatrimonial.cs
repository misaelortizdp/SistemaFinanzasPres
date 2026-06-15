using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Modelos;

public class SnapshotPatrimonial
{
    public int Id { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public Usuario? Usuario { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow.Date;

    public decimal Activos { get; set; }
    public decimal Pasivos { get; set; }
    public decimal PatrimonioNeto { get; set; }

    [MaxLength(300)]
    public string? Notas { get; set; }
}
