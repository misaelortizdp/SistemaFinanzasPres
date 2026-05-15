using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class TransactionsViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly MonthService _month;

    public TransactionsViewModel(AppDbContext db, MonthService month)
    {
        _db = db;
        _month = month;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "Gastos";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string totalLabel = "$0";
    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string? selectedPillar;
    [ObservableProperty] private Category? selectedCategory;

    public ObservableCollection<TxRow> Items { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public List<string> PillarOptions { get; } = new() { "Todos", "Necesidad", "Deseo", "Ahorro", "Primer Fruto" };

    partial void OnSearchTextChanged(string? value) => _ = LoadAsync();
    partial void OnSelectedPillarChanged(string? value) => _ = LoadAsync();
    partial void OnSelectedCategoryChanged(Category? value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;

            if (Categories.Count == 0)
            {
                var cats = await _db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync();
                Categories.Clear();
                foreach (var c in cats) Categories.Add(c);
            }

            var query = _db.Transactions.AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.Account)
                .Where(t => t.Date >= _month.Start && t.Date < _month.End);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.Trim();
                query = query.Where(t => t.Concept.Contains(s) || (t.Notes ?? "").Contains(s));
            }

            if (!string.IsNullOrEmpty(SelectedPillar) && SelectedPillar != "Todos"
                && Enum.TryParse<Pillar>(SelectedPillar, out var p))
            {
                query = query.Where(t => t.Category!.Pillar == p);
            }

            if (SelectedCategory is not null)
            {
                var catId = SelectedCategory.Id;
                query = query.Where(t => t.CategoryId == catId);
            }

            var data = await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync();

            Items.Clear();
            foreach (var t in data)
            {
                Items.Add(new TxRow(
                    t.Id, t.Date, t.Concept,
                    t.Category?.Name ?? "—",
                    t.Category?.Pillar.Short() ?? "",
                    t.Account?.Name ?? "",
                    t.Amount,
                    t.Amount.ToString("C0")));
            }

            TotalLabel = data.Sum(x => x.Amount).ToString("C0");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
        => await Shell.Current.GoToAsync(nameof(TransactionEditPage));

    [RelayCommand]
    private async Task EditAsync(TxRow? row)
    {
        if (row is null) return;
        await Shell.Current.GoToAsync($"{nameof(TransactionEditPage)}?id={row.Id}");
    }

    [RelayCommand]
    private async Task DeleteAsync(TxRow? row)
    {
        if (row is null) return;
        var ok = await Shell.Current.DisplayAlert("Eliminar", $"¿Eliminar '{row.Concept}'?", "Sí", "No");
        if (!ok) return;
        var entity = await _db.Transactions.FindAsync(row.Id);
        if (entity != null)
        {
            _db.Transactions.Remove(entity);
            await _db.SaveChangesAsync();
            await LoadAsync();
        }
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);
}

public record TxRow(int Id, DateTime Date, string Concept, string Category, string PillarShort, string Account, decimal Amount, string AmountLabel);
