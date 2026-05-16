using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.Services;

namespace SistemaFinanzasPres.ViewModels;

[QueryProperty(nameof(GoalId), "id")]
public partial class SavingsGoalEditViewModel : BaseViewModel
{
    private readonly SavingsGoalService _svc;

    public SavingsGoalEditViewModel(SavingsGoalService svc)
    {
        _svc = svc;
        Title = "Nueva meta";
    }

    [ObservableProperty] private int goalId;
    [ObservableProperty] private bool isExisting;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string priority = "MEDIA";
    [ObservableProperty] private string targetText = "0";
    [ObservableProperty] private string achievedText = "0";
    [ObservableProperty] private string monthlyPlannedText = "0";
    [ObservableProperty] private bool hasDeadline;
    [ObservableProperty] private DateTime deadline = DateTime.Today.AddMonths(12);
    [ObservableProperty] private string? notes;

    public List<string> PriorityOptions { get; } = new() { "URGENTE", "ALTA", "MEDIA", "BAJA", "FUTURO" };

    partial void OnGoalIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (GoalId <= 0) return;
        var g = await _svc.GetByIdAsync(GoalId);
        if (g == null) return;
        Name = g.Name;
        Priority = string.IsNullOrWhiteSpace(g.Priority) ? "MEDIA" : g.Priority;
        TargetText = g.Target.ToString("0.##");
        AchievedText = g.Achieved.ToString("0.##");
        MonthlyPlannedText = g.MonthlyPlanned.ToString("0.##");
        HasDeadline = g.Deadline.HasValue;
        if (g.Deadline.HasValue) Deadline = g.Deadline.Value;
        Notes = g.Notes;
        Title = "Editar meta";
        IsExisting = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Falta", "Escribe el nombre de la meta.", "OK");
            return;
        }
        if (!decimal.TryParse(TargetText, out var target) || target <= 0)
        {
            await Shell.Current.DisplayAlert("Falta", "Monto objetivo inválido.", "OK");
            return;
        }
        decimal.TryParse(AchievedText, out var achieved);
        decimal.TryParse(MonthlyPlannedText, out var monthlyPlanned);

        if (IsExisting)
        {
            var g = await _svc.GetByIdAsync(GoalId);
            if (g == null) return;
            g.Name = Name.Trim();
            g.Priority = Priority;
            g.Target = target;
            g.Achieved = Math.Max(0, achieved);
            g.MonthlyPlanned = Math.Max(0, monthlyPlanned);
            g.Deadline = HasDeadline ? Deadline : null;
            g.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
            g.IsActive = true;
            await _svc.UpdateAsync(g);
        }
        else
        {
            await _svc.AddAsync(new SavingsGoal
            {
                Name = Name.Trim(),
                Priority = Priority,
                Target = target,
                Achieved = Math.Max(0, achieved),
                MonthlyPlanned = Math.Max(0, monthlyPlanned),
                Deadline = HasDeadline ? Deadline : null,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                IsActive = true,
                SortOrder = 100,
            });
        }
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!IsExisting) return;
        var ok = await Shell.Current.DisplayAlert("Eliminar", "¿Eliminar esta meta?", "Sí", "No");
        if (!ok) return;
        await _svc.DeleteAsync(GoalId);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync() => await Shell.Current.GoToAsync("..");
}
