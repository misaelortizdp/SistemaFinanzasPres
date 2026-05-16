using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres.ViewModels;

public record GoalRow(
    int Id,
    string Name,
    string Priority,
    decimal Target,
    decimal Achieved,
    decimal Remaining,
    string TargetLabel,
    string AchievedLabel,
    string RemainingLabel,
    double Progress,
    string ProgressLabel,
    string DeadlineLabel,
    string RecommendedLabel,
    string PaceLabel,
    string Status,
    Color StatusColor,
    string? Notes,
    bool HasNotes);

public partial class SavingsGoalsViewModel : BaseViewModel
{
    private readonly SavingsGoalService _svc;

    public SavingsGoalsViewModel(SavingsGoalService svc)
    {
        _svc = svc;
        Title = "Metas de ahorro";
    }

    public ObservableCollection<GoalRow> Goals { get; } = new();

    [ObservableProperty] private string totalTargetLabel = "$0";
    [ObservableProperty] private string totalAchievedLabel = "$0";
    [ObservableProperty] private string overallProgressLabel = "0%";
    [ObservableProperty] private double overallProgress;
    [ObservableProperty] private bool isEmpty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var goals = await _svc.GetActiveAsync();
            var today = DateTime.Today;
            var rows = new List<GoalRow>();

            foreach (var g in goals)
            {
                var remaining = Math.Max(0m, g.Target - g.Achieved);
                var progress = g.Target > 0 ? (double)Math.Min(1m, g.Achieved / g.Target) : 0d;

                string deadlineLabel = "Sin fecha límite";
                string recommendedLabel = "—";
                string paceLabel = "—";
                string status;
                Color statusColor;

                if (g.Achieved >= g.Target && g.Target > 0)
                {
                    status = "✅ Alcanzada";
                    statusColor = Color.FromArgb("#10B981");
                }
                else if (g.Deadline.HasValue)
                {
                    var deadline = g.Deadline.Value.Date;
                    deadlineLabel = $"Fecha: {deadline:dd MMM yyyy}";
                    var totalMonths = Math.Max(0, ((deadline.Year - today.Year) * 12) + (deadline.Month - today.Month));
                    if (deadline < today)
                    {
                        status = "⏰ Vencida";
                        statusColor = Color.FromArgb("#EF4444");
                        recommendedLabel = $"Pendiente: {remaining:C0}";
                    }
                    else if (totalMonths == 0)
                    {
                        recommendedLabel = $"Necesitas {remaining:C0} este mes";
                        status = remaining == 0 ? "✅ Lista" : "🔴 Mes final";
                        statusColor = remaining == 0 ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
                    }
                    else
                    {
                        var monthly = remaining / totalMonths;
                        recommendedLabel = $"Necesitas {monthly:C0}/mes ({totalMonths} meses)";
                        if (g.MonthlyPlanned > 0)
                        {
                            status = g.MonthlyPlanned >= monthly ? "🟢 En camino" : "🟡 Atrasada";
                            statusColor = g.MonthlyPlanned >= monthly ? Color.FromArgb("#10B981") : Color.FromArgb("#F59E0B");
                        }
                        else
                        {
                            status = "🟡 Sin plan mensual";
                            statusColor = Color.FromArgb("#F59E0B");
                        }
                    }
                }
                else
                {
                    status = remaining > 0 ? "🟡 En progreso" : "✅ Lista";
                    statusColor = remaining > 0 ? Color.FromArgb("#F59E0B") : Color.FromArgb("#10B981");
                }

                if (g.MonthlyPlanned > 0 && remaining > 0)
                {
                    var monthsToReach = (int)Math.Ceiling((double)(remaining / g.MonthlyPlanned));
                    var reachDate = today.AddMonths(monthsToReach);
                    paceLabel = $"Al ritmo de {g.MonthlyPlanned:C0}/mes: ~{monthsToReach} meses ({reachDate:MMM yyyy})";
                }
                else if (g.MonthlyPlanned > 0)
                {
                    paceLabel = $"Plan: {g.MonthlyPlanned:C0}/mes";
                }

                rows.Add(new GoalRow(
                    g.Id, g.Name, g.Priority,
                    g.Target, g.Achieved, remaining,
                    g.Target.ToString("C0"),
                    g.Achieved.ToString("C0"),
                    remaining.ToString("C0"),
                    progress, progress.ToString("P0"),
                    deadlineLabel, recommendedLabel, paceLabel,
                    status, statusColor,
                    g.Notes, !string.IsNullOrWhiteSpace(g.Notes)));
            }

            Goals.Clear();
            foreach (var r in rows) Goals.Add(r);

            IsEmpty = rows.Count == 0;
            var totalTarget = goals.Sum(g => g.Target);
            var totalAchieved = goals.Sum(g => g.Achieved);
            TotalTargetLabel = totalTarget.ToString("C0");
            TotalAchievedLabel = totalAchieved.ToString("C0");
            OverallProgress = totalTarget > 0 ? (double)Math.Min(1m, totalAchieved / totalTarget) : 0d;
            OverallProgressLabel = OverallProgress.ToString("P0");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAsync()
        => await Shell.Current.GoToAsync(nameof(SavingsGoalEditPage));

    [RelayCommand]
    private async Task EditAsync(GoalRow? row)
    {
        if (row is null) return;
        await Shell.Current.GoToAsync($"{nameof(SavingsGoalEditPage)}?id={row.Id}");
    }

    [RelayCommand]
    private async Task DeleteAsync(GoalRow? row)
    {
        if (row is null) return;
        var ok = await Shell.Current.DisplayAlert(
            "Eliminar meta", $"¿Eliminar '{row.Name}'?", "Sí", "No");
        if (!ok) return;
        await _svc.DeleteAsync(row.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ContributeAsync(GoalRow? row)
    {
        if (row is null) return;
        var input = await Shell.Current.DisplayPromptAsync(
            $"Aporte a '{row.Name}'",
            $"Restante: {row.RemainingLabel}\nMonto del aporte:",
            "Registrar", "Cancelar",
            placeholder: "0",
            keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(input)) return;
        if (!decimal.TryParse(input, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlert("Monto inválido", "Ingresa un número mayor a 0.", "OK");
            return;
        }
        await _svc.RegisterContributionAsync(row.Id, amount);
        await LoadAsync();
    }
}
