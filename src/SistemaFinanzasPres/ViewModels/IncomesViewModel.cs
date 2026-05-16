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
    [ObservableProperty] private bool isEmpty;

    public ObservableCollection<Income> Items { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;
            var list = await _incomes.GetForMonthAsync(_month.Year, _month.Month);
            Items.Clear();
            foreach (var i in list)
                Items.Add(i);
            TotalLabel = list.Sum(i => i.Amount).ToString("C0");
            IsEmpty = list.Count == 0;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
        => await Shell.Current.GoToAsync(nameof(IncomeEditPage));

    [RelayCommand]
    private async Task EditAsync(Income? income)
    {
        if (income is null) return;
        await Shell.Current.GoToAsync($"{nameof(IncomeEditPage)}?id={income.Id}");
    }

    [RelayCommand]
    private async Task DeleteAsync(Income? income)
    {
        if (income is null) return;
        var ok = await Shell.Current.DisplayAlert("Eliminar", $"¿Eliminar '{income.Concept}'?", "Sí", "No");
        if (!ok) return;
        await _incomes.DeleteAsync(income.Id);
        await LoadAsync();
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);
}
