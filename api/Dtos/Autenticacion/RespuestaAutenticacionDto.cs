namespace SistemaFinanzasPres.Api.Dtos.Autenticacion;

public class RespuestaAutenticacionDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
