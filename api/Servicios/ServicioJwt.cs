using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Servicios;

public class ServicioJwt : IServicioJwt
{
    private readonly IConfiguration _config;

    public ServicioJwt(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario)
    {
        var llave = _config["Jwt:Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
        var emisor = _config["Jwt:Issuer"];
        var audiencia = _config["Jwt:Audience"];
        var minutos = int.TryParse(_config["Jwt:ExpiresInMinutes"], out var m) ? m : 1440;

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(llave)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new("nombre", usuario.Nombre),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var expira = DateTime.UtcNow.AddMinutes(minutos);

        var token = new JwtSecurityToken(
            issuer: emisor,
            audience: audiencia,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
