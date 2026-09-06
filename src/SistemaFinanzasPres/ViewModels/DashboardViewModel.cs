using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly BudgetService _budget;
    private readonly MonthService _month;
    private readonly KpiService _kpi;
    private readonly DebtService _debts;

    public DashboardViewModel(BudgetService budget, MonthService month, KpiService kpi, DebtService debts)
    {
        _budget = budget;
        _month = month;
        _kpi = kpi;
        _debts = debts;
        _month.Changed += async (_, __) => await LoadAsync();
        Title = "Dashboard";
    }

    [ObservableProperty] private string monthLabel = string.Empty;
    [ObservableProperty] private string ingreso = "$0";
    [ObservableProperty] private string ingresoSource = "Fijo";
    [ObservableProperty] private string totalGastado = "$0";
    [ObservableProperty] private string disponible = "$0";
    [ObservableProperty] private string pctEjecutado = "0%";
    [ObservableProperty] private string tasaAhorro = "0%";

    [ObservableProperty] private bool showProjection;
    [ObservableProperty] private string projectionHeadline = string.Empty;
    [ObservableProperty] private string projectedSpend = "$0";
    [ObservableProperty] private string allowedDaily = "$0";
    [ObservableProperty] private string burnDaily = "$0";
    [ObservableProperty] private string daysRemainingLabel = string.Empty;
    [ObservableProperty] private Color projectionColor = Colors.Gray;

    [ObservableProperty] private string metaFondo = "$0";
    [ObservableProperty] private string acumuladoFondo = "$0";
    [ObservableProperty] private string avanceFondo = "0%";
    [ObservableProperty] private double avanceFondoProgress;
    [ObservableProperty] private string statusFondo = "—";
    [ObservableProperty] private int mesesFondo;

    [ObservableProperty] private bool showDeudas;
    [ObservableProperty] private string totalDeudas = "$0";
    [ObservableProperty] private int deudasActivas;

    [ObservableProperty] private string diasConRegistroLabel = string.Empty;

    [ObservableProperty] private string mejorMes = "—";
    [ObservableProperty] private string peorMes = "—";
    [ObservableProperty] private string promedioAhorro = "0%";
    [ObservableProperty] private string promedioGasto = "$0";

    [ObservableProperty] private bool hasSugerencias;

    public ObservableCollection<PillarRow> Pillars { get; } = new();
    public ObservableCollection<CategoryRow> Categories { get; } = new();
    public ObservableCollection<string> Sugerencias { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            MonthLabel = _month.Label;
            var snap = await _budget.GetSnapshotAsync(_month.Year, _month.Month);

            Ingreso = snap.IngresoDisponible.ToString("C0");
            IngresoSource = snap.IngresoTotal > 0
                ? $"Total {snap.IngresoTotal:C0} − Diezmo {snap.DiezmoMonto:C0}"
                : "Sin ingresos registrados";
            TotalGastado = snap.TotalSpent.ToString("C0");
            Disponible = (snap.IngresoDisponible - snap.TotalSpent).ToString("C0");
            PctEjecutado = (snap.IngresoDisponible > 0 ? snap.TotalSpent / snap.IngresoDisponible : 0m).ToString("P1");
            var ahorro = snap.Pillars.FirstOrDefault(p => p.Pillar == Models.Pillar.Ahorro);
            TasaAhorro = (snap.IngresoDisponible > 0 && ahorro != null ? ahorro.Spent / snap.IngresoDisponible : 0m).ToString("P1");

            var proj = snap.Projection;
            ShowProjection = proj.IsCurrentMonth && snap.IngresoDisponible > 0;
            ProjectionHeadline = proj.Headline;
            ProjectedSpend = proj.ProjectedSpend.ToString("C0");
            AllowedDaily = proj.AllowedDaily.ToString("C0");
            BurnDaily = proj.BurnRateDaily.ToString("C0");
            DaysRemainingLabel = proj.IsCurrentMonth
                ? $"Día {proj.DaysElapsed} de {proj.DaysInMonth} · Quedan {proj.DaysRemaining}"
                : string.Empty;
            ProjectionColor = proj.ProjectedSpend <= snap.IngresoDisponible
                ? Color.FromArgb("#10B981")
                : Color.FromArgb("#EF4444");

            Pillars.Clear();
            foreach (var p in snap.Pillars)
            {
                Pillars.Add(new PillarRow(
                    p.Pillar.Display(),
                    p.Budgeted.ToString("C0"),
                    p.Spent.ToString("C0"),
                    p.MetaAmount.ToString("C0"),
                    p.PercentOfIncomeBase.ToString("P1"),
                    p.Status));
            }

            Categories.Clear();
            foreach (var c in snap.Categories.OrderBy(c => c.Pillar).ThenBy(c => c.CategoryName))
            {
                var progress = (double)Math.Min(1m, c.Budgeted > 0 ? c.Spent / c.Budgeted : 0m);
                Categories.Add(new CategoryRow(
                    c.CategoryName, c.Pillar.Short(),
                    c.Budgeted.ToString("C0"),
                    c.Spent.ToString("C0"),
                    c.Available.ToString("C0"),
                    c.Status,
                    progress,
                    c.Percent.ToString("P0")));
            }

            var fondo = await _kpi.GetFondoEmergenciaAsync(_month.Year, _month.Month);
            MetaFondo = fondo.Meta.ToString("C0");
            MesesFondo = fondo.Meses;
            AcumuladoFondo = fondo.Acumulado.ToString("C0");
            AvanceFondo = fondo.Avance.ToString("P1");
            AvanceFondoProgress = (double)Math.Min(1m, fondo.Avance);
            StatusFondo = fondo.Status;

            var (dias, diasDelMes) = await _kpi.GetDiasConRegistroAsync(_month.Year, _month.Month);
            DiasConRegistroLabel = $"{dias} de {diasDelMes} días";

            var overview = await _debts.GetOverviewAsync();
            ShowDeudas = overview.ActiveCount > 0;
            TotalDeudas = overview.TotalBalance.ToString("C0");
            DeudasActivas = overview.ActiveCount;

            var trend = await _kpi.GetTrendSummaryAsync();
            MejorMes = trend.MejorMes;
            PeorMes = trend.PeorMes;
            PromedioAhorro = trend.PromedioAhorro;
            PromedioGasto = trend.PromedioGasto;

            var sugerencias = await _kpi.GetSugerenciasAsync(_month.Year, _month.Month);
            Sugerencias.Clear();
            foreach (var s in sugerencias) Sugerencias.Add(s);
            HasSugerencias = sugerencias.Count > 0;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void PrevMonth() => _month.Shift(-1);
    [RelayCommand] private void NextMonth() => _month.Shift(1);
}

public record PillarRow(string Name, string Budgeted, string Spent, string Meta, string Pct, string Status);
public record CategoryRow(string Name, string Pillar, string Budgeted, string Spent, string Available, string Status, double Progress, string ProgressLabel);
