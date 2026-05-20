using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Servicios;

public interface IServicioJwt
{
    (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario);
}
