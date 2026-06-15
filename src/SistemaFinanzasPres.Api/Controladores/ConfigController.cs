using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/configuracion")]
public class ConfigController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public ConfigController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<ConfigUsuarioDto>> Obtener()
    {
        var uid = User.ObtenerId();
        var c = await _bd.ConfigUsuarios.AsNoTracking().FirstOrDefaultAsync(x => x.UsuarioId == uid);
        if (c == null) return Ok(new ConfigUsuarioDto());
        return Ok(ToDto(c));
    }

    [HttpPut]
    public async Task<ActionResult<ConfigUsuarioDto>> Guardar([FromBody] ConfigUsuarioDto dto)
    {
        var uid = User.ObtenerId();
        var c = await _bd.ConfigUsuarios.FirstOrDefaultAsync(x => x.UsuarioId == uid);
        if (c == null)
        {
            c = new ConfigUsuario { UsuarioId = uid };
            _bd.ConfigUsuarios.Add(c);
        }
        c.DiezmoPct = dto.DiezmoPct;
        c.MetaNecesidadesPct = dto.MetaNecesidadesPct;
        c.MetaDeseosPct = dto.MetaDeseosPct;
        c.MetaAhorroPct = dto.MetaAhorroPct;
        c.FondoEmergenciaMeses = dto.FondoEmergenciaMeses;
        c.MetaAhorroMinimoPct = dto.MetaAhorroMinimoPct;
        c.MetaAhorroOptimoPct = dto.MetaAhorroOptimoPct;
        c.CategoriaDiezmoId = dto.CategoriaDiezmoId;
        c.SnapshotAutomatico = dto.SnapshotAutomatico;
        c.SnapshotDia = Math.Clamp(dto.SnapshotDia, 1, 28);
        await _bd.SaveChangesAsync();
        return Ok(ToDto(c));
    }

    private static ConfigUsuarioDto ToDto(ConfigUsuario c) => new()
    {
        DiezmoPct = c.DiezmoPct,
        MetaNecesidadesPct = c.MetaNecesidadesPct,
        MetaDeseosPct = c.MetaDeseosPct,
        MetaAhorroPct = c.MetaAhorroPct,
        FondoEmergenciaMeses = c.FondoEmergenciaMeses,
        MetaAhorroMinimoPct = c.MetaAhorroMinimoPct,
        MetaAhorroOptimoPct = c.MetaAhorroOptimoPct,
        CategoriaDiezmoId = c.CategoriaDiezmoId,
        SnapshotAutomatico = c.SnapshotAutomatico,
        SnapshotDia = c.SnapshotDia,
    };
}
