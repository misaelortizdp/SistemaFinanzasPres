using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public record FondoEmergenciaInfo(
    decimal GastosFijos,
    int Meses,
    decimal Meta,
    decimal Acumulado,
    decimal Avance,
    string Status);

public record TrendSummary(string MejorMes, string PeorMes, string PromedioAhorro, string PromedioGasto);

public class KpiService
{
    private readonly AppDbContext _db;
    private readonly BudgetService _budget;

    public KpiService(AppDbContext db, BudgetService budget)
    {
        _db = db;
        _budget = budget;
    }

    public async Task<FondoEmergenciaInfo> GetFondoEmergenciaAsync(int year, int month)
    {
        var snap = await _budget.GetSnapshotAsync(year, month);
        var cfg = snap.Config;
        var necPilar = snap.Pillars.First(p => p.Pillar == Pillar.Necesidad);

        var gastosFijos = necPilar.Budgeted;
        var metaFondo = gastosFijos * cfg.FondoEmergenciaMeses;
        var fondoBalanceRaw = await _db.Transactions.AsNoTracking()
            .Where(t => t.Category != null && t.Category.Name == "Fondo de Emergencia")
            .Select(t => t.Amount)
            .ToListAsync();
        var fondoBalance = fondoBalanceRaw.Sum();
        var avance = metaFondo > 0 ? fondoBalance / metaFondo : 0m;
        string status =
            fondoBalance >= metaFondo ? "✅ Meta cumplida" :
            fondoBalance >= metaFondo * 0.5m ? "🟡 En progreso" :
            "🔴 Por construir";

        return new FondoEmergenciaInfo(gastosFijos, cfg.FondoEmergenciaMeses, metaFondo, fondoBalance, avance, status);
    }

    public async Task<(int Dias, int DiasDelMes)> GetDiasConRegistroAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var dias = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= start && t.Date < end)
            .Select(t => t.Date.Date)
            .Distinct()
            .CountAsync();
        return (dias, DateTime.DaysInMonth(year, month));
    }

    // Resumen de los últimos 6 meses (mejor/peor mes por neto, promedios) — el detalle
    // mes a mes que antes vivía en una pantalla aparte no se trae: el mismo dato ya se ve,
    // mes a mes, en el propio Dashboard al navegar con las flechas de mes.
    public async Task<TrendSummary> GetTrendSummaryAsync()
    {
        var cfg = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync() ?? new AppConfig();
        var diezmoPct = cfg.DiezmoPct;

        var today = DateTime.Today;
        var monthList = Enumerable.Range(0, 6)
            .Select(i => { var d = today.AddMonths(-5 + i); return (d.Year, d.Month); })
            .ToList();

        var rangeStart = new DateTime(monthList[0].Year, monthList[0].Month, 1);
        var rangeEnd = new DateTime(monthList[^1].Year, monthList[^1].Month, 1).AddMonths(1);

        var incomesByMonth = (await _db.Incomes.AsNoTracking()
            .Where(i => i.Date >= rangeStart && i.Date < rangeEnd)
            .Select(i => new { i.Date.Year, i.Date.Month, i.Amount })
            .ToListAsync())
            .GroupBy(i => (i.Year, i.Month))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var ahoroCatIds = await _db.Categories.AsNoTracking()
            .Where(c => c.Pillar == Pillar.Ahorro && c.IsActive)
            .Select(c => c.Id).ToListAsync();

        var primerFrutoCatIds = await _db.Categories.AsNoTracking()
            .Where(c => c.Pillar == Pillar.PrimerFruto)
            .Select(c => c.Id).ToListAsync();

        var allTx = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= rangeStart && t.Date < rangeEnd && !primerFrutoCatIds.Contains(t.CategoryId))
            .Select(t => new { t.Date.Year, t.Date.Month, t.Amount, t.CategoryId })
            .ToListAsync();

        var txByMonth = allTx.GroupBy(t => (t.Year, t.Month)).ToDictionary(g => g.Key, g => g.ToList());

        var raw = monthList.Select(ym =>
        {
            var ingresoTotal = incomesByMonth.GetValueOrDefault(ym);
            var disponible = ingresoTotal - ingresoTotal * diezmoPct;
            var txs = txByMonth.GetValueOrDefault(ym);
            var gastado = txs?.Sum(t => t.Amount) ?? 0m;
            var ahorro = txs?.Where(t => ahoroCatIds.Contains(t.CategoryId)).Sum(t => t.Amount) ?? 0m;
            var neto = disponible - gastado;
            var tasaAhorro = disponible > 0 ? ahorro / disponible : 0m;
            return (ym, disponible, gastado, neto, tasaAhorro);
        }).Where(r => r.disponible > 0).ToList();

        if (raw.Count == 0)
            return new TrendSummary("—", "—", "0%", "$0");

        var mejor = raw.OrderByDescending(r => r.neto).First();
        var peor = raw.OrderBy(r => r.neto).First();

        return new TrendSummary(
            $"{new DateTime(mejor.ym.Year, mejor.ym.Month, 1):MMM yyyy} (neto {mejor.neto:C0})",
            $"{new DateTime(peor.ym.Year, peor.ym.Month, 1):MMM yyyy} (neto {peor.neto:C0})",
            raw.Average(r => r.tasaAhorro).ToString("P1"),
            raw.Average(r => r.gastado).ToString("C0"));
    }
}
