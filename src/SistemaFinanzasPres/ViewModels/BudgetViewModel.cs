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

    public BudgetViewModel(AppDbContext db, BudgetService budgetSvc, MonthService month)
    {
        _db = db;
        _budgetSvc = budgetSvc;
        _month = month;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "Presupuesto";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
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
            Rows.Clear();
            foreach (var c in snap.Categories)
            {
                Rows.Add(new BudgetRow
                {
                    CategoryId = c.CategoryId,
                    Name = c.CategoryName,
                    Pillar = c.Pillar.Short(),
                    Budgeted = c.Budgeted,
                    BudgetedText = c.Budgeted.ToString("N0"),
                    Spent = c.Spent.ToString("C0"),
                    Available = c.Available.ToString("C0"),
                    Pct = c.Percent.ToString("P1"),
                    Status = c.Status,
                });
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        foreach (var r in Rows)
        {
            if (decimal.TryParse(r.BudgetedText, out var amt))
            {
                await _budgetSvc.UpsertBudgetAsync(r.CategoryId, _month.Year, _month.Month, amt);
            }
        }
        await LoadAsync();
        await Shell.Current.DisplayAlert("Presupuesto", "Cambios guardados.", "OK");
    }

    [RelayCommand]
    private async Task CopyPrevAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Copiar mes anterior",
            "¿Importar los montos del mes anterior para las categorías sin presupuesto este mes?",
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
    [ObservableProperty] private string budgetedText = "0";
    public string Spent { get; set; } = string.Empty;
    public string Available { get; set; } = string.Empty;
    public string Pct { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
