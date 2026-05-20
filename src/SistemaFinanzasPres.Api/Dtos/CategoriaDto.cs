using System.ComponentModel.DataAnnotations;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Dtos;

public class CategoriaDto
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; } = TipoCategoria.Necesidad;

    [MaxLength(20)]
    public string? Color { get; set; }

    [MaxLength(20)]
    public string? Icono { get; set; }

    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
}
