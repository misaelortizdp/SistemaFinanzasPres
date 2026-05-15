using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Models;
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
    [ObservableProperty] private string ingresoSource = "Fijo";
    [ObservableProperty] private string totalGastado = "$0";
    [ObservableProperty] private string disponible = "$0";
    [ObservableProperty] private string pctEjecutado = "0%";
    [ObservableProperty] private string tasaAhorro = "0%";

    [ObservableProperty] private bool showProjection;
    [ObservableProperty] private string projectionHeadline = string.Empty;
    [ObservableProperty] private string projectedSpend = "$0";
    [ObservableProperty] private string allowedDaily = "$0";
    [ObservableProperty] private string burnDaily = "$0";
    [ObservableProperty] private string daysRemainingLabel = string.Empty;
    [ObservableProperty] private Color projectionColor = Colors.Gray;

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
            IngresoSource = snap.UsedTransactionalIncome ? "Ingresos registrados" : "Salario configurado";
            TotalGastado = snap.TotalSpent.ToString("C0");
            Disponible = (snap.IngresoDisponible - snap.TotalSpent).ToString("C0");
            PctEjecutado = (snap.IngresoTotal > 0 ? snap.TotalSpent / snap.IngresoTotal : 0m).ToString("P1");
            var ahorro = snap.Pillars.First(p => p.Pillar == Models.Pillar.Ahorro);
            TasaAhorro = (snap.IngresoTotal > 0 ? ahorro.Spent / snap.IngresoTotal : 0m).ToString("P1");

            var proj = snap.Projection;
            ShowProjection = proj.IsCurrentMonth && snap.IngresoDisponible > 0;
            ProjectionHeadline = proj.Headline;
            ProjectedSpend = proj.ProjectedSpend.ToString("C0");
            AllowedDaily = proj.AllowedDaily.ToString("C0");
            BurnDaily = proj.BurnRateDaily.ToString("C0");
            DaysRemainingLabel = proj.IsCurrentMonth
                ? $"Día {proj.DaysElapsed} de {proj.DaysInMonth} · Quedan {proj.DaysRemaining}"
                : string.Empty;
            ProjectionColor = proj.ProjectedSpend <= snap.IngresoDisponible
                ? Color.FromArgb("#10B981")
                : Color.FromArgb("#EF4444");

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

    [RelayCommand]
    private async Task AddIncomeAsync()
        => await Shell.Current.GoToAsync(nameof(IncomeEditPage));
}

public record PillarRow(string Name, string Budgeted, string Spent, string Meta, string Pct, string Status);
public record CategoryRow(string Name, string Pillar, string Budgeted, string Spent, string Available, string Status);
