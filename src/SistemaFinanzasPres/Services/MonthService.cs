using System.Globalization;

namespace SistemaFinanzasPres.Services;

public class MonthService
{
    public int Year { get; private set; } = DateTime.Today.Year;
    public int Month { get; private set; } = DateTime.Today.Month;

    public event EventHandler? Changed;

    public DateTime Start => new(Year, Month, 1);
    public DateTime End => Start.AddMonths(1);

    public string Label =>
        CultureInfo.GetCultureInfo("es-CO").DateTimeFormat.GetMonthName(Month) + " " + Year;

    public void Set(int year, int month)
    {
        Year = year;
        Month = month;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Shift(int months)
    {
        var d = Start.AddMonths(months);
        Set(d.Year, d.Month);
    }
}
