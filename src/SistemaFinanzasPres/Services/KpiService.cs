using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public record KpiRow(string Name, string Value, string Target, string Status, string Hint);

public record FondoEmergenciaInfo(
    decimal GastosFijos,
    int Meses,
    decimal Meta,
    decimal Acumulado,
    decimal Avance,
    string Status);

public class KpiService
{
    private readonly AppDbContext _db;
    private readonly BudgetService _budget;

    public KpiService(AppDbContext db, BudgetService budget)
    {
        _db = db;
        _budget = budget;
    }

    public async Task<(IReadOnlyList<KpiRow> Rows, FondoEmergenciaInfo Fondo)> GetKpisAsync(int year, int month)
    {
        var snap = await _budget.GetSnapshotAsync(year, month);
        var cfg = snap.Config;

        var ahorroPilar = snap.Pillars.First(p => p.Pillar == Pillar.Ahorro);
        var necPilar = snap.Pillars.First(p => p.Pillar == Pillar.Necesidad);
        var desPilar = snap.Pillars.First(p => p.Pillar == Pillar.Deseo);

        var tasaAhorro = snap.IngresoTotal > 0 ? ahorroPilar.Spent / snap.IngresoTotal : 0m;

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var deseosCatIds = snap.Categories
            .Where(c => c.Pillar == Pillar.Deseo)
            .Select(c => c.CategoryId)
            .ToList();

        var hormigaTx = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= start && t.Date < end && deseosCatIds.Contains(t.CategoryId))
            .Select(t => new { t.Amount, t.Date })
            .ToListAsync();
        var hormiga = hormigaTx.Sum(x => x.Amount);

        var dias = await _db.Transactions.AsNoTracking()
            .Where(t => t.Date >= start && t.Date < end)
            .Select(t => t.Date.Date)
            .Distinct()
            .CountAsync();

        var rows = new List<KpiRow>
        {
            new("💰 Tasa de Ahorro",
                tasaAhorro.ToString("P1"),
                "≥ 20%",
                tasaAhorro >= 0.20m ? "✅ Excelente" :
                tasaAhorro >= 0.10m ? "🟡 Bajo meta" : "🔴 Crítico",
                "Porcentaje del ingreso destinado a ahorro e inversión"),

            new("🏦 Ahorro Mensual",
                ahorroPilar.Spent.ToString("C0"),
                (snap.IngresoTotal * cfg.MetaAhorroMinimoPct).ToString("C0"),
                "—",
                "Suma de ahorro líquido + fondo emergencia + inversión"),

            new("🛒 % Gasto en Necesidades",
                necPilar.PercentOfIncomeBase.ToString("P1"),
                "≤ 50%",
                necPilar.PercentOfIncomeBase <= 0.50m ? "✅ OK" : "🔴 Sobre 50%",
                "Sobre ingreso disponible (sin diezmo)"),

            new("🎉 % Gasto en Deseos",
                desPilar.PercentOfIncomeBase.ToString("P1"),
                "≤ 30%",
                desPilar.PercentOfIncomeBase <= 0.30m ? "✅ OK" : "🔴 Sobre 30%",
                "Sobre ingreso disponible (sin diezmo)"),

            new("🐜 Gasto en Deseos (acumulado)",
                hormiga.ToString("C0"),
                desPilar.MetaAmount.ToString("C0"),
                hormiga <= desPilar.MetaAmount ? "✅ Controlado" : "🟡 Revisar",
                "Suma de gastos del pilar Deseos en el mes"),

            new("📅 Días con registro",
                dias.ToString(),
                "30 días",
                "—",
                "Constancia en el registro = mejor control"),
        };

        var gastosFijos = necPilar.Budgeted;
        var metaFondo = gastosFijos * cfg.FondoEmergenciaMeses;
        var fondoBalanceRaw = await _db.Transactions.AsNoTracking()
            .Where(t => t.Category != null && t.Category.Name == "Fondo de Emergencia")
            .Select(t => t.Amount)
            .ToListAsync();
        var fondoBalance = fondoBalanceRaw.Sum();
        var avance = metaFondo > 0 ? fondoBalance / metaFondo : 0m;
        string fondoStatus =
            fondoBalance >= metaFondo ? "✅ Meta cumplida" :
            fondoBalance >= metaFondo * 0.5m ? "🟡 En progreso" :
            "🔴 Por construir";

        var fondo = new FondoEmergenciaInfo(gastosFijos, cfg.FondoEmergenciaMeses, metaFondo, fondoBalance, avance, fondoStatus);
        return (rows, fondo);
    }
}
