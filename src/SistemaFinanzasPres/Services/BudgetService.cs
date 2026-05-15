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

public record BudgetSnapshot(
    AppConfig Config,
    decimal IngresoTotal,
    decimal IngresoDisponible,
    decimal TotalBudgeted,
    decimal TotalSpent,
    decimal TotalAvailable,
    decimal PercentExecuted,
    IReadOnlyList<CategoryStatus> Categories,
    IReadOnlyList<PillarSummary> Pillars);

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
        var ingresoTotal = config.IngresoTotal;
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

        return new BudgetSnapshot(
            config, ingresoTotal, ingresoDisponible,
            totalB, totalS, totalB - totalS, pctExec,
            rows, pillars);
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
