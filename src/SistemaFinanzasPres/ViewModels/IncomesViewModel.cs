using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class IncomesViewModel : BaseViewModel
{
    private readonly IncomeService _incomes;
    private readonly MonthService _month;

    public IncomesViewModel(IncomeService incomes, MonthService month)
    {
        _incomes = incomes;
        _month = month;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "Ingresos";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string totalLabel = "$0";

    public ObservableCollection<IncomeRow> Items { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;
            var data = await _incomes.GetForMonthAsync(_month.Year, _month.Month);
            Items.Clear();
            foreach (var i in data)
            {
                Items.Add(new IncomeRow(
                    i.Id, i.Date, i.Concept, i.Source,
                    i.Amount, i.Amount.ToString("C0"), i.IsRecurring));
            }
            TotalLabel = data.Sum(x => x.Amount).ToString("C0");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
        => await Shell.Current.GoToAsync(nameof(IncomeEditPage));

    [RelayCommand]
    private async Task EditAsync(IncomeRow? row)
    {
        if (row is null) return;
        await Shell.Current.GoToAsync($"{nameof(IncomeEditPage)}?id={row.Id}");
    }

    [RelayCommand]
    private async Task DeleteAsync(IncomeRow? row)
    {
        if (row is null) return;
        var ok = await Shell.Current.DisplayAlert("Eliminar", $"¿Eliminar ingreso '{row.Concept}'?", "Sí", "No");
        if (!ok) return;
        await _incomes.DeleteAsync(row.Id);
        await LoadAsync();
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);
}

public record IncomeRow(int Id, DateTime Date, string Concept, string Source, decimal Amount, string AmountLabel, bool IsRecurring);
