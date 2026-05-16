using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;

namespace SistemaFinanzasPres.ViewModels;

[QueryProperty(nameof(IncomeId), "id")]
public partial class IncomeEditViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly IncomeService _incomes;

    public IncomeEditViewModel(AppDbContext db, IncomeService incomes)
    {
        _db = db;
        _incomes = incomes;
        Title = "Nuevo ingreso";
    }

    [ObservableProperty] private int incomeId;
    [ObservableProperty] private bool isExisting;
    [ObservableProperty] private DateTime date = DateTime.Today;
    [ObservableProperty] private string concept = string.Empty;
    [ObservableProperty] private string amountText = "0";
    [ObservableProperty] private string? source = "Salario";
    [ObservableProperty] private string? notes;
    [ObservableProperty] private bool isRecurring;

    public List<string> SourceOptions { get; private set; } = new();

    partial void OnIncomeIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        SourceOptions = await _incomes.GetSourceSuggestionsAsync();
        OnPropertyChanged(nameof(SourceOptions));

        if (IncomeId > 0)
        {
            var i = await _db.Incomes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == IncomeId);
            if (i != null)
            {
                Date = i.Date;
                Concept = i.Concept;
                AmountText = i.Amount.ToString("0.##");
                Source = i.Source;
                Notes = i.Notes;
                IsRecurring = i.IsRecurring;
                Title = "Editar ingreso";
                IsExisting = true;
            }
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Concept))
        {
            await Shell.Current.DisplayAlert("Falta", "Escribe un concepto.", "OK");
            return;
        }
        if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlert("Falta", "Monto inválido.", "OK");
            return;
        }

        if (IncomeId > 0)
        {
            var existing = await _db.Incomes.FindAsync(IncomeId);
            if (existing == null) return;
            existing.Date = Date;
            existing.Concept = Concept.Trim();
            existing.Amount = amount;
            existing.Source = string.IsNullOrWhiteSpace(Source) ? "Otro" : Source.Trim();
            existing.Notes = Notes;
            existing.IsRecurring = IsRecurring;
            await _incomes.UpdateAsync(existing);
        }
        else
        {
            await _incomes.AddAsync(new Income
            {
                Date = Date,
                Concept = Concept.Trim(),
                Amount = amount,
                Source = string.IsNullOrWhiteSpace(Source) ? "Otro" : Source!.Trim(),
                Notes = Notes,
                IsRecurring = IsRecurring,
            });
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IncomeId <= 0) return;
        var ok = await Shell.Current.DisplayAlert("Eliminar", "¿Eliminar este ingreso?", "Sí", "No");
        if (!ok) return;
        await _incomes.DeleteAsync(IncomeId);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}
