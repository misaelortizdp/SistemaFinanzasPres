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
        StrategyOptions = new List<string> { "Avalancha (mayor interés)", "Bola de nieve (menor saldo)" };
        SelectedStrategy = StrategyOptions[0];
    }

    [ObservableProperty] private string totalLabel = "$0";
    [ObservableProperty] private string minPaymentLabel = "$0";
    [ObservableProperty] private string weightedRateLabel = "0%";
    [ObservableProperty] private int activeCount;

    [ObservableProperty] private string extraMonthlyText = "0";
    [ObservableProperty] private string selectedStrategy = string.Empty;
    [ObservableProperty] private string simulationSummary = "Define un aporte extra mensual y simula.";
    [ObservableProperty] private bool hasSimulation;

    public List<string> StrategyOptions { get; }
    public ObservableCollection<DebtRow> Items { get; } = new();
    public ObservableCollection<PlanItemRow> PlanItems { get; } = new();

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

            Items.Clear();
            foreach (var d in debts)
            {
                var pagoMensual = d.MinPayment + d.ExtraPayment;
                Items.Add(new DebtRow(
                    d.Id, d.Name,
                    d.CurrentBalance, d.CurrentBalance.ToString("C0"),
                    d.OriginalAmount, d.OriginalAmount.ToString("C0"),
                    d.InterestRate, (d.InterestRate / 100m).ToString("P1"),
                    d.MinPayment, d.MinPayment.ToString("C0"),
                    d.DueDay,
                    d.OriginalAmount > 0 ? (decimal)(1 - d.CurrentBalance / d.OriginalAmount) : 0m,
                    d.IsActive ? "" : "✓ Pagada",
                    d.IsActive ? $"📋 Presupuesto mensual: {pagoMensual:C0}" : string.Empty));
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

    [RelayCommand]
    private async Task SimulateAsync()
    {
        if (!decimal.TryParse(ExtraMonthlyText, out var extra) || extra < 0) extra = 0m;
        var strat = SelectedStrategy.StartsWith("Avalancha")
            ? PayoffStrategy.Avalanche
            : PayoffStrategy.Snowball;

        var result = await _debts.SimulateAsync(strat, extra);
        SimulationSummary = result.Summary;
        HasSimulation = result.Items.Count > 0;

        PlanItems.Clear();
        foreach (var item in result.Items)
        {
            var years = item.MonthsToPayoff / 12;
            var months = item.MonthsToPayoff % 12;
            var label = years > 0 ? $"{years}a {months}m" : $"{months}m";
            PlanItems.Add(new PlanItemRow(
                item.Name, item.MonthsToPayoff, label,
                item.TotalInterest, item.TotalInterest.ToString("C0")));
        }
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
    string PresupuestoLabel);

public record PlanItemRow(string Name, int Months, string MonthsLabel, decimal Interest, string InterestLabel);
