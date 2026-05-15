using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.ViewModels;

[QueryProperty(nameof(Id), "id")]
public partial class TransactionEditViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    public TransactionEditViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Nuevo gasto";
    }

    [ObservableProperty] private int? id;
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private string concept = string.Empty;
    [ObservableProperty] private string amountText = string.Empty;
    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private Account? selectedAccount;
    [ObservableProperty] private string? notes;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Account> Accounts { get; } = new();

    partial void OnIdChanged(int? value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (Categories.Count == 0)
        {
            var cats = await _db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync();
            foreach (var c in cats) Categories.Add(c);
        }
        if (Accounts.Count == 0)
        {
            var accs = await _db.Accounts.AsNoTracking().OrderBy(a => a.SortOrder).ToListAsync();
            foreach (var a in accs) Accounts.Add(a);
        }

        if (Id is int idVal && idVal > 0)
        {
            var t = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == idVal);
            if (t != null)
            {
                Title = "Editar gasto";
                Date = t.Date;
                Concept = t.Concept;
                AmountText = t.Amount.ToString("N0");
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == t.CategoryId);
                SelectedAccount = Accounts.FirstOrDefault(a => a.Id == t.AccountId);
                Notes = t.Notes;
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedCategory == null)
        {
            await Shell.Current.DisplayAlert("Validación", "Selecciona una categoría.", "OK");
            return;
        }
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlert("Validación", "Ingresa un valor válido (> 0).", "OK");
            return;
        }

        if (Id is int idVal && idVal > 0)
        {
            var t = await _db.Transactions.FirstOrDefaultAsync(x => x.Id == idVal);
            if (t == null) return;
            t.Date = Date;
            t.Concept = Concept ?? string.Empty;
            t.CategoryId = SelectedCategory.Id;
            t.AccountId = SelectedAccount?.Id;
            t.Amount = amount;
            t.Notes = Notes;
        }
        else
        {
            _db.Transactions.Add(new Transaction
            {
                Date = Date,
                Concept = Concept ?? string.Empty,
                CategoryId = SelectedCategory.Id,
                AccountId = SelectedAccount?.Id,
                Amount = amount,
                Notes = Notes,
            });
        }
        await _db.SaveChangesAsync();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}
