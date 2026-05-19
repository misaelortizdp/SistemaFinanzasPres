using Microsoft.AspNetCore.Identity;

namespace SistemaFinanzasPres.Api.Modelos;

public class Usuario : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
