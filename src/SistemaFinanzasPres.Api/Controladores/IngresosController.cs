using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/ingresos")]
public class IngresosController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public IngresosController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IngresoDto>>> Listar([FromQuery] int? anio, [FromQuery] int? mes)
    {
        var uid = User.ObtenerId();
        var q = _bd.Ingresos.AsNoTracking().Where(i => i.UsuarioId == uid);
        if (anio.HasValue) q = q.Where(i => i.Fecha.Year == anio.Value);
        if (mes.HasValue) q = q.Where(i => i.Fecha.Month == mes.Value);

        var datos = await q.OrderByDescending(i => i.Fecha).ThenByDescending(i => i.Id)
            .Select(i => new IngresoDto
            {
                Id = i.Id, Fecha = i.Fecha, Concepto = i.Concepto, Monto = i.Monto,
                Fuente = i.Fuente,
                CuentaId = i.CuentaId, NombreCuenta = i.Cuenta != null ? i.Cuenta.Nombre : null,
                Notas = i.Notas, EsRecurrente = i.EsRecurrente,
            }).ToListAsync();
        return Ok(datos);
    }

    [HttpPost]
    public async Task<ActionResult<IngresoDto>> Crear([FromBody] IngresoDto dto)
    {
        var uid = User.ObtenerId();
        var i = new Ingreso
        {
            UsuarioId = uid,
            Fecha = dto.Fecha == default ? DateTime.UtcNow.Date : dto.Fecha,
            Concepto = dto.Concepto.Trim(),
            Monto = dto.Monto,
            Fuente = string.IsNullOrWhiteSpace(dto.Fuente) ? "Salario" : dto.Fuente.Trim(),
            CuentaId = dto.CuentaId,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            EsRecurrente = dto.EsRecurrente,
        };
        _bd.Ingresos.Add(i);

        if (dto.CuentaId.HasValue)
        {
            var c = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == dto.CuentaId && x.UsuarioId == uid);
            if (c != null) c.Saldo += dto.Monto;
        }
        await _bd.SaveChangesAsync();
        dto.Id = i.Id;
        return Ok(dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] IngresoDto dto)
    {
        var uid = User.ObtenerId();
        var i = await _bd.Ingresos.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (i == null) return NotFound();

        if (i.CuentaId.HasValue)
        {
            var ant = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == i.CuentaId && x.UsuarioId == uid);
            if (ant != null) ant.Saldo -= i.Monto;
        }

        i.Fecha = dto.Fecha;
        i.Concepto = dto.Concepto.Trim();
        i.Monto = dto.Monto;
        i.Fuente = dto.Fuente;
        i.CuentaId = dto.CuentaId;
        i.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();
        i.EsRecurrente = dto.EsRecurrente;

        if (dto.CuentaId.HasValue)
        {
            var nue = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == dto.CuentaId && x.UsuarioId == uid);
            if (nue != null) nue.Saldo += dto.Monto;
        }
        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var uid = User.ObtenerId();
        var i = await _bd.Ingresos.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (i == null) return NotFound();

        if (i.CuentaId.HasValue)
        {
            var c = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == i.CuentaId && x.UsuarioId == uid);
            if (c != null) c.Saldo -= i.Monto;
        }

        _bd.Ingresos.Remove(i);
        await _bd.SaveChangesAsync();
        return NoContent();
    }
}
