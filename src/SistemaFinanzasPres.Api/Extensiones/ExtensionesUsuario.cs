using System.Security.Claims;

namespace SistemaFinanzasPres.Api.Extensiones;

public static class ExtensionesUsuario
{
    public static string ObtenerId(this ClaimsPrincipal usuario)
        => usuario.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? usuario.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("Token sin identidad de usuario.");
}
