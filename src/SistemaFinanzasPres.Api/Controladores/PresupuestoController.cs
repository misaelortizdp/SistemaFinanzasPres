using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/presupuesto")]
public class PresupuestoController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public PresupuestoController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LineaPresupuestoDto>>> Listar([FromQuery] int anio, [FromQuery] int mes)
    {
        var uid = User.ObtenerId();
        var lineas = await _bd.LineasPresupuesto.AsNoTracking()
            .Where(l => l.UsuarioId == uid && l.Anio == anio && l.Mes == mes)
            .Include(l => l.Categoria)
            .ToListAsync();

        var ejecutado = await _bd.Movimientos.AsNoTracking()
            .Where(m => m.UsuarioId == uid && m.Fecha.Year == anio && m.Fecha.Month == mes)
            .GroupBy(m => m.CategoriaId)
            .Select(g => new { CategoriaId = g.Key, Total = g.Sum(m => m.Monto) })
            .ToDictionaryAsync(x => x.CategoriaId, x => x.Total);

        // Deudas activas vinculadas a una categoría: su presupuesto se calcula solo
        // (PagoMinimo + AbonoExtra) — no se guarda a mano en LineasPresupuesto.
        var deudasActivas = await _bd.Deudas.AsNoTracking()
            .Where(d => d.UsuarioId == uid && d.Activa && d.CategoriaId != null)
            .Include(d => d.Categoria)
            .ToListAsync();
        var deudaPorCategoria = deudasActivas.ToDictionary(d => d.CategoriaId!.Value);

        var datos = lineas.Select(l => new LineaPresupuestoDto
        {
            Id = l.Id,
            CategoriaId = l.CategoriaId,
            NombreCategoria = l.Categoria?.Nombre,
            Anio = l.Anio,
            Mes = l.Mes,
            Monto = deudaPorCategoria.TryGetValue(l.CategoriaId, out var deudaLinea)
                ? deudaLinea.PagoMinimo + deudaLinea.AbonoExtra : l.Monto,
            Ejecutado = ejecutado.TryGetValue(l.CategoriaId, out var v) ? v : 0m,
            EsAutomatico = deudaPorCategoria.ContainsKey(l.CategoriaId),
        }).ToList();

        // Deudas cuya categoría todavía no tiene fila de presupuesto este mes
        // (recién creada, o mes sin "copiar mes anterior") — se sintetiza igual.
        var categoriasYaListadas = datos.Select(x => x.CategoriaId).ToHashSet();
        foreach (var d in deudasActivas.Where(d => !categoriasYaListadas.Contains(d.CategoriaId!.Value)))
        {
            datos.Add(new LineaPresupuestoDto
            {
                CategoriaId = d.CategoriaId!.Value,
                NombreCategoria = d.Categoria?.Nombre,
                Anio = anio,
                Mes = mes,
                Monto = d.PagoMinimo + d.AbonoExtra,
                Ejecutado = ejecutado.TryGetValue(d.CategoriaId.Value, out var v2) ? v2 : 0m,
                EsAutomatico = true,
            });
        }

        return Ok(datos.OrderBy(x => x.NombreCategoria).ToList());
    }

    [HttpPut]
    public async Task<ActionResult<LineaPresupuestoDto>> GuardarLinea([FromBody] LineaPresupuestoDto dto)
    {
        var uid = User.ObtenerId();

        var esDeDeuda = await _bd.Deudas.AnyAsync(d => d.UsuarioId == uid && d.Activa && d.CategoriaId == dto.CategoriaId);
        if (esDeDeuda)
            return BadRequest(new { error = "El presupuesto de esta categoría se calcula automáticamente desde la deuda vinculada (pago mínimo + abono extra)." });

        var linea = await _bd.LineasPresupuesto.FirstOrDefaultAsync(
            l => l.UsuarioId == uid && l.Anio == dto.Anio && l.Mes == dto.Mes && l.CategoriaId == dto.CategoriaId);

        if (linea == null)
        {
            linea = new LineaPresupuesto
            {
                UsuarioId = uid,
                CategoriaId = dto.CategoriaId,
                Anio = dto.Anio,
                Mes = dto.Mes,
                Monto = dto.Monto,
            };
            _bd.LineasPresupuesto.Add(linea);
        }
        else
        {
            linea.Monto = dto.Monto;
        }
        await _bd.SaveChangesAsync();
        dto.Id = linea.Id;
        return Ok(dto);
    }

    [HttpPost("copiar-mes-anterior")]
    public async Task<IActionResult> CopiarMesAnterior([FromQuery] int anio, [FromQuery] int mes)
    {
        var uid = User.ObtenerId();
        var ant = new DateTime(anio, mes, 1).AddMonths(-1);

        var anteriores = await _bd.LineasPresupuesto.AsNoTracking()
            .Where(l => l.UsuarioId == uid && l.Anio == ant.Year && l.Mes == ant.Month)
            .ToListAsync();
        if (anteriores.Count == 0) return Ok(new { copiadas = 0 });

        var existentes = await _bd.LineasPresupuesto
            .Where(l => l.UsuarioId == uid && l.Anio == anio && l.Mes == mes)
            .Select(l => l.CategoriaId)
            .ToListAsync();

        int copiadas = 0;
        foreach (var p in anteriores.Where(a => !existentes.Contains(a.CategoriaId)))
        {
            _bd.LineasPresupuesto.Add(new LineaPresupuesto
            {
                UsuarioId = uid, CategoriaId = p.CategoriaId, Anio = anio, Mes = mes, Monto = p.Monto,
            });
            copiadas++;
        }
        await _bd.SaveChangesAsync();
        return Ok(new { copiadas });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var uid = User.ObtenerId();
        var l = await _bd.LineasPresupuesto.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid);
        if (l == null) return NotFound();
        _bd.LineasPresupuesto.Remove(l);
        await _bd.SaveChangesAsync();
        return NoContent();
    }
}
