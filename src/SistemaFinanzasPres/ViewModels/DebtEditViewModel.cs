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
    [ObservableProperty] private string extraPaymentText = "0";
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
        ExtraPaymentText = d.ExtraPayment.ToString("0.##");
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
        decimal.TryParse(ExtraPaymentText, out var extra);

        if (balance < 0)
        {
            await Shell.Current.DisplayAlert("Falta", "Saldo inválido.", "OK");
            return;
        }
        if (original <= 0) original = balance;

        var nombreNuevo = Name.Trim();

        if (DebtId > 0)
        {
            var existing = await _db.Debts.FindAsync(DebtId);
            if (existing == null) return;

            var nombreAnterior = existing.Name;
            if (nombreAnterior != nombreNuevo &&
                await _db.Categories.AnyAsync(c => c.Name == nombreNuevo && c.Id != existing.CategoryId))
            {
                await Shell.Current.DisplayAlert("Nombre repetido", "Ya existe una categoría con ese nombre. Usa un nombre distinto para la deuda.", "OK");
                return;
            }

            existing.Name = nombreNuevo;
            existing.OriginalAmount = original;
            existing.CurrentBalance = balance;
            existing.InterestRate = rate;
            existing.MinPayment = min;
            existing.ExtraPayment = extra;
            existing.DueDay = Math.Clamp(DueDay, 1, 31);
            existing.IsActive = IsActive;
            existing.Notes = Notes;
            await _debts.UpdateDebtAsync(existing, nombreAnterior);
        }
        else
        {
            if (await _db.Categories.AnyAsync(c => c.Name == nombreNuevo))
            {
                await Shell.Current.DisplayAlert("Nombre repetido", "Ya existe una categoría con ese nombre. Usa un nombre distinto para la deuda.", "OK");
                return;
            }

            await _debts.AddDebtAsync(new Debt
            {
                Name = nombreNuevo,
                OriginalAmount = original,
                CurrentBalance = balance,
                InterestRate = rate,
                MinPayment = min,
                ExtraPayment = extra,
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
