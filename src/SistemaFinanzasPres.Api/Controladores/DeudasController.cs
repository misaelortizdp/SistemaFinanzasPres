using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/deudas")]
public class DeudasController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public DeudasController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeudaDto>>> Listar()
    {
        var uid = User.ObtenerId();
        var datos = await _bd.Deudas.AsNoTracking()
            .Where(d => d.UsuarioId == uid)
            .OrderByDescending(d => d.Activa).ThenByDescending(d => d.SaldoActual)
            .Select(d => new DeudaDto
            {
                Id = d.Id, Nombre = d.Nombre,
                MontoOriginal = d.MontoOriginal, SaldoActual = d.SaldoActual,
                TasaInteres = d.TasaInteres, PagoMinimo = d.PagoMinimo,
                DiaPago = d.DiaPago, Activa = d.Activa, Notas = d.Notas,
            }).ToListAsync();
        return Ok(datos);
    }

    [HttpPost]
    public async Task<ActionResult<DeudaDto>> Crear([FromBody] DeudaDto dto)
    {
        var uid = User.ObtenerId();
        var d = new Deuda
        {
            UsuarioId = uid,
            Nombre = dto.Nombre.Trim(),
            MontoOriginal = dto.MontoOriginal,
            SaldoActual = dto.SaldoActual == 0 ? dto.MontoOriginal : dto.SaldoActual,
            TasaInteres = dto.TasaInteres,
            PagoMinimo = dto.PagoMinimo,
            DiaPago = dto.DiaPago,
            Activa = dto.Activa,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
        };
        _bd.Deudas.Add(d);
        await _bd.SaveChangesAsync();
        dto.Id = d.Id;
        return Ok(dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] DeudaDto dto)
    {
        var uid = User.ObtenerId();
        var d = await _bd.Deudas.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (d == null) return NotFound();
        d.Nombre = dto.Nombre.Trim();
        d.MontoOriginal = dto.MontoOriginal;
        d.SaldoActual = dto.SaldoActual;
        d.TasaInteres = dto.TasaInteres;
        d.PagoMinimo = dto.PagoMinimo;
        d.DiaPago = dto.DiaPago;
        d.Activa = dto.Activa;
        d.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();
        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var uid = User.ObtenerId();
        var d = await _bd.Deudas.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (d == null) return NotFound();
        _bd.Deudas.Remove(d);
        await _bd.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{deudaId:int}/pagos")]
    public async Task<ActionResult<IEnumerable<PagoDeudaDto>>> ListarPagos(int deudaId)
    {
        var uid = User.ObtenerId();
        var existe = await _bd.Deudas.AnyAsync(d => d.Id == deudaId && d.UsuarioId == uid);
        if (!existe) return NotFound();

        var datos = await _bd.PagosDeuda.AsNoTracking()
            .Where(p => p.DeudaId == deudaId)
            .OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id)
            .Select(p => new PagoDeudaDto
            {
                Id = p.Id, DeudaId = p.DeudaId, Fecha = p.Fecha,
                Monto = p.Monto, PorcionInteres = p.PorcionInteres,
                PorcionCapital = p.PorcionCapital, Notas = p.Notas,
            }).ToListAsync();
        return Ok(datos);
    }

    [HttpPost("{deudaId:int}/pagos")]
    public async Task<ActionResult<PagoDeudaDto>> RegistrarPago(int deudaId, [FromBody] PagoDeudaDto dto)
    {
        var uid = User.ObtenerId();
        var d = await _bd.Deudas.FirstOrDefaultAsync(x => x.Id == deudaId && x.UsuarioId == uid);
        if (d == null) return NotFound();

        var p = new PagoDeuda
        {
            DeudaId = deudaId,
            Fecha = dto.Fecha == default ? DateTime.UtcNow.Date : dto.Fecha,
            Monto = dto.Monto,
            PorcionInteres = dto.PorcionInteres,
            PorcionCapital = dto.PorcionCapital == 0 ? Math.Max(0, dto.Monto - dto.PorcionInteres) : dto.PorcionCapital,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
        };
        _bd.PagosDeuda.Add(p);

        d.SaldoActual = Math.Max(0, d.SaldoActual - p.PorcionCapital);
        if (d.SaldoActual == 0) d.Activa = false;

        await _bd.SaveChangesAsync();
        dto.Id = p.Id;
        dto.DeudaId = deudaId;
        return Ok(dto);
    }

    [HttpDelete("{deudaId:int}/pagos/{pagoId:int}")]
    public async Task<IActionResult> EliminarPago(int deudaId, int pagoId)
    {
        var uid = User.ObtenerId();
        var d = await _bd.Deudas.FirstOrDefaultAsync(x => x.Id == deudaId && x.UsuarioId == uid);
        if (d == null) return NotFound();
        var p = await _bd.PagosDeuda.FirstOrDefaultAsync(x => x.Id == pagoId && x.DeudaId == deudaId);
        if (p == null) return NotFound();

        d.SaldoActual += p.PorcionCapital;
        if (d.SaldoActual > 0) d.Activa = true;

        _bd.PagosDeuda.Remove(p);
        await _bd.SaveChangesAsync();
        return NoContent();
    }
}
