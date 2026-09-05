using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

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

    // Fórmula de amortización estándar (la misma que usa el Excel de referencia): con un pago
    // mensual fijo, cuántos meses hacen falta para llevar el saldo a 0. Null cuando el pago no
    // alcanza ni para cubrir el interés del mes — con ese pago la deuda nunca baja.
    public static int? MonthsToPayoff(Debt d)
    {
        if (d.CurrentBalance <= 0) return 0;
        var payment = d.MinPayment + d.ExtraPayment;
        if (payment <= 0) return null;

        var monthlyRate = d.InterestRate / 100m / 12m;
        if (monthlyRate == 0) return (int)Math.Ceiling(d.CurrentBalance / payment);

        var monthlyInterest = d.CurrentBalance * monthlyRate;
        if (payment <= monthlyInterest) return null;

        var months = -Math.Log(1 - (double)(monthlyInterest / payment)) / Math.Log(1 + (double)monthlyRate);
        return (int)Math.Ceiling(months);
    }

    // Prioridad avalancha: 1 = mayor tasa de interés, solo entre las que aún tienen saldo.
    public static Dictionary<int, int> AvalanchePriority(IEnumerable<Debt> debts)
    {
        return debts
            .Where(d => d.IsActive && d.CurrentBalance > 0)
            .OrderByDescending(d => d.InterestRate)
            .Select((d, i) => (d.Id, Priority: i + 1))
            .ToDictionary(x => x.Id, x => x.Priority);
    }
}
