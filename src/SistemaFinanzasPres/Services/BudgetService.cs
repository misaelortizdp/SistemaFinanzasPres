using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public record CategoryStatus(
    int CategoryId,
    string CategoryName,
    Pillar Pillar,
    decimal Budgeted,
    decimal Spent,
    decimal Available,
    decimal Percent,
    string Status);

public record PillarSummary(
    Pillar Pillar,
    decimal Budgeted,
    decimal Spent,
    decimal Available,
    decimal PercentOfIncomeBase,
    decimal MetaPct,
    decimal MetaAmount,
    string Status);

public record MonthProjection(
    bool IsCurrentMonth,
    int DaysElapsed,
    int DaysRemaining,
    int DaysInMonth,
    decimal BurnRateDaily,
    decimal ProjectedSpend,
    decimal AllowedDaily,
    decimal ProjectedSurplus,
    string Headline);

public record BudgetSnapshot(
    AppConfig Config,
    decimal IngresoTotal,
    decimal IngresoDisponible,
    bool UsedTransactionalIncome,
    decimal TotalBudgeted,
    decimal TotalSpent,
    decimal TotalAvailable,
    decimal PercentExecuted,
    IReadOnlyList<CategoryStatus> Categories,
    IReadOnlyList<PillarSummary> Pillars,
    MonthProjection Projection);

public class BudgetService
{
    private readonly AppDbContext _db;

    public BudgetService(AppDbContext db) => _db = db;

    public async Task<BudgetSnapshot> GetSnapshotAsync(int year, int month, CancellationToken ct = default)
    {
        var config = await _db.AppConfigs.AsNoTracking().FirstAsync(ct);

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        var budgets = await _db.Budgets.AsNoTracking()
            .Where(b => b.Year == year && b.Month == month)
            .ToListAsync(ct);

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var txInMonth = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= start && t.Date < end)
            .Select(t => new { t.CategoryId, t.Amount })
            .ToListAsync(ct);

        var incomesInMonth = await _db.Incomes.AsNoTracking()
            .Where(i => i.Date >= start && i.Date < end)
            .Select(i => i.Amount)
            .ToListAsync(ct);

        var budgetByCat = budgets.ToDictionary(b => b.CategoryId, b => b.Amount);
        var spentByCat = txInMonth
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var rows = categories.Select(c =>
        {
            var b = budgetByCat.GetValueOrDefault(c.Id);
            var s = spentByCat.GetValueOrDefault(c.Id);
            var pct = b > 0 ? s / b : 0m;
            string status =
                s == 0 ? "⬜ Sin gastos" :
                pct > 1m ? "🔴 Excedido" :
                pct > 0.85m ? "🟡 Alerta" :
                "🟢 OK";
            return new CategoryStatus(c.Id, c.Name, c.Pillar, b, s, b - s, pct, status);
        }).ToList();

        var diezmoBudget = rows.Where(r => r.Pillar == Pillar.PrimerFruto).Sum(r => r.Budgeted);

        var transactionalIncome = incomesInMonth.Sum();
        var usedTransactional = transactionalIncome > 0;
        var ingresoTotal = usedTransactional ? transactionalIncome : config.IngresoTotal;
        var ingresoDisponible = ingresoTotal - diezmoBudget;

        PillarSummary BuildPillar(Pillar p, decimal metaPct)
        {
            var items = rows.Where(r => r.Pillar == p).ToList();
            var b = items.Sum(x => x.Budgeted);
            var s = items.Sum(x => x.Spent);
            var pctBase = ingresoDisponible > 0 ? s / ingresoDisponible : 0m;
            var meta = ingresoDisponible * metaPct;
            string status = p == Pillar.PrimerFruto
                ? "🙏 Pre-comprometido"
                : (pctBase <= metaPct ? "✅ Dentro meta" : "⚠️ Sobre meta");
            return new PillarSummary(p, b, s, b - s, pctBase, metaPct, meta, status);
        }

        var pillars = new List<PillarSummary>
        {
            BuildPillar(Pillar.PrimerFruto, 0m),
            BuildPillar(Pillar.Necesidad,   config.MetaNecesidadesPct),
            BuildPillar(Pillar.Deseo,       config.MetaDeseosPct),
            BuildPillar(Pillar.Ahorro,      config.MetaAhorroPct),
        };

        var totalB = rows.Sum(r => r.Budgeted);
        var totalS = rows.Sum(r => r.Spent);
        var pctExec = totalB > 0 ? totalS / totalB : 0m;

        var projection = BuildProjection(year, month, totalS, ingresoTotal);

        return new BudgetSnapshot(
            config, ingresoTotal, ingresoDisponible, usedTransactional,
            totalB, totalS, totalB - totalS, pctExec,
            rows, pillars, projection);
    }

    private static MonthProjection BuildProjection(int year, int month, decimal totalSpent, decimal ingresoBase)
    {
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddDays(daysInMonth - 1);

        var isCurrent = today >= monthStart && today <= monthEnd;
        var isPast = today > monthEnd;

        int daysElapsed;
        int daysRemaining;
        if (isCurrent) { daysElapsed = today.Day; daysRemaining = daysInMonth - today.Day; }
        else if (isPast) { daysElapsed = daysInMonth; daysRemaining = 0; }
        else { daysElapsed = 0; daysRemaining = daysInMonth; }

        var burnDaily = daysElapsed > 0 ? totalSpent / daysElapsed : 0m;
        var projectedSpend = isCurrent ? burnDaily * daysInMonth : totalSpent;
        var remainingBudget = ingresoBase - totalSpent;
        var allowedDaily = daysRemaining > 0 ? Math.Max(0m, remainingBudget / daysRemaining) : 0m;
        var projectedSurplus = ingresoBase - projectedSpend;

        string headline;
        if (!isCurrent && !isPast)
            headline = "Mes futuro — sin proyección aún.";
        else if (isPast)
            headline = totalSpent <= ingresoBase
                ? $"Cerró con superávit de {(ingresoBase - totalSpent):C0}."
                : $"Cerró con déficit de {(totalSpent - ingresoBase):C0}.";
        else if (ingresoBase <= 0)
            headline = "Define tu ingreso para ver proyección.";
        else if (projectedSpend <= ingresoBase)
            headline = $"A este ritmo cerrarás con {(ingresoBase - projectedSpend):C0} de sobrante.";
        else
            headline = $"⚠️ A este ritmo te faltarán {(projectedSpend - ingresoBase):C0} este mes.";

        return new MonthProjection(
            isCurrent, daysElapsed, daysRemaining, daysInMonth,
            burnDaily, projectedSpend, allowedDaily, projectedSurplus, headline);
    }

    public async Task UpsertBudgetAsync(int categoryId, int year, int month, decimal amount)
    {
        var existing = await _db.Budgets
            .FirstOrDefaultAsync(b => b.CategoryId == categoryId && b.Year == year && b.Month == month);
        if (existing == null)
        {
            _db.Budgets.Add(new BudgetItem { CategoryId = categoryId, Year = year, Month = month, Amount = amount });
        }
        else
        {
            existing.Amount = amount;
        }
        await _db.SaveChangesAsync();
    }

    public async Task CopyBudgetFromPreviousMonthAsync(int year, int month)
    {
        var prev = new DateTime(year, month, 1).AddMonths(-1);
        var prevItems = await _db.Budgets
            .Where(b => b.Year == prev.Year && b.Month == prev.Month)
            .ToListAsync();
        if (prevItems.Count == 0) return;

        var existing = await _db.Budgets
            .Where(b => b.Year == year && b.Month == month)
            .Select(b => b.CategoryId)
            .ToListAsync();

        foreach (var p in prevItems.Where(p => !existing.Contains(p.CategoryId)))
        {
            _db.Budgets.Add(new BudgetItem
            {
                CategoryId = p.CategoryId, Year = year, Month = month, Amount = p.Amount,
            });
        }
        await _db.SaveChangesAsync();
    }
}
