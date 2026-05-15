using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly BudgetService _budget;
    private readonly MonthService _month;

    public DashboardViewModel(BudgetService budget, MonthService month)
    {
        _budget = budget;
        _month = month;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "Dashboard";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string ingreso = "$0";
    [ObservableProperty] private string totalGastado = "$0";
    [ObservableProperty] private string disponible = "$0";
    [ObservableProperty] private string pctEjecutado = "0%";
    [ObservableProperty] private string tasaAhorro = "0%";

    public ObservableCollection<PillarRow> Pillars { get; } = new();
    public ObservableCollection<CategoryRow> Categories { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;
            var snap = await _budget.GetSnapshotAsync(_month.Year, _month.Month);

            Ingreso = snap.IngresoTotal.ToString("C0");
            TotalGastado = snap.TotalSpent.ToString("C0");
            Disponible = (snap.IngresoTotal - snap.TotalSpent).ToString("C0");
            PctEjecutado = (snap.IngresoTotal > 0 ? snap.TotalSpent / snap.IngresoTotal : 0m).ToString("P1");
            var ahorro = snap.Pillars.First(p => p.Pillar == Models.Pillar.Ahorro);
            TasaAhorro = (snap.IngresoTotal > 0 ? ahorro.Spent / snap.IngresoTotal : 0m).ToString("P1");

            Pillars.Clear();
            foreach (var p in snap.Pillars)
            {
                Pillars.Add(new PillarRow(
                    p.Pillar.Display(),
                    p.Budgeted.ToString("C0"),
                    p.Spent.ToString("C0"),
                    p.MetaAmount.ToString("C0"),
                    p.PercentOfIncomeBase.ToString("P1"),
                    p.Status));
            }

            Categories.Clear();
            foreach (var c in snap.Categories.OrderBy(c => c.Pillar).ThenBy(c => c.CategoryName))
            {
                Categories.Add(new CategoryRow(
                    c.CategoryName, c.Pillar.Short(),
                    c.Budgeted.ToString("C0"),
                    c.Spent.ToString("C0"),
                    c.Available.ToString("C0"),
                    c.Status));
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);

    [RelayCommand]
    private async Task AddTransactionAsync()
        => await Shell.Current.GoToAsync(nameof(TransactionEditPage));
}

public record PillarRow(string Name, string Budgeted, string Spent, string Meta, string Pct, string Status);
public record CategoryRow(string Name, string Pillar, string Budgeted, string Spent, string Available, string Status);
