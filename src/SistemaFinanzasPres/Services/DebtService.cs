using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public enum PayoffStrategy { Avalanche, Snowball }

public record DebtPlanItem(int DebtId, string Name, int MonthsToPayoff, decimal TotalInterest);

public record DebtPlanResult(
    PayoffStrategy Strategy,
    int TotalMonths,
    decimal TotalInterest,
    decimal TotalPaid,
    IReadOnlyList<DebtPlanItem> Items,
    string Summary);

public record DebtsOverview(
    decimal TotalBalance,
    decimal TotalMinPayment,
    decimal WeightedRate,
    int ActiveCount);

public class DebtService
{
    private readonly AppDbContext _db;
    public DebtService(AppDbContext db) => _db = db;

    public async Task<List<Debt>> GetAllAsync(bool activeOnly = true)
    {
        var q = _db.Debts.AsNoTracking().AsQueryable();
        if (activeOnly) q = q.Where(d => d.IsActive);
        return await q.OrderByDescending(d => d.InterestRate).ThenBy(d => d.Name).ToListAsync();
    }

    public async Task<DebtsOverview> GetOverviewAsync()
    {
        var debts = await GetAllAsync();
        var total = debts.Sum(d => d.CurrentBalance);
        var minPay = debts.Sum(d => d.MinPayment);
        var weighted = total > 0
            ? debts.Sum(d => d.CurrentBalance * d.InterestRate) / total
            : 0m;
        return new DebtsOverview(total, minPay, weighted, debts.Count);
    }

    public async Task<List<DebtPayment>> GetPaymentsAsync(int debtId)
    {
        return await _db.DebtPayments.AsNoTracking()
            .Where(p => p.DebtId == debtId)
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task AddDebtAsync(Debt d)
    {
        if (d.OriginalAmount <= 0) d.OriginalAmount = d.CurrentBalance;

        // Cada deuda tiene su propia categoría de presupuesto (pilar Deuda), creada
        // automáticamente — así el usuario no tiene que duplicar el alta a mano.
        d.Category = new Category
        {
            Name = d.Name,
            Pillar = Pillar.Deuda,
            IsActive = d.IsActive,
        };
        _db.Debts.Add(d);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateDebtAsync(Debt d, string? nombreAnterior = null)
    {
        if (d.CategoryId != null && nombreAnterior != null && nombreAnterior != d.Name)
        {
            var cat = await _db.Categories.FindAsync(d.CategoryId.Value);
            if (cat != null) cat.Name = d.Name;
        }
        _db.Debts.Update(d);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteDebtAsync(int id)
    {
        var existing = await _db.Debts.FindAsync(id);
        if (existing == null) return;

        // La categoría se desactiva en vez de borrarse: preserva el histórico de
        // transacciones ya registradas contra ella.
        if (existing.CategoryId != null)
        {
            var cat = await _db.Categories.FindAsync(existing.CategoryId.Value);
            if (cat != null) cat.IsActive = false;
        }

        _db.Debts.Remove(existing);
        await _db.SaveChangesAsync();
    }

    public async Task RegisterPaymentAsync(int debtId, DateTime date, decimal amount, string? notes)
    {
        var debt = await _db.Debts.FindAsync(debtId);
        if (debt == null) return;

        var monthlyRate = debt.InterestRate / 12m / 100m;
        var interestPortion = Math.Round(debt.CurrentBalance * monthlyRate, 2);
        if (interestPortion > amount) interestPortion = amount;
        var principalPortion = amount - interestPortion;

        debt.CurrentBalance = Math.Max(0m, debt.CurrentBalance - principalPortion);
        if (debt.CurrentBalance == 0m) debt.IsActive = false;

        _db.DebtPayments.Add(new DebtPayment
        {
            DebtId = debtId,
            Date = date,
            Amount = amount,
            InterestPortion = interestPortion,
            PrincipalPortion = principalPortion,
            Notes = notes,
        });
        await _db.SaveChangesAsync();
    }

    public async Task<DebtPlanResult> SimulateAsync(PayoffStrategy strategy, decimal extraMonthly)
    {
        var debts = await GetAllAsync();
        return Simulate(debts, strategy, extraMonthly);
    }

    public static DebtPlanResult Simulate(IReadOnlyList<Debt> debtsSource, PayoffStrategy strategy, decimal extraMonthly)
    {
        var debts = debtsSource
            .Where(d => d.CurrentBalance > 0)
            .Select(d => new SimDebt
            {
                Id = d.Id,
                Name = d.Name,
                Balance = d.CurrentBalance,
                Rate = d.InterestRate / 12m / 100m,
                MinPayment = d.MinPayment,
                MonthsToZero = 0,
                InterestPaid = 0m,
            })
            .ToList();

        if (debts.Count == 0)
            return new DebtPlanResult(strategy, 0, 0m, 0m, Array.Empty<DebtPlanItem>(),
                "No tienes deudas activas. 🎉");

        int month = 0;
        decimal totalInterest = 0m;
        decimal totalPaid = 0m;
        const int MaxMonths = 600;

        while (debts.Any(d => d.Balance > 0) && month < MaxMonths)
        {
            month++;
            decimal extra = extraMonthly;

            foreach (var d in debts.Where(x => x.Balance > 0))
            {
                var interest = Math.Round(d.Balance * d.Rate, 2);
                d.InterestPaid += interest;
                totalInterest += interest;
                d.Balance += interest;

                var pay = Math.Min(d.MinPayment, d.Balance);
                d.Balance -= pay;
                totalPaid += pay;
            }

            var queue = strategy == PayoffStrategy.Avalanche
                ? debts.Where(d => d.Balance > 0).OrderByDescending(d => d.Rate).ToList()
                : debts.Where(d => d.Balance > 0).OrderBy(d => d.Balance).ToList();

            foreach (var d in queue)
            {
                if (extra <= 0) break;
                var pay = Math.Min(extra, d.Balance);
                d.Balance -= pay;
                extra -= pay;
                totalPaid += pay;
            }

            foreach (var d in debts.Where(d => d.Balance <= 0.01m && d.MonthsToZero == 0))
            {
                d.Balance = 0m;
                d.MonthsToZero = month;
            }
        }

        var items = debts
            .OrderBy(d => d.MonthsToZero == 0 ? int.MaxValue : d.MonthsToZero)
            .Select(d => new DebtPlanItem(
                d.Id, d.Name,
                d.MonthsToZero == 0 ? month : d.MonthsToZero,
                Math.Round(d.InterestPaid, 2)))
            .ToList();

        var stratName = strategy == PayoffStrategy.Avalanche ? "Avalancha (mayor tasa primero)" : "Bola de nieve (menor saldo primero)";
        var summary = month >= MaxMonths
            ? $"Con esta estrategia ({stratName}), tu pago no alcanza ni para los intereses. Aumenta el aporte extra."
            : $"Con {stratName} y {extraMonthly:C0} extra/mes, serás libre de deudas en {month} meses. Intereses totales: {Math.Round(totalInterest, 2):C0}.";

        return new DebtPlanResult(strategy, month, Math.Round(totalInterest, 2), Math.Round(totalPaid, 2), items, summary);
    }

    private class SimDebt
    {
        public int Id;
        public string Name = "";
        public decimal Balance;
        public decimal Rate;
        public decimal MinPayment;
        public int MonthsToZero;
        public decimal InterestPaid;
    }
}
