using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Services;

namespace SistemaFinanzasPres.ViewModels;

public partial class KpisViewModel : BaseViewModel
{
    private readonly KpiService _kpi;
    private readonly MonthService _month;

    public KpisViewModel(KpiService kpi, MonthService month)
    {
        _kpi = kpi;
        _month = month;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "KPIs";
    }

    public ObservableCollection<KpiRow> Rows { get; } = new();
    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string gastosFijos = "$0";
    [ObservableProperty] private string metaFondo = "$0";
    [ObservableProperty] private string acumuladoFondo = "$0";
    [ObservableProperty] private string avanceFondo = "0%";
    [ObservableProperty] private string statusFondo = "—";
    [ObservableProperty] private int mesesFondo;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;
            var (rows, fondo) = await _kpi.GetKpisAsync(_month.Year, _month.Month);
            Rows.Clear();
            foreach (var r in rows) Rows.Add(r);

            GastosFijos = fondo.GastosFijos.ToString("C0");
            MesesFondo = fondo.Meses;
            MetaFondo = fondo.Meta.ToString("C0");
            AcumuladoFondo = fondo.Acumulado.ToString("C0");
            AvanceFondo = fondo.Avance.ToString("P1");
            StatusFondo = fondo.Status;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);
}
