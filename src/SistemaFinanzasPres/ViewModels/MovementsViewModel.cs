using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class MovementsViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly MonthService _month;

    public MovementsViewModel(AppDbContext db, MonthService month)
    {
        _db = db;
        _month = month;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "Movimientos";
        SelectedFilter = "Todos";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string netoLabel = "$0";
    [ObservableProperty] private string ingresosLabel = "$0";
    [ObservableProperty] private string gastosLabel = "$0";
    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string selectedFilter = "Todos";
    [ObservableProperty] private Color netoColor = Color.FromArgb("#10B981");

    public List<string> FilterOptions { get; } = new() { "Todos", "Ingresos", "Gastos" };
    public ObservableCollection<MovementRow> Items { get; } = new();

    partial void OnSearchTextChanged(string? value) => _ = LoadAsync();
    partial void OnSelectedFilterChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;
            var start = _month.Start;
            var end = _month.End;
            var s = SearchText?.Trim() ?? string.Empty;
            var hasSearch = !string.IsNullOrWhiteSpace(s);

            var rows = new List<MovementRow>();
            decimal sumIngresos = 0m, sumGastos = 0m;

            if (SelectedFilter != "Gastos")
            {
                var incomes = await _db.Incomes.AsNoTracking()
                    .Where(i => i.Date >= start && i.Date < end)
                    .ToListAsync();
                if (hasSearch)
                    incomes = incomes.Where(i =>
                        i.Concept.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        (i.Notes ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        i.Source.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var i in incomes)
                {
                    sumIngresos += i.Amount;
                    rows.Add(new MovementRow(
                        i.Id, "Ingreso", i.Date, i.Concept,
                        i.Source, null, i.Amount,
                        "+" + i.Amount.ToString("C0"),
                        Color.FromArgb("#10B981"),
                        "💵"));
                }
            }

            if (SelectedFilter != "Ingresos")
            {
                var txs = await _db.Transactions.AsNoTracking()
                    .Include(t => t.Category)
                    .Include(t => t.Account)
                    .Where(t => t.Date >= start && t.Date < end)
                    .ToListAsync();
                if (hasSearch)
                    txs = txs.Where(t =>
                        t.Concept.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        (t.Notes ?? "").Contains(s, StringComparison.OrdinalIgnoreCase) ||
                        (t.Category?.Name ?? "").Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var t in txs)
                {
                    sumGastos += t.Amount;
                    rows.Add(new MovementRow(
                        t.Id, "Gasto", t.Date, t.Concept,
                        t.Category?.Name ?? "—",
                        t.Account?.Name,
                        t.Amount,
                        "-" + t.Amount.ToString("C0"),
                        Color.FromArgb("#EF4444"),
                        t.Category?.Pillar.Short() ?? "💸"));
                }
            }

            Items.Clear();
            foreach (var r in rows.OrderByDescending(r => r.Date).ThenByDescending(r => r.Id))
                Items.Add(r);

            IngresosLabel = "+" + sumIngresos.ToString("C0");
            GastosLabel = "-" + sumGastos.ToString("C0");
            var neto = sumIngresos - sumGastos;
            NetoLabel = (neto >= 0 ? "+" : "") + neto.ToString("C0");
            NetoColor = neto >= 0 ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddIncomeAsync()
        => await Shell.Current.GoToAsync(nameof(IncomeEditPage));

    [RelayCommand]
    private async Task AddExpenseAsync()
        => await Shell.Current.GoToAsync(nameof(TransactionEditPage));

    [RelayCommand]
    private async Task EditAsync(MovementRow? row)
    {
        if (row is null) return;
        var route = row.Type == "Ingreso" ? nameof(IncomeEditPage) : nameof(TransactionEditPage);
        await Shell.Current.GoToAsync($"{route}?id={row.Id}");
    }

    [RelayCommand]
    private async Task DeleteAsync(MovementRow? row)
    {
        if (row is null) return;
        var ok = await Shell.Current.DisplayAlert(
            "Eliminar",
            $"¿Eliminar {row.Type.ToLower()} '{row.Concept}'?",
            "Sí", "No");
        if (!ok) return;

        if (row.Type == "Ingreso")
        {
            var i = await _db.Incomes.FindAsync(row.Id);
            if (i != null) { _db.Incomes.Remove(i); await _db.SaveChangesAsync(); }
        }
        else
        {
            var t = await _db.Transactions.FindAsync(row.Id);
            if (t != null) { _db.Transactions.Remove(t); await _db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);
}

public record MovementRow(
    int Id,
    string Type,
    DateTime Date,
    string Concept,
    string CategoryOrSource,
    string? Account,
    decimal Amount,
    string AmountLabel,
    Color AmountColor,
    string TypeIcon);
