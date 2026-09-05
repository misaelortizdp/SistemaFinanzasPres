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
        var deudas = await _bd.Deudas.AsNoTracking()
            .Where(d => d.UsuarioId == uid)
            .OrderByDescending(d => d.Activa).ThenByDescending(d => d.SaldoActual)
            .ToListAsync();

        // Prioridad avalancha: 1 = mayor tasa de interés, solo entre las que aún tienen saldo.
        var prioridades = deudas
            .Where(d => d.Activa && d.SaldoActual > 0)
            .OrderByDescending(d => d.TasaInteres)
            .Select((d, i) => (d.Id, Prioridad: i + 1))
            .ToDictionary(x => x.Id, x => x.Prioridad);

        var datos = deudas.Select(d => new DeudaDto
        {
            Id = d.Id, Nombre = d.Nombre,
            MontoOriginal = d.MontoOriginal, SaldoActual = d.SaldoActual,
            TasaInteres = d.TasaInteres, PagoMinimo = d.PagoMinimo, AbonoExtra = d.AbonoExtra,
            DiaPago = d.DiaPago, Activa = d.Activa, Notas = d.Notas,
            CategoriaId = d.CategoriaId,
            MesesParaLiquidar = CalcularMesesParaLiquidar(d),
            PrioridadAvalancha = prioridades.TryGetValue(d.Id, out var p) ? p : null,
        }).ToList();
        return Ok(datos);
    }

    // Fórmula de amortización estándar (la misma que usa el Excel de referencia): con un pago
    // mensual fijo, cuántos meses hacen falta para llevar el saldo a 0. Null cuando el pago no
    // alcanza ni para cubrir el interés del mes — con ese pago la deuda nunca baja.
    private static int? CalcularMesesParaLiquidar(Deuda d)
    {
        if (d.SaldoActual <= 0) return 0;
        var pago = d.PagoMinimo + d.AbonoExtra;
        if (pago <= 0) return null;

        var tasaMensual = d.TasaInteres / 100m / 12m;
        if (tasaMensual == 0) return (int)Math.Ceiling(d.SaldoActual / pago);

        var interesMensual = d.SaldoActual * tasaMensual;
        if (pago <= interesMensual) return null;

        var meses = -Math.Log(1 - (double)(interesMensual / pago)) / Math.Log(1 + (double)tasaMensual);
        return (int)Math.Ceiling(meses);
    }

    [HttpPost]
    public async Task<ActionResult<DeudaDto>> Crear([FromBody] DeudaDto dto)
    {
        var uid = User.ObtenerId();
        var nombre = dto.Nombre.Trim();

        // Cada deuda tiene su propia categoría de presupuesto (pilar Deuda), creada
        // automáticamente — así el usuario no tiene que duplicar el alta a mano.
        var categoria = new Categoria
        {
            UsuarioId = uid,
            Nombre = nombre,
            Tipo = TipoCategoria.Deuda,
            Activa = dto.Activa,
        };

        var d = new Deuda
        {
            UsuarioId = uid,
            Nombre = nombre,
            MontoOriginal = dto.MontoOriginal,
            SaldoActual = dto.SaldoActual == 0 ? dto.MontoOriginal : dto.SaldoActual,
            TasaInteres = dto.TasaInteres,
            PagoMinimo = dto.PagoMinimo,
            AbonoExtra = dto.AbonoExtra,
            DiaPago = dto.DiaPago,
            Activa = dto.Activa,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
            Categoria = categoria,
        };
        _bd.Categorias.Add(categoria);
        _bd.Deudas.Add(d);
        await _bd.SaveChangesAsync();
        dto.Id = d.Id;
        dto.CategoriaId = categoria.Id;
        return Ok(dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] DeudaDto dto)
    {
        var uid = User.ObtenerId();
        var d = await _bd.Deudas.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (d == null) return NotFound();

        var nombreNuevo = dto.Nombre.Trim();
        if (d.CategoriaId != null && d.Nombre != nombreNuevo)
        {
            var cat = await _bd.Categorias.FirstOrDefaultAsync(c => c.Id == d.CategoriaId && c.UsuarioId == uid);
            if (cat != null) cat.Nombre = nombreNuevo;
        }

        d.Nombre = nombreNuevo;
        d.MontoOriginal = dto.MontoOriginal;
        d.SaldoActual = dto.SaldoActual;
        d.TasaInteres = dto.TasaInteres;
        d.PagoMinimo = dto.PagoMinimo;
        d.AbonoExtra = dto.AbonoExtra;
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

        // La categoría se desactiva en vez de borrarse: preserva el histórico de
        // movimientos ya registrados contra ella (igual que "desactivar" en Categorías).
        if (d.CategoriaId != null)
        {
            var cat = await _bd.Categorias.FirstOrDefaultAsync(c => c.Id == d.CategoriaId && c.UsuarioId == uid);
            if (cat != null) cat.Activa = false;
        }

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
