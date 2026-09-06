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
        var hace5 = hoy.AddMonths(-5);
        var fechaInicio = new DateTime(hace5.Year, hace5.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var gastosMes = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha >= fechaInicio && m.Categoria!.Tipo != TipoCategoria.Ingreso)
            .GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(m => m.Monto) })
            .ToListAsync();

        var ingresosMes = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha >= fechaInicio && m.Categoria!.Tipo == TipoCategoria.Ingreso)
            .GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(m => m.Monto) })
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
            .Where(m => m.TipoCat != TipoCategoria.Ingreso)
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

        var ingresosActual = movMes.Where(m => m.TipoCat == TipoCategoria.Ingreso).Sum(m => m.Monto);

        var config = await _bd.ConfigUsuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UsuarioId == uid);

        var diezmoPct = config?.DiezmoPct ?? 0m;
        var diezmoMonto = Math.Round(ingresosActual * diezmoPct, 2);
        var ingresoDisponible = ingresosActual - diezmoMonto;

        var necesidades = movMes.Where(m => m.TipoCat == TipoCategoria.Necesidad).Sum(m => m.Monto);
        var deseos = movMes.Where(m => m.TipoCat == TipoCategoria.Deseo).Sum(m => m.Monto);
        var deuda = movMes.Where(m => m.TipoCat == TipoCategoria.Deuda).Sum(m => m.Monto);
        var ahorro = movMes.Where(m => m.TipoCat == TipoCategoria.Ahorro).Sum(m => m.Monto);

        var distribucion = new DistribucionDto
        {
            Necesidades = necesidades,
            Deseos = deseos,
            Deuda = deuda,
            Ahorro = ahorro,
            TotalIngresos = ingresosActual,
            IngresoDisponible = ingresoDisponible,
            DiezmoMonto = diezmoMonto,
        };

        // Proyección de gasto al fin de mes
        var diasTotales = DateTime.DaysInMonth(anioActual, mesActual);
        var diasTranscurridos = Math.Max(1, hoy.Day);
        var gastoActual = movMes.Where(m => m.TipoCat != TipoCategoria.Ingreso).Sum(m => m.Monto);
        var tasaQuema = diasTranscurridos > 0 ? gastoActual / diasTranscurridos : 0;
        var gastoProyectado = Math.Round(tasaQuema * diasTotales, 2);
        var presupuestoDiario = diasTotales > 0
            ? Math.Round(ingresoDisponible / diasTotales, 2)
            : 0;

        var proyeccion = new ProyeccionDto
        {
            DiasTranscurridos = diasTranscurridos,
            DiasTotales = diasTotales,
            GastoActual = gastoActual,
            GastoProyectado = gastoProyectado,
            TasaQuemaDiaria = Math.Round(tasaQuema, 2),
            PresupuestoDiarioPermitido = presupuestoDiario,
            IngresoDisponible = ingresoDisponible,
        };

        return Ok(new ResumenPanelDto
        {
            Tendencia = tendencia,
            GastosPorCategoria = gastosPorCategoria,
            Distribucion = distribucion,
            Proyeccion = proyeccion,
        });
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<KpisPanelDto>> Kpis()
    {
        var uid = User.ObtenerId();
        var hoy = DateTime.UtcNow;
        var anioActual = hoy.Year;
        var mesActual = hoy.Month;

        var config = await _bd.ConfigUsuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UsuarioId == uid);

        var diezmoPct = config?.DiezmoPct ?? 0m;
        var necPct = config?.MetaNecesidadesPct ?? 0.50m;
        var desPct = config?.MetaDeseosPct ?? 0.30m;
        var deudaPct = config?.MetaDeudaPct ?? 0m;
        var ahorPct = config?.MetaAhorroPct ?? 0.20m;
        var fondoMeses = config?.FondoEmergenciaMeses ?? 4;

        var movMes = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha.Year == anioActual && m.Fecha.Month == mesActual)
            .Select(m => new { m.Monto, TipoCat = m.Categoria!.Tipo })
            .ToListAsync();

        var totalIngresos = movMes.Where(m => m.TipoCat == TipoCategoria.Ingreso).Sum(m => m.Monto);
        var diezmoMonto = Math.Round(totalIngresos * diezmoPct, 2);
        var ingresoDisponible = totalIngresos - diezmoMonto;

        var totalGastos = movMes.Where(m => m.TipoCat != TipoCategoria.Ingreso).Sum(m => m.Monto);
        var necesidades = movMes.Where(m => m.TipoCat == TipoCategoria.Necesidad).Sum(m => m.Monto);
        var deseos = movMes.Where(m => m.TipoCat == TipoCategoria.Deseo).Sum(m => m.Monto);
        var deudaPagada = movMes.Where(m => m.TipoCat == TipoCategoria.Deuda).Sum(m => m.Monto);
        var ahorroReal = movMes.Where(m => m.TipoCat == TipoCategoria.Ahorro).Sum(m => m.Monto);
        var ahorroMensual = ingresoDisponible - totalGastos;
        var tasaAhorro = ingresoDisponible > 0 ? Math.Round((ahorroMensual / ingresoDisponible) * 100, 1) : 0;

        // Porcentajes reales sobre ingreso disponible
        var pctNec = ingresoDisponible > 0 ? Math.Round((necesidades / ingresoDisponible) * 100, 1) : 0;
        var pctDes = ingresoDisponible > 0 ? Math.Round((deseos / ingresoDisponible) * 100, 1) : 0;
        var pctDeu = ingresoDisponible > 0 ? Math.Round((deudaPagada / ingresoDisponible) * 100, 1) : 0;
        var pctAhor = ingresoDisponible > 0 ? Math.Round((ahorroReal / ingresoDisponible) * 100, 1) : 0;

        // Fondo de emergencia: gastos de necesidades promedio 3 meses × meses meta
        var hace3 = hoy.AddMonths(-3);
        var fechaInicio3 = new DateTime(hace3.Year, hace3.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var necesidades3m = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha >= fechaInicio3
                     && (m.Categoria!.Tipo == TipoCategoria.Necesidad))
            .SumAsync(m => m.Monto);
        var promedioNecMes = necesidades3m / 3;
        var fondoMeta = Math.Round(promedioNecMes * fondoMeses, 2);

        // Cuenta de ahorro como fondo de emergencia (cuentas tipo ahorro / efectivo)
        var fondoActual = await _bd.Cuentas
            .Where(c => c.UsuarioId == uid && c.Activa)
            .SumAsync(c => c.Saldo);

        // Deudas
        var deudas = await _bd.Deudas
            .Where(d => d.UsuarioId == uid && d.Activa)
            .ToListAsync();

        var diasTotales = DateTime.DaysInMonth(anioActual, mesActual);
        var diasTranscurridos = Math.Max(1, hoy.Day);
        var sugerencias = GenerarSugerencias(
            ingresoDisponible, totalGastos,
            necesidades, deseos, deudaPagada, ahorroReal,
            necPct, desPct, deudaPct, ahorPct,
            diasTranscurridos, diasTotales);

        return Ok(new KpisPanelDto
        {
            TasaAhorroPct = tasaAhorro,
            AhorroMensual = ahorroMensual,
            TotalIngresos = totalIngresos,
            TotalGastos = totalGastos,
            IngresoDisponible = ingresoDisponible,
            DiezmoMonto = diezmoMonto,
            PctNecesidades = pctNec,
            PctDeseos = pctDes,
            PctDeuda = pctDeu,
            PctAhorro = pctAhor,
            FondoEmergenciaActual = fondoActual,
            FondoEmergenciaMeta = fondoMeta,
            FondoEmergenciaMeses = fondoMeses,
            TotalDeudas = deudas.Sum(d => d.SaldoActual),
            DeudasActivas = deudas.Count,
            Sugerencias = sugerencias,
        });
    }

    // Motor de sugerencias accionables: 5 reglas fijas, cada una (snapshot) -> mensaje | nada.
    // Los porcentajes de meta (necPct/desPct/deudaPct/ahorroPct) llegan como fracción (0-1).
    private static List<string> GenerarSugerencias(
        decimal ingresoDisponible, decimal totalGastos,
        decimal necesidades, decimal deseos, decimal deudaPagada, decimal ahorroReal,
        decimal necPct, decimal desPct, decimal deudaPct, decimal ahorroPct,
        int diasTranscurridos, int diasTotales)
    {
        var sugerencias = new List<string>();
        if (ingresoDisponible <= 0) return sugerencias;

        var balance = ingresoDisponible - totalGastos;
        var yaEnRojo = balance < 0;
        if (yaEnRojo)
            sugerencias.Add($"Vas {Math.Abs(balance):C0} en números rojos este mes.");

        void EvaluarSobreMeta(string nombre, decimal monto, decimal metaPct)
        {
            var metaMonto = ingresoDisponible * metaPct;
            if (metaMonto > 0 && monto > metaMonto)
                sugerencias.Add($"Vas {monto - metaMonto:C0} sobre tu meta en {nombre}.");
        }
        EvaluarSobreMeta("Necesidades", necesidades, necPct);
        EvaluarSobreMeta("Deseos", deseos, desPct);
        if (deudaPct > 0) EvaluarSobreMeta("Deuda", deudaPagada, deudaPct);

        var metaAhorroMonto = ingresoDisponible * ahorroPct;
        if (metaAhorroMonto > 0 && ahorroReal < metaAhorroMonto)
            sugerencias.Add($"Te faltan {metaAhorroMonto - ahorroReal:C0} este mes para tu meta de ahorro.");

        // Solo se avisa la proyección cuando el balance de hoy todavía es sano — si ya está
        // en rojo (regla anterior), decir además que "va a terminar peor" no aporta nada nuevo.
        if (!yaEnRojo && diasTranscurridos > 0 && diasTotales > 0)
        {
            var gastoProyectado = totalGastos / diasTranscurridos * diasTotales;
            if (gastoProyectado > ingresoDisponible)
                sugerencias.Add($"Al ritmo actual, vas a cerrar el mes con déficit de {gastoProyectado - ingresoDisponible:C0}.");
        }

        // Gastos hormiga: el pilar Deseos se está ejecutando más rápido de lo que avanza
        // el mes (en vez de un umbral fijo en pesos, que no generaliza entre usuarios).
        if (diasTranscurridos >= 5 && diasTotales > 0)
        {
            var metaDeseosMonto = ingresoDisponible * desPct;
            if (metaDeseosMonto > 0)
            {
                var pctDeseosEjecutado = deseos / metaDeseosMonto;
                var pctMesTranscurrido = (decimal)diasTranscurridos / diasTotales;
                if (pctDeseosEjecutado > pctMesTranscurrido + 0.15m)
                    sugerencias.Add($"Ya usaste el {pctDeseosEjecutado:P0} de tu presupuesto de Deseos y solo va el {pctMesTranscurrido:P0} del mes — cuidado con los gastos hormiga.");
            }
        }

        return sugerencias;
    }

    [HttpGet("tendencias")]
    public async Task<ActionResult<TendenciaDetalleDto>> Tendencias()
    {
        var uid = User.ObtenerId();
        var hoy = DateTime.UtcNow;
        var hace5 = hoy.AddMonths(-5);
        var fechaInicio = new DateTime(hace5.Year, hace5.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var config = await _bd.ConfigUsuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UsuarioId == uid);
        var diezmoPct = config?.DiezmoPct ?? 0m;

        var gastosMes = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha >= fechaInicio && m.Categoria!.Tipo != TipoCategoria.Ingreso)
            .GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(m => m.Monto) })
            .ToListAsync();

        var ingresosMes = await _bd.Movimientos
            .Where(m => m.UsuarioId == uid && m.Fecha >= fechaInicio && m.Categoria!.Tipo == TipoCategoria.Ingreso)
            .GroupBy(m => new { m.Fecha.Year, m.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(m => m.Monto) })
            .ToListAsync();

        var meses = Enumerable.Range(0, 6)
            .Select(i => hoy.AddMonths(-5 + i))
            .Select((d, idx) =>
            {
                var ing = ingresosMes.FirstOrDefault(g => g.Year == d.Year && g.Month == d.Month)?.Total ?? 0;
                var gas = gastosMes.FirstOrDefault(g => g.Year == d.Year && g.Month == d.Month)?.Total ?? 0;
                var diezmo = Math.Round(ing * diezmoPct, 2);
                var disponible = ing - diezmo;
                var ahorro = disponible - gas;
                var tasa = disponible > 0 ? Math.Round((ahorro / disponible) * 100, 1) : 0;
                return new { d.Year, d.Month, Ingresos = ing, Gastos = gas, Disponible = disponible, Ahorro = ahorro, Tasa = tasa, Idx = idx };
            })
            .ToList();

        var mesesDto = meses.Select((m, i) =>
        {
            var prev = i > 0 ? meses[i - 1] : null;
            string flecha = "—";
            if (prev != null)
            {
                if (m.Gastos < prev.Gastos) flecha = "↓";
                else if (m.Gastos > prev.Gastos) flecha = "↑";
            }
            return new TendenciaMesDetalleDto
            {
                Anio = m.Year, Mes = m.Month,
                Ingresos = m.Ingresos, Gastos = m.Gastos,
                IngresoDisponible = m.Disponible, Ahorro = m.Ahorro,
                TasaAhorroPct = m.Tasa, Flecha = flecha,
            };
        }).ToList();

        var conDatos = meses.Where(m => m.Ingresos > 0 || m.Gastos > 0).ToList();
        TendenciaMesDetalleDto? mejorDto = null;
        TendenciaMesDetalleDto? peorDto = null;

        if (conDatos.Any())
        {
            var mejor = conDatos.OrderByDescending(m => m.Tasa).First();
            var peor = conDatos.OrderBy(m => m.Tasa).First();
            mejorDto = mesesDto.First(m => m.Anio == mejor.Year && m.Mes == mejor.Month);
            peorDto = mesesDto.First(m => m.Anio == peor.Year && m.Mes == peor.Month);
        }

        var promedioTasa = conDatos.Any() ? Math.Round(conDatos.Average(m => (double)m.Tasa), 1) : 0;
        var promedioGastos = conDatos.Any() ? Math.Round(conDatos.Average(m => (double)m.Gastos), 2) : 0;

        return Ok(new TendenciaDetalleDto
        {
            Meses = mesesDto,
            MejorMes = mejorDto != null ? $"{mejorDto.Anio}-{mejorDto.Mes:D2}" : null,
            PeorMes = peorDto != null ? $"{peorDto.Anio}-{peorDto.Mes:D2}" : null,
            PromedioTasaAhorro = (decimal)promedioTasa,
            PromedioGastos = (decimal)promedioGastos,
        });
    }
}
