using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/cuentas")]
public class CuentasController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public CuentasController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CuentaDto>>> Listar()
    {
        var uid = User.ObtenerId();
        var datos = await _bd.Cuentas.AsNoTracking()
            .Where(c => c.UsuarioId == uid)
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .Select(c => new CuentaDto { Id = c.Id, Nombre = c.Nombre, Saldo = c.Saldo, Orden = c.Orden, Activa = c.Activa })
            .ToListAsync();
        return Ok(datos);
    }

    [HttpPost]
    public async Task<ActionResult<CuentaDto>> Crear([FromBody] CuentaDto dto)
    {
        var uid = User.ObtenerId();
        var c = new Cuenta { UsuarioId = uid, Nombre = dto.Nombre.Trim(), Saldo = dto.Saldo, Orden = dto.Orden, Activa = dto.Activa };
        _bd.Cuentas.Add(c);
        await _bd.SaveChangesAsync();
        dto.Id = c.Id;
        return Ok(dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CuentaDto dto)
    {
        var uid = User.ObtenerId();
        var c = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (c == null) return NotFound();
        c.Nombre = dto.Nombre.Trim();
        c.Saldo = dto.Saldo;
        c.Orden = dto.Orden;
        c.Activa = dto.Activa;
        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var uid = User.ObtenerId();
        var c = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (c == null) return NotFound();
        _bd.Cuentas.Remove(c);
        await _bd.SaveChangesAsync();
        return NoContent();
    }
}
