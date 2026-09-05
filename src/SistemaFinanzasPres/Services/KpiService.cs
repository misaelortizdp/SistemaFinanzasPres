using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public record KpiRow(string Name, string Value, string Target, string Status, string Hint, double Progress = 0d);

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
        var deudaPilar = snap.Pillars.First(p => p.Pillar == Pillar.Deuda);

        var tasaAhorro = snap.IngresoDisponible > 0 ? ahorroPilar.Spent / snap.IngresoDisponible : 0m;

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

        var ahorroProgress = (double)Math.Min(1m, snap.IngresoDisponible > 0 ? tasaAhorro / 0.20m : 0m);
        var necProgress = (double)Math.Min(1m, necPilar.PercentOfIncomeBase / 0.50m);
        var desProgress = (double)Math.Min(1m, desPilar.PercentOfIncomeBase / 0.30m);
        var hormigaProgress = desPilar.MetaAmount > 0
            ? (double)Math.Min(1m, hormiga / desPilar.MetaAmount) : 0d;

        var rows = new List<KpiRow>
        {
            new("💰 Tasa de Ahorro",
                tasaAhorro.ToString("P1"),
                "≥ 20%",
                tasaAhorro >= 0.20m ? "✅ Excelente" :
                tasaAhorro >= 0.10m ? "🟡 Bajo meta" : "🔴 Crítico",
                "Porcentaje del ingreso destinado a ahorro e inversión",
                ahorroProgress),

            new("🏦 Ahorro Mensual",
                ahorroPilar.Spent.ToString("C0"),
                (snap.IngresoDisponible * cfg.MetaAhorroMinimoPct).ToString("C0"),
                ahorroPilar.Spent >= snap.IngresoDisponible * cfg.MetaAhorroMinimoPct ? "✅ Meta alcanzada" : "🟡 En progreso",
                "Suma de ahorro líquido + fondo emergencia + inversión",
                snap.IngresoDisponible * cfg.MetaAhorroMinimoPct > 0
                    ? (double)Math.Min(1m, ahorroPilar.Spent / (snap.IngresoDisponible * cfg.MetaAhorroMinimoPct)) : 0d),

            new("🛒 % Gasto en Necesidades",
                necPilar.PercentOfIncomeBase.ToString("P1"),
                "≤ 50%",
                necPilar.PercentOfIncomeBase <= 0.50m ? "✅ OK" : "🔴 Sobre 50%",
                "Sobre ingreso disponible (sin diezmo). Menor es mejor.",
                necProgress),

            new("🎉 % Gasto en Deseos",
                desPilar.PercentOfIncomeBase.ToString("P1"),
                "≤ 30%",
                desPilar.PercentOfIncomeBase <= 0.30m ? "✅ OK" : "🔴 Sobre 30%",
                "Sobre ingreso disponible (sin diezmo). Menor es mejor.",
                desProgress),

            new("🐜 Gastos en Deseos",
                hormiga.ToString("C0"),
                desPilar.MetaAmount.ToString("C0"),
                hormiga <= desPilar.MetaAmount ? "✅ Controlado" : "🟡 Revisar",
                "Suma total de gastos del pilar Deseos vs el presupuesto.",
                hormigaProgress),

            new("📅 Días con registro",
                dias.ToString(),
                $"{DateTime.DaysInMonth(year, month)} días",
                dias >= 20 ? "✅ Constante" : dias >= 10 ? "🟡 Regular" : "🔴 Bajo",
                "Constancia en el registro = mejor control",
                (double)Math.Min(1d, dias / (double)DateTime.DaysInMonth(year, month))),
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

        if (deudaPilar.MetaPct > 0 || deudaPilar.Spent > 0)
        {
            rows.Add(new KpiRow(
                "💳 % Gasto en Deuda",
                deudaPilar.PercentOfIncomeBase.ToString("P1"),
                deudaPilar.MetaPct.ToString("P0"),
                deudaPilar.PercentOfIncomeBase <= deudaPilar.MetaPct ? "✅ OK" : "🔴 Sobre meta",
                "Pago de deudas sobre ingreso disponible (sin diezmo). Menor es mejor.",
                (double)Math.Min(1m, deudaPilar.MetaPct > 0 ? deudaPilar.PercentOfIncomeBase / deudaPilar.MetaPct : 0m)));
        }

        var debtTotal = await _db.Debts.AsNoTracking()
            .Where(d => d.IsActive)
            .SumAsync(d => (double)d.CurrentBalance);
        var debtTotalDec = (decimal)debtTotal;
        if (debtTotalDec > 0)
        {
            rows.Add(new KpiRow(
                "💳 Total en deudas",
                debtTotalDec.ToString("C0"),
                "$0",
                debtTotalDec == 0 ? "✅ Sin deudas 🎉" : "🔴 Activas",
                "Suma de saldos de todas tus deudas activas. Meta: $0.",
                0d));
        }

        return (rows, fondo);
    }
}
