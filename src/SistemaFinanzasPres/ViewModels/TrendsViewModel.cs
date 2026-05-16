using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.ViewModels;

public record TrendMonth(
    string Label,
    int Year,
    int Month,
    decimal IngresoDisponible,
    decimal TotalGastado,
    decimal Ahorro,
    decimal Neto,
    decimal TasaAhorro,
    string TendIngreso,
    string TendGasto,
    Color TendIngresoColor,
    Color TendGastoColor,
    double IngresoBar,
    double GastoBar,
    bool IsCurrentMonth);

public partial class TrendsViewModel : BaseViewModel
{
    private readonly AppDbContext _db;

    public TrendsViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Tendencias";
    }

    public ObservableCollection<TrendMonth> Months { get; } = new();

    [ObservableProperty] private string mejorMes = "—";
    [ObservableProperty] private string peorMes = "—";
    [ObservableProperty] private string promedioAhorro = "0%";
    [ObservableProperty] private string promedioGasto = "$0";

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            var cfg = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync() ?? new AppConfig();
            var diezmoPct = cfg.DiezmoPct;

            var today = DateTime.Today;
            var monthList = Enumerable.Range(0, 6)
                .Select(i => { var d = today.AddMonths(-5 + i); return (d.Year, d.Month); })
                .ToList();

            var rangeStart = new DateTime(monthList[0].Year, monthList[0].Month, 1);
            var rangeEnd = new DateTime(monthList[^1].Year, monthList[^1].Month, 1).AddMonths(1);

            var incomesByMonth = (await _db.Incomes.AsNoTracking()
                .Where(i => i.Date >= rangeStart && i.Date < rangeEnd)
                .Select(i => new { i.Date.Year, i.Date.Month, i.Amount })
                .ToListAsync())
                .GroupBy(i => (i.Year, i.Month))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var ahoroCatIds = await _db.Categories.AsNoTracking()
                .Where(c => c.Pillar == Pillar.Ahorro && c.IsActive)
                .Select(c => c.Id).ToListAsync();

            var primerFrutoCatIds = await _db.Categories.AsNoTracking()
                .Where(c => c.Pillar == Pillar.PrimerFruto)
                .Select(c => c.Id).ToListAsync();

            var allTx = await _db.Transactions.AsNoTracking()
                .Where(t => t.Date >= rangeStart && t.Date < rangeEnd
                            && !primerFrutoCatIds.Contains(t.CategoryId))
                .Select(t => new { t.Date.Year, t.Date.Month, t.Amount, t.CategoryId })
                .ToListAsync();

            var txByMonth = allTx
                .GroupBy(t => (t.Year, t.Month))
                .ToDictionary(g => g.Key, g => g.ToList());

            var raw = monthList.Select(ym =>
            {
                var ingresoTotal = incomesByMonth.GetValueOrDefault(ym);
                var diezmo = ingresoTotal * diezmoPct;
                var disponible = ingresoTotal - diezmo;
                var txs = txByMonth.GetValueOrDefault(ym);
                var gastado = txs?.Sum(t => t.Amount) ?? 0m;
                var ahorro = txs?.Where(t => ahoroCatIds.Contains(t.CategoryId)).Sum(t => t.Amount) ?? 0m;
                return (ym, disponible, gastado, ahorro);
            }).ToList();

            var maxIngreso = raw.Max(r => r.disponible);
            if (maxIngreso <= 0) maxIngreso = 1m;

            var rows = new List<TrendMonth>();
            for (int i = 0; i < raw.Count; i++)
            {
                var (ym, disp, gast, aho) = raw[i];
                var neto = disp - gast;
                var tasa = disp > 0 ? aho / disp : 0m;

                string tendI = "—", tendG = "—";
                var colorI = Colors.Gray;
                var colorG = Colors.Gray;

                if (i > 0)
                {
                    var (_, prevDisp, prevGast, _) = raw[i - 1];
                    if (prevDisp > 0)
                    {
                        var pctI = (disp - prevDisp) / prevDisp;
                        tendI = (pctI >= 0 ? "↑ +" : "↓ ") + pctI.ToString("P0");
                        colorI = pctI >= 0 ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
                    }
                    if (prevGast > 0)
                    {
                        var pctG = (gast - prevGast) / prevGast;
                        tendG = (pctG >= 0 ? "↑ +" : "↓ ") + pctG.ToString("P0");
                        colorG = pctG <= 0 ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
                    }
                }

                var label = new DateTime(ym.Year, ym.Month, 1).ToString("MMM yyyy");
                rows.Add(new TrendMonth(
                    label, ym.Year, ym.Month,
                    disp, gast, aho, neto, tasa,
                    tendI, tendG, colorI, colorG,
                    (double)(disp / maxIngreso),
                    (double)Math.Min(1m, gast / maxIngreso),
                    ym.Year == today.Year && ym.Month == today.Month));
            }

            Months.Clear();
            foreach (var r in rows) Months.Add(r);

            var withData = rows.Where(r => r.IngresoDisponible > 0).ToList();
            if (withData.Count > 0)
            {
                var mejor = withData.OrderByDescending(r => r.Neto).First();
                var peor = withData.OrderBy(r => r.Neto).First();
                MejorMes = $"{mejor.Label} (neto {mejor.Neto:C0})";
                PeorMes = $"{peor.Label} (neto {peor.Neto:C0})";
                PromedioAhorro = (withData.Average(r => r.TasaAhorro)).ToString("P1");
                PromedioGasto = (withData.Average(r => r.TotalGastado)).ToString("C0");
            }
        }
        finally { IsBusy = false; }
    }
}
