using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;

namespace SistemaFinanzasPres.ViewModels;

[QueryProperty(nameof(DebtId), "id")]
public partial class DebtEditViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly DebtService _debts;

    public DebtEditViewModel(AppDbContext db, DebtService debts)
    {
        _db = db;
        _debts = debts;
        Title = "Nueva deuda";
    }

    [ObservableProperty] private int debtId;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string originalText = "0";
    [ObservableProperty] private string balanceText = "0";
    [ObservableProperty] private string rateText = "0";
    [ObservableProperty] private string minPaymentText = "0";
    [ObservableProperty] private int dueDay = 1;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private string? notes;

    partial void OnDebtIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (DebtId <= 0) return;
        var d = await _db.Debts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == DebtId);
        if (d == null) return;
        Name = d.Name;
        OriginalText = d.OriginalAmount.ToString("0.##");
        BalanceText = d.CurrentBalance.ToString("0.##");
        RateText = d.InterestRate.ToString("0.##");
        MinPaymentText = d.MinPayment.ToString("0.##");
        DueDay = d.DueDay;
        IsActive = d.IsActive;
        Notes = d.Notes;
        Title = "Editar deuda";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Falta", "Escribe un nombre.", "OK");
            return;
        }
        decimal.TryParse(OriginalText, out var original);
        decimal.TryParse(BalanceText, out var balance);
        decimal.TryParse(RateText, out var rate);
        decimal.TryParse(MinPaymentText, out var min);

        if (balance < 0)
        {
            await Shell.Current.DisplayAlert("Falta", "Saldo inválido.", "OK");
            return;
        }
        if (original <= 0) original = balance;

        if (DebtId > 0)
        {
            var existing = await _db.Debts.FindAsync(DebtId);
            if (existing == null) return;
            existing.Name = Name.Trim();
            existing.OriginalAmount = original;
            existing.CurrentBalance = balance;
            existing.InterestRate = rate;
            existing.MinPayment = min;
            existing.DueDay = Math.Clamp(DueDay, 1, 31);
            existing.IsActive = IsActive;
            existing.Notes = Notes;
            await _debts.UpdateDebtAsync(existing);
        }
        else
        {
            await _debts.AddDebtAsync(new Debt
            {
                Name = Name.Trim(),
                OriginalAmount = original,
                CurrentBalance = balance,
                InterestRate = rate,
                MinPayment = min,
                DueDay = Math.Clamp(DueDay, 1, 31),
                IsActive = balance > 0,
                CreatedAt = DateTime.Today,
                Notes = Notes,
            });
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}
