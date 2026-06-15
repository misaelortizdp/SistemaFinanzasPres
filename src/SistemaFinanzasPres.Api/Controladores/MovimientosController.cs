using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/movimientos")]
public class MovimientosController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public MovimientosController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovimientoDto>>> Listar([FromQuery] int? anio, [FromQuery] int? mes, [FromQuery] int? categoriaId)
    {
        var uid = User.ObtenerId();
        var q = _bd.Movimientos.AsNoTracking().Where(m => m.UsuarioId == uid);
        if (anio.HasValue) q = q.Where(m => m.Fecha.Year == anio.Value);
        if (mes.HasValue) q = q.Where(m => m.Fecha.Month == mes.Value);
        if (categoriaId.HasValue) q = q.Where(m => m.CategoriaId == categoriaId.Value);

        var datos = await q.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .Select(m => new MovimientoDto
            {
                Id = m.Id, Fecha = m.Fecha, Concepto = m.Concepto,
                CategoriaId = m.CategoriaId, NombreCategoria = m.Categoria!.Nombre,
                CuentaId = m.CuentaId, NombreCuenta = m.Cuenta != null ? m.Cuenta.Nombre : null,
                Monto = m.Monto, Notas = m.Notas,
            }).ToListAsync();
        return Ok(datos);
    }

    [HttpPost]
    public async Task<ActionResult<MovimientoDto>> Crear([FromBody] MovimientoDto dto)
    {
        var uid = User.ObtenerId();
        var m = new Movimiento
        {
            UsuarioId = uid,
            Fecha = dto.Fecha == default ? DateTime.UtcNow.Date : dto.Fecha,
            Concepto = dto.Concepto.Trim(),
            CategoriaId = dto.CategoriaId,
            CuentaId = dto.CuentaId,
            Monto = dto.Monto,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
        };
        _bd.Movimientos.Add(m);

        if (dto.CuentaId.HasValue)
        {
            var c = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == dto.CuentaId && x.UsuarioId == uid);
            if (c != null) c.Saldo -= dto.Monto;
        }
        await _bd.SaveChangesAsync();
        dto.Id = m.Id;
        return Ok(dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] MovimientoDto dto)
    {
        var uid = User.ObtenerId();
        var m = await _bd.Movimientos.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (m == null) return NotFound();

        if (m.CuentaId.HasValue)
        {
            var ant = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == m.CuentaId && x.UsuarioId == uid);
            if (ant != null) ant.Saldo += m.Monto;
        }

        m.Fecha = dto.Fecha;
        m.Concepto = dto.Concepto.Trim();
        m.CategoriaId = dto.CategoriaId;
        m.CuentaId = dto.CuentaId;
        m.Monto = dto.Monto;
        m.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();

        if (dto.CuentaId.HasValue)
        {
            var nue = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == dto.CuentaId && x.UsuarioId == uid);
            if (nue != null) nue.Saldo -= dto.Monto;
        }

        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var uid = User.ObtenerId();
        var m = await _bd.Movimientos.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (m == null) return NotFound();

        if (m.CuentaId.HasValue)
        {
            var c = await _bd.Cuentas.FirstOrDefaultAsync(x => x.Id == m.CuentaId && x.UsuarioId == uid);
            if (c != null) c.Saldo += m.Monto;
        }

        _bd.Movimientos.Remove(m);
        await _bd.SaveChangesAsync();
        return NoContent();
    }
}
