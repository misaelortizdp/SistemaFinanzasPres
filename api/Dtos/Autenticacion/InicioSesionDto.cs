using System.ComponentModel.DataAnnotations;

namespace SistemaFinanzasPres.Api.Dtos.Autenticacion;

public class InicioSesionDto
{
    [Required, EmailAddress, MaxLength(120)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Contrasena { get; set; } = string.Empty;
}
