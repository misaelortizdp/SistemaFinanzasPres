using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Api.Datos;
using SistemaFinanzasPres.Api.Dtos;
using SistemaFinanzasPres.Api.Extensiones;
using SistemaFinanzasPres.Api.Modelos;

namespace SistemaFinanzasPres.Api.Controladores;

[ApiController, Authorize, Route("api/panel")]
public class PanelController : ControllerBase
{
    private readonly BaseDatosContexto _bd;
    public PanelController(BaseDatosContexto bd) => _bd = bd;

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenPanelDto>> Resumen()
    {
        var uid = User.ObtenerId();
        var hoy = DateTime.UtcNow;
        var anioActual = hoy.Year;
        var mesActual = hoy.Month;

        // Último mes incluido en tendencia = mes actual; primero = hace 5 meses
        var fechaInicio = new DateTime(hoy.AddMonths(-5).Year, hoy.AddMonths(-5).Month, 1);

        // ── Tendencia: últimos 6 meses ───────────────────────────────
        var gastosMes = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha >= fechaInicio)
            .GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(m => m.Monto) })
            .ToListAsync();

        var ingresosMes = await _bd.Ingresos
            .Where(i => i.UsuarioId == uid && i.Fecha >= fechaInicio)
            .GroupBy(i => new { i.Fecha.Year, i.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(i => i.Monto) })
            .ToListAsync();

        var tendencia = Enumerable.Range(0, 6)
            .Select(i => hoy.AddMonths(-5 + i))
            .Select(d => new TendenciaMesDto
            {
                Anio = d.Year,
                Mes = d.Month,
                Gastos = gastosMes.FirstOrDefault(g => g.Year == d.Year && g.Month == d.Month)?.Total ?? 0,
                Ingresos = ingresosMes.FirstOrDefault(g => g.Year == d.Year && g.Month == d.Month)?.Total ?? 0,
            }).ToList();

        // ── Gastos por categoría del mes actual ──────────────────────
        var movMes = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha.Year == anioActual && m.Fecha.Month == mesActual)
            .Select(m => new
            {
                m.Monto,
                m.CategoriaId,
                NombreCat = m.Categoria!.Nombre,
                ColorCat = m.Categoria.Color,
                IconoCat = m.Categoria.Icono,
                TipoCat = m.Categoria.Tipo,
            }).ToListAsync();

        var gastosPorCategoria = movMes
            .GroupBy(m => new { m.CategoriaId, m.NombreCat, m.ColorCat, m.IconoCat })
            .Select(g => new GastoCategoriaPanelDto
            {
                Nombre = g.Key.NombreCat,
                Monto = g.Sum(m => m.Monto),
                Color = g.Key.ColorCat,
                Icono = g.Key.IconoCat,
            })
            .OrderByDescending(g => g.Monto)
            .ToList();

        // ── Distribución 50/30/20 ────────────────────────────────────
        var ingresosActual = await _bd.Ingresos
            .Where(i => i.UsuarioId == uid && i.Fecha.Year == anioActual && i.Fecha.Month == mesActual)
            .SumAsync(i => i.Monto);

        var distribucion = new DistribucionDto
        {
            Necesidades = movMes.Where(m => m.TipoCat == TipoCategoria.Necesidad).Sum(m => m.Monto),
            Deseos = movMes.Where(m => m.TipoCat == TipoCategoria.Deseo).Sum(m => m.Monto),
            Ahorro = movMes.Where(m => m.TipoCat == TipoCategoria.Ahorro).Sum(m => m.Monto),
            TotalIngresos = ingresosActual,
        };

        return Ok(new ResumenPanelDto
        {
            Tendencia = tendencia,
            GastosPorCategoria = gastosPorCategoria,
            Distribucion = distribucion,
        });
    }
}
