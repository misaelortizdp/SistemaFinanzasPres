using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public class SavingsGoalService
{
    private readonly AppDbContext _db;

    public SavingsGoalService(AppDbContext db) => _db = db;

    public async Task<List<SavingsGoal>> GetActiveAsync()
        => await _db.SavingsGoals.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Id)
            .ToListAsync();

    public async Task<SavingsGoal?> GetByIdAsync(int id)
        => await _db.SavingsGoals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);

    public async Task AddAsync(SavingsGoal goal)
    {
        _db.SavingsGoals.Add(goal);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SavingsGoal goal)
    {
        _db.SavingsGoals.Update(goal);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var g = await _db.SavingsGoals.FindAsync(id);
        if (g == null) return;
        _db.SavingsGoals.Remove(g);
        await _db.SaveChangesAsync();
    }

    public async Task RegisterContributionAsync(int goalId, decimal amount)
    {
        var g = await _db.SavingsGoals.FindAsync(goalId);
        if (g == null) return;
        g.Achieved += amount;
        await _db.SaveChangesAsync();
    }
}
