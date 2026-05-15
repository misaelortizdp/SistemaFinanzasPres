using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.ViewModels;

public partial class AccountsViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    public AccountsViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Cuentas";
    }

    public ObservableCollection<Account> Items { get; } = new();
    [ObservableProperty] private string newName = string.Empty;
    [ObservableProperty] private string newBalance = "0";
    [ObservableProperty] private string totalLabel = "$0";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await _db.Accounts.AsNoTracking().OrderBy(a => a.SortOrder).ToListAsync();
        Items.Clear();
        foreach (var a in data) Items.Add(a);
        TotalLabel = data.Where(a => a.IsActive).Sum(a => a.Balance).ToString("C0");
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return;
        if (!decimal.TryParse(NewBalance, out var bal)) bal = 0m;
        var max = await _db.Accounts.MaxAsync(a => (int?)a.SortOrder) ?? 0;
        _db.Accounts.Add(new Account { Name = NewName.Trim(), Balance = bal, SortOrder = max + 1 });
        await _db.SaveChangesAsync();
        NewName = string.Empty;
        NewBalance = "0";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EditBalanceAsync(Account? a)
    {
        if (a == null) return;
        var input = await Shell.Current.DisplayPromptAsync(a.Name, "Nuevo saldo:",
            initialValue: a.Balance.ToString("0.##"), keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(input)) return;
        if (!decimal.TryParse(input, out var bal)) return;
        var entity = await _db.Accounts.FindAsync(a.Id);
        if (entity == null) return;
        entity.Balance = bal;
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RenameAsync(Account? a)
    {
        if (a == null) return;
        var nuevo = await Shell.Current.DisplayPromptAsync("Renombrar", "Nuevo nombre:", initialValue: a.Name);
        if (string.IsNullOrWhiteSpace(nuevo)) return;
        var entity = await _db.Accounts.FindAsync(a.Id);
        if (entity == null) return;
        entity.Name = nuevo.Trim();
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Account? a)
    {
        if (a == null) return;
        var ok = await Shell.Current.DisplayAlert("Eliminar", $"¿Eliminar la cuenta '{a.Name}'?", "Sí", "No");
        if (!ok) return;
        var entity = await _db.Accounts.FindAsync(a.Id);
        if (entity == null) return;
        _db.Accounts.Remove(entity);
        await _db.SaveChangesAsync();
        await LoadAsync();
    }
}
