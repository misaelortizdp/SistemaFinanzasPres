using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class ConfigViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    public ConfigViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Configuración";
    }

    [ObservableProperty] private string salarioText = "0";
    [ObservableProperty] private string otrosText = "0";
    [ObservableProperty] private string ingresoTotal = "$0";
    [ObservableProperty] private string ingresoDisponible = "$0";

    [ObservableProperty] private string metaNecText = "50";
    [ObservableProperty] private string metaDesText = "30";
    [ObservableProperty] private string metaAhoText = "20";

    [ObservableProperty] private int fondoMeses = 4;
    [ObservableProperty] private string ahorroMinPctText = "20";
    [ObservableProperty] private string ahorroOptPctText = "30";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var cfg = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync() ?? new AppConfig();
        SalarioText = cfg.SalarioNeto.ToString("0.##");
        OtrosText = cfg.OtrosIngresos.ToString("0.##");
        MetaNecText = (cfg.MetaNecesidadesPct * 100m).ToString("0.##");
        MetaDesText = (cfg.MetaDeseosPct * 100m).ToString("0.##");
        MetaAhoText = (cfg.MetaAhorroPct * 100m).ToString("0.##");
        FondoMeses = cfg.FondoEmergenciaMeses;
        AhorroMinPctText = (cfg.MetaAhorroMinimoPct * 100m).ToString("0.##");
        AhorroOptPctText = (cfg.MetaAhorroOptimoPct * 100m).ToString("0.##");
        RecalcLabels();
    }

    partial void OnSalarioTextChanged(string value) => RecalcLabels();
    partial void OnOtrosTextChanged(string value) => RecalcLabels();

    private async void RecalcLabels()
    {
        decimal.TryParse(SalarioText, out var sal);
        decimal.TryParse(OtrosText, out var otr);
        IngresoTotal = (sal + otr).ToString("C0");

        var diezmoBudget = 0m;
        try
        {
            var cfg = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync();
            if (cfg != null && cfg.CategoriaDiezmoId > 0)
            {
                var today = DateTime.Today;
                diezmoBudget = await _db.Budgets.AsNoTracking()
                    .Where(b => b.CategoryId == cfg.CategoriaDiezmoId && b.Year == today.Year && b.Month == today.Month)
                    .Select(b => b.Amount)
                    .FirstOrDefaultAsync();
            }
        }
        catch { }
        IngresoDisponible = (sal + otr - diezmoBudget).ToString("C0");
    }

    [RelayCommand]
    private async Task GoToCategoriesAsync()
        => await Shell.Current.GoToAsync(nameof(CategoriesPage));

    [RelayCommand]
    private async Task GoToAccountsAsync()
        => await Shell.Current.GoToAsync(nameof(AccountsPage));

    [RelayCommand]
    private async Task SaveAsync()
    {
        var cfg = await _db.AppConfigs.FirstOrDefaultAsync();
        if (cfg == null)
        {
            cfg = new AppConfig { Id = 1 };
            _db.AppConfigs.Add(cfg);
        }
        if (decimal.TryParse(SalarioText, out var sal)) cfg.SalarioNeto = sal;
        if (decimal.TryParse(OtrosText, out var otr)) cfg.OtrosIngresos = otr;
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
