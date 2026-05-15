using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;

namespace SistemaFinanzasPres.ViewModels;

public partial class BudgetViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly BudgetService _budgetSvc;
    private readonly MonthService _month;
    private decimal _ingresoBase;

    public BudgetViewModel(AppDbContext db, BudgetService budgetSvc, MonthService month)
    {
        _db = db;
        _budgetSvc = budgetSvc;
        _month = month;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "Presupuesto";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string totalPresupuestado = "$0";
    [ObservableProperty] private string ingresoLabel = "$0";
    [ObservableProperty] private string diferenciaLabel = "$0 disponible";
    [ObservableProperty] private bool isOverBudget;
    [ObservableProperty] private Color diferenciaColor = Color.FromArgb("#10B981");
    [ObservableProperty] private Color summaryBgColor = Color.FromArgb("#D1FAE5");

    public ObservableCollection<BudgetRow> Rows { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;

            var snap = await _budgetSvc.GetSnapshotAsync(_month.Year, _month.Month);
            _ingresoBase = snap.IngresoTotal;
            IngresoLabel = snap.IngresoTotal.ToString("C0");

            foreach (var row in Rows)
                row.PropertyChanged -= OnRowChanged;

            Rows.Clear();

            foreach (var c in snap.Categories)
            {
                var row = new BudgetRow
                {
                    CategoryId = c.CategoryId,
                    Name = c.CategoryName,
                    Pillar = c.Pillar.Short(),
                    Budgeted = c.Budgeted,
                    BudgetedText = c.Budgeted == 0 ? string.Empty : c.Budgeted.ToString("N0"),
                    Spent = c.Spent.ToString("C0"),
                    Available = c.Available.ToString("C0"),
                    Pct = c.Percent.ToString("P0"),
                    Status = c.Status,
                };
                row.PropertyChanged += OnRowChanged;
                Rows.Add(row);
            }

            RecalcTotals();
        }
        finally { IsBusy = false; }
    }

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BudgetRow.BudgetedText))
            RecalcTotals();
    }

    private void RecalcTotals()
    {
        var total = Rows.Sum(r => decimal.TryParse(r.BudgetedText, out var v) ? v : 0m);
        TotalPresupuestado = total.ToString("C0");
        var diff = _ingresoBase - total;
        IsOverBudget = diff < 0;
        DiferenciaLabel = diff >= 0 ? $"+{diff:C0} sin asignar" : $"{diff:C0} excedido";
        DiferenciaColor = diff < 0 ? Color.FromArgb("#EF4444") : Color.FromArgb("#10B981");
        SummaryBgColor = diff < 0 ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#D1FAE5");
    }

    [RelayCommand]
    private async Task SaveRowAsync(BudgetRow? row)
    {
        if (row is null) return;
        if (!decimal.TryParse(row.BudgetedText, out var amt)) return;
        await _budgetSvc.UpsertBudgetAsync(row.CategoryId, _month.Year, _month.Month, amt);
        row.Budgeted = amt;
        row.MarkSaved();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        foreach (var r in Rows)
        {
            if (decimal.TryParse(r.BudgetedText, out var amt))
                await _budgetSvc.UpsertBudgetAsync(r.CategoryId, _month.Year, _month.Month, amt);
        }
        await LoadAsync();
        await Shell.Current.DisplayAlert("Presupuesto", "Cambios guardados.", "OK");
    }

    [RelayCommand]
    private async Task CopyPrevAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Copiar mes anterior",
            "¿Importar los montos del mes anterior para las categorías sin presupuesto?",
            "Sí", "No");
        if (!confirm) return;
        await _budgetSvc.CopyBudgetFromPreviousMonthAsync(_month.Year, _month.Month);
        await LoadAsync();
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);
}

public partial class BudgetRow : ObservableObject
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Pillar { get; set; } = string.Empty;
    public decimal Budgeted { get; set; }
    [ObservableProperty] private string budgetedText = string.Empty;
    [ObservableProperty] private bool isJustSaved;
    public string Spent { get; set; } = string.Empty;
    public string Available { get; set; } = string.Empty;
    public string Pct { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public async void MarkSaved()
    {
        IsJustSaved = true;
        await Task.Delay(1800);
        IsJustSaved = false;
    }
}
