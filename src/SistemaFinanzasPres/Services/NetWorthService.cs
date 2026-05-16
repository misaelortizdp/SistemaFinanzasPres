using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Services;

public record NetWorthLine(string Name, decimal Amount);

public record NetWorthBreakdown(
    IReadOnlyList<NetWorthLine> Assets,
    IReadOnlyList<NetWorthLine> Liabilities,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal NetWorth);

public class NetWorthService
{
    private readonly AppDbContext _db;

    public NetWorthService(AppDbContext db) => _db = db;

    public async Task<NetWorthBreakdown> GetCurrentAsync()
    {
        var accounts = await _db.Accounts.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .ToListAsync();

        var debts = await _db.Debts.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.CurrentBalance)
            .ToListAsync();

        var assets = accounts.Select(a => new NetWorthLine(a.Name, a.Balance)).ToList();
        var liabilities = debts.Select(d => new NetWorthLine(d.Name, d.CurrentBalance)).ToList();

        var totalA = assets.Sum(x => x.Amount);
        var totalL = liabilities.Sum(x => x.Amount);

        return new NetWorthBreakdown(assets, liabilities, totalA, totalL, totalA - totalL);
    }

    public async Task<List<NetWorthSnapshot>> GetSnapshotsAsync()
        => await _db.NetWorthSnapshots.AsNoTracking()
            .OrderByDescending(s => s.Date).ThenByDescending(s => s.Id)
            .ToListAsync();

    public async Task<NetWorthSnapshot> TakeSnapshotAsync(string? notes = null)
    {
        var b = await GetCurrentAsync();
        var snap = new NetWorthSnapshot
        {
            Date = DateTime.Today,
            Assets = b.TotalAssets,
            Liabilities = b.TotalLiabilities,
            NetWorth = b.NetWorth,
            Notes = notes,
        };
        _db.NetWorthSnapshots.Add(snap);
        await _db.SaveChangesAsync();
        return snap;
    }

    public async Task DeleteSnapshotAsync(int id)
    {
        var s = await _db.NetWorthSnapshots.FindAsync(id);
        if (s == null) return;
        _db.NetWorthSnapshots.Remove(s);
        await _db.SaveChangesAsync();
    }
}
