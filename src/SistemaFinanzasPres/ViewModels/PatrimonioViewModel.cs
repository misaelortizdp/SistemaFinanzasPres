using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaFinanzasPres.Services;

namespace SistemaFinanzasPres.ViewModels;

public record SnapshotRow(
    int Id,
    DateTime Date,
    string DateLabel,
    string AssetsLabel,
    string LiabilitiesLabel,
    decimal NetWorth,
    string NetWorthLabel,
    string DeltaLabel,
    Color DeltaColor,
    string? Notes,
    bool HasNotes);

public partial class PatrimonioViewModel : BaseViewModel
{
    private readonly NetWorthService _svc;

    public PatrimonioViewModel(NetWorthService svc)
    {
        _svc = svc;
        Title = "Patrimonio";
    }

    [ObservableProperty] private string totalAssetsLabel = "$0";
    [ObservableProperty] private string totalLiabilitiesLabel = "$0";
    [ObservableProperty] private string netWorthLabel = "$0";
    [ObservableProperty] private Color netWorthColor = Color.FromArgb("#10B981");
    [ObservableProperty] private string netWorthHint = "Sin snapshots aún";
    [ObservableProperty] private bool hasSnapshots;

    public ObservableCollection<NetWorthLine> Assets { get; } = new();
    public ObservableCollection<NetWorthLine> Liabilities { get; } = new();
    public ObservableCollection<SnapshotRow> Snapshots { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            try { await _svc.EnsureMonthlySnapshotAsync(); } catch { }

            var b = await _svc.GetCurrentAsync();
            Assets.Clear();
            foreach (var a in b.Assets) Assets.Add(a);
            Liabilities.Clear();
            foreach (var l in b.Liabilities) Liabilities.Add(l);

            TotalAssetsLabel = b.TotalAssets.ToString("C0");
            TotalLiabilitiesLabel = b.TotalLiabilities.ToString("C0");
            NetWorthLabel = b.NetWorth.ToString("C0");
            NetWorthColor = b.NetWorth >= 0 ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");

            var snaps = await _svc.GetSnapshotsAsync();
            Snapshots.Clear();
            for (int i = 0; i < snaps.Count; i++)
            {
                var s = snaps[i];
                var prev = i + 1 < snaps.Count ? snaps[i + 1] : null;
                string delta = "—";
                var deltaColor = Colors.Gray;
                if (prev != null)
                {
                    var diff = s.NetWorth - prev.NetWorth;
                    delta = (diff >= 0 ? "▲ +" : "▼ ") + diff.ToString("C0");
                    deltaColor = diff >= 0 ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
                }
                Snapshots.Add(new SnapshotRow(
                    s.Id, s.Date, s.Date.ToString("dd MMM yyyy"),
                    s.Assets.ToString("C0"),
                    s.Liabilities.ToString("C0"),
                    s.NetWorth, s.NetWorth.ToString("C0"),
                    delta, deltaColor, s.Notes, !string.IsNullOrWhiteSpace(s.Notes)));
            }

            HasSnapshots = snaps.Count > 0;
            if (snaps.Count > 0)
            {
                var latest = snaps[0];
                var diff = b.NetWorth - latest.NetWorth;
                NetWorthHint = diff == 0
                    ? $"Sin cambios desde {latest.Date:dd MMM yyyy}"
                    : (diff > 0 ? "▲ +" : "▼ ") + diff.ToString("C0") + $" vs {latest.Date:dd MMM yyyy}";
            }
            else
            {
                NetWorthHint = "Sin snapshots aún. Toma uno para empezar a medir tu evolución.";
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task TakeSnapshotAsync()
    {
        var notes = await Shell.Current.DisplayPromptAsync(
            "Nuevo snapshot",
            "Nota opcional (ej: corte fin de mes)",
            "Guardar", "Cancelar",
            placeholder: "Opcional",
            maxLength: 500);
        if (notes == null) return;
        await _svc.TakeSnapshotAsync(string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteSnapshotAsync(SnapshotRow? row)
    {
        if (row is null) return;
        var ok = await Shell.Current.DisplayAlert(
            "Eliminar snapshot",
            $"¿Eliminar el snapshot del {row.DateLabel}?", "Sí", "No");
        if (!ok) return;
        await _svc.DeleteSnapshotAsync(row.Id);
        await LoadAsync();
    }
}
