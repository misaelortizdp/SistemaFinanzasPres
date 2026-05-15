using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public class IncomeService
{
    private readonly AppDbContext _db;
    public IncomeService(AppDbContext db) => _db = db;

    public async Task<List<Income>> GetForMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await _db.Incomes.AsNoTracking()
            .Where(i => i.Date >= start && i.Date < end)
            .OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalForMonthAsync(int year, int month)
    {
        var list = await GetForMonthAsync(year, month);
        return list.Sum(i => i.Amount);
    }

    public async Task<List<string>> GetSourceSuggestionsAsync()
    {
        var fromDb = await _db.Incomes.AsNoTracking()
            .Select(i => i.Source)
            .Distinct()
            .ToListAsync();
        var defaults = new[] { "Salario", "Quincena", "Freelance", "Bono", "Comisión", "Reembolso", "Otro" };
        return defaults.Concat(fromDb)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();
    }

    public async Task AddAsync(Income income)
    {
        _db.Incomes.Add(income);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Income income)
    {
        _db.Incomes.Update(income);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _db.Incomes.FindAsync(id);
        if (existing == null) return;
        _db.Incomes.Remove(existing);
        await _db.SaveChangesAsync();
    }
}
