using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Dtos.Autenticacion;

public class RegistroDto
{
    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Contrasena { get; set; } = string.Empty;
}
