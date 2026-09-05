using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class DebtsViewModel : BaseViewModel
{
    private readonly DebtService _debts;

    public DebtsViewModel(DebtService debts)
    {
        _debts = debts;
        Title = "Deudas";
    }

    [ObservableProperty] private string totalLabel = "$0";
    [ObservableProperty] private string minPaymentLabel = "$0";
    [ObservableProperty] private string weightedRateLabel = "0%";
    [ObservableProperty] private int activeCount;
    [ObservableProperty] private string freedomLabel = "—";

    public ObservableCollection<DebtRow> Items { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var debts = await _debts.GetAllAsync(activeOnly: false);
            var overview = await _debts.GetOverviewAsync();

            TotalLabel = overview.TotalBalance.ToString("C0");
            MinPaymentLabel = overview.TotalMinPayment.ToString("C0");
            WeightedRateLabel = (overview.WeightedRate / 100m).ToString("P1");
            ActiveCount = overview.ActiveCount;

            var priorities = DebtService.AvalanchePriority(debts);
            var months = debts.ToDictionary(d => d.Id, d => DebtService.MonthsToPayoff(d));
            var computableMonths = debts
                .Where(d => d.IsActive)
                .Select(d => months[d.Id])
                .Where(m => m.HasValue)
                .Select(m => m!.Value)
                .ToList();
            FreedomLabel = computableMonths.Count > 0 ? $"{computableMonths.Max()} meses" : "—";

            Items.Clear();
            foreach (var d in debts)
            {
                var pagoMensual = d.MinPayment + d.ExtraPayment;
                var payoffLabel = string.Empty;
                if (d.IsActive && priorities.TryGetValue(d.Id, out var priority))
                {
                    payoffLabel = months[d.Id] is int m
                        ? $"🎯 Prioridad #{priority} · {m} meses para liquidar"
                        : $"🎯 Prioridad #{priority} · con el pago actual no baja";
                }

                Items.Add(new DebtRow(
                    d.Id, d.Name,
                    d.CurrentBalance, d.CurrentBalance.ToString("C0"),
                    d.OriginalAmount, d.OriginalAmount.ToString("C0"),
                    d.InterestRate, (d.InterestRate / 100m).ToString("P1"),
                    d.MinPayment, d.MinPayment.ToString("C0"),
                    d.DueDay,
                    d.OriginalAmount > 0 ? (decimal)(1 - d.CurrentBalance / d.OriginalAmount) : 0m,
                    d.IsActive ? "" : "✓ Pagada",
                    d.IsActive ? $"📋 Presupuesto mensual: {pagoMensual:C0}" : string.Empty,
                    payoffLabel));
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
        => await Shell.Current.GoToAsync(nameof(DebtEditPage));

    [RelayCommand]
    private async Task EditAsync(DebtRow? row)
    {
        if (row is null) return;
        await Shell.Current.GoToAsync($"{nameof(DebtEditPage)}?id={row.Id}");
    }

    [RelayCommand]
    private async Task PayAsync(DebtRow? row)
    {
        if (row is null) return;
        var input = await Shell.Current.DisplayPromptAsync(
            $"Pago a {row.Name}",
            $"Saldo actual: {row.BalanceLabel}\nSugerido (min): {row.MinPaymentLabel}",
            "Registrar", "Cancelar",
            initialValue: row.MinPayment.ToString("0.##"),
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(input)) return;
        if (!decimal.TryParse(input, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlert("Pago", "Monto inválido.", "OK");
            return;
        }

        await _debts.RegisterPaymentAsync(row.Id, DateTime.Today, amount, null);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(DebtRow? row)
    {
        if (row is null) return;
        var ok = await Shell.Current.DisplayAlert(
            "Eliminar",
            $"¿Eliminar deuda '{row.Name}'? Se borrarán también sus pagos.",
            "Sí", "No");
        if (!ok) return;
        await _debts.DeleteDebtAsync(row.Id);
        await LoadAsync();
    }
}

public record DebtRow(
    int Id, string Name,
    decimal Balance, string BalanceLabel,
    decimal Original, string OriginalLabel,
    decimal Rate, string RateLabel,
    decimal MinPayment, string MinPaymentLabel,
    int DueDay,
    decimal Progress,
    string StatusLabel,
    string PresupuestoLabel,
    string PayoffLabel);
