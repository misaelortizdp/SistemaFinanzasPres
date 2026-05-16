using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class ConfigViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly IncomeService _incomes;
    private readonly MonthService _month;

    public ConfigViewModel(AppDbContext db, IncomeService incomes, MonthService month)
    {
        _db = db;
        _incomes = incomes;
        _month = month;
        Title = "Configuración";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string ingresoTotalLabel = "$0";
    [ObservableProperty] private string diezmoPctText = "10";
    [ObservableProperty] private string diezmoAmountLabel = "$0";
    [ObservableProperty] private string ingresoDisponibleLabel = "$0";
    [ObservableProperty] private bool sinIngresos;

    public ObservableCollection<IncomeRow> Ingresos { get; } = new();

    [ObservableProperty] private string metaNecText = "50";
    [ObservableProperty] private string metaDesText = "30";
    [ObservableProperty] private string metaAhoText = "20";

    [ObservableProperty] private string metaNecAmount = "$0";
    [ObservableProperty] private string metaDesAmount = "$0";
    [ObservableProperty] private string metaAhoAmount = "$0";
    [ObservableProperty] private string sumaMetasLabel = "Suma: 100%";
    [ObservableProperty] private Color sumaMetasColor = Color.FromArgb("#10B981");

    [ObservableProperty] private int fondoMeses = 4;
    [ObservableProperty] private string ahorroMinPctText = "20";
    [ObservableProperty] private string ahorroOptPctText = "30";

    private decimal _ingresoTotal;
    private decimal _diezmoPctValue = 0.10m;

    [RelayCommand]
    public async Task LoadAsync()
    {
        MonthLabel = _month.Label;

        var cfg = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync() ?? new AppConfig();
        _diezmoPctValue = cfg.DiezmoPct;
        DiezmoPctText = (cfg.DiezmoPct * 100m).ToString("0.##");
        MetaNecText = (cfg.MetaNecesidadesPct * 100m).ToString("0.##");
        MetaDesText = (cfg.MetaDeseosPct * 100m).ToString("0.##");
        MetaAhoText = (cfg.MetaAhorroPct * 100m).ToString("0.##");
        FondoMeses = cfg.FondoEmergenciaMeses;
        AhorroMinPctText = (cfg.MetaAhorroMinimoPct * 100m).ToString("0.##");
        AhorroOptPctText = (cfg.MetaAhorroOptimoPct * 100m).ToString("0.##");

        var list = await _incomes.GetForMonthAsync(_month.Year, _month.Month);
        Ingresos.Clear();
        foreach (var i in list)
            Ingresos.Add(new IncomeRow(i.Id, i.Date, i.Concept, i.Source, i.Amount, i.Amount.ToString("C0")));
        _ingresoTotal = list.Sum(i => i.Amount);
        SinIngresos = list.Count == 0;
        IngresoTotalLabel = _ingresoTotal.ToString("C0");

        RecalcLabels();
    }

    partial void OnDiezmoPctTextChanged(string value) => RecalcLabels();
    partial void OnMetaNecTextChanged(string value) => RecalcLabels();
    partial void OnMetaDesTextChanged(string value) => RecalcLabels();
    partial void OnMetaAhoTextChanged(string value) => RecalcLabels();

    private void RecalcLabels()
    {
        if (decimal.TryParse(DiezmoPctText, out var dp)) _diezmoPctValue = dp / 100m;
        var diezmo = _ingresoTotal * _diezmoPctValue;
        var disp = _ingresoTotal - diezmo;
        DiezmoAmountLabel = diezmo.ToString("C0");
        IngresoDisponibleLabel = disp.ToString("C0");

        decimal.TryParse(MetaNecText, out var pnec);
        decimal.TryParse(MetaDesText, out var pdes);
        decimal.TryParse(MetaAhoText, out var paho);
        MetaNecAmount = (disp * pnec / 100m).ToString("C0");
        MetaDesAmount = (disp * pdes / 100m).ToString("C0");
        MetaAhoAmount = (disp * paho / 100m).ToString("C0");
        var suma = pnec + pdes + paho;
        SumaMetasLabel = $"Suma: {suma}% {(suma == 100m ? "✅" : suma < 100m ? "— faltan " + (100 - suma) + "%" : "⚠️ excede 100%")}";
        SumaMetasColor = suma == 100m ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
    }

    [RelayCommand]
    private async Task GoToIncomesAsync()
        => await Shell.Current.GoToAsync(nameof(IncomesPage));

    [RelayCommand]
    private async Task AddIncomeAsync()
        => await Shell.Current.GoToAsync(nameof(IncomeEditPage));

    [RelayCommand]
    private async Task EditIncomeAsync(IncomeRow? row)
    {
        if (row is null) return;
        await Shell.Current.GoToAsync($"{nameof(IncomeEditPage)}?id={row.Id}");
    }

    [RelayCommand]
    private async Task GoToCategoriesAsync()
        => await Shell.Current.GoToAsync(nameof(CategoriesPage));

    [RelayCommand]
    private async Task GoToAccountsAsync()
        => await Shell.Current.GoToAsync(nameof(AccountsPage));

    [RelayCommand]
    private async Task GoToDebtsAsync()
        => await Shell.Current.GoToAsync(nameof(DebtsPage));

    [RelayCommand] private void PrevMonth() { _month.Shift(-1); _ = LoadAsync(); }
    [RelayCommand] private void NextMonth() { _month.Shift(1); _ = LoadAsync(); }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var cfg = await _db.AppConfigs.FirstOrDefaultAsync();
        if (cfg == null)
        {
            cfg = new AppConfig { Id = 1 };
            _db.AppConfigs.Add(cfg);
        }
        if (decimal.TryParse(DiezmoPctText, out var dp)) cfg.DiezmoPct = dp / 100m;
        if (decimal.TryParse(MetaNecText, out var mn)) cfg.MetaNecesidadesPct = mn / 100m;
        if (decimal.TryParse(MetaDesText, out var md)) cfg.MetaDeseosPct = md / 100m;
        if (decimal.TryParse(MetaAhoText, out var ma)) cfg.MetaAhorroPct = ma / 100m;
        cfg.FondoEmergenciaMeses = FondoMeses;
        if (decimal.TryParse(AhorroMinPctText, out var amin)) cfg.MetaAhorroMinimoPct = amin / 100m;
        if (decimal.TryParse(AhorroOptPctText, out var aopt)) cfg.MetaAhorroOptimoPct = aopt / 100m;

        await _db.SaveChangesAsync();
        await Shell.Current.DisplayAlert("Configuración", "Cambios guardados.", "OK");
        RecalcLabels();
    }
}

public record IncomeRow(int Id, DateTime Date, string Concept, string Source, decimal Amount, string AmountLabel);
