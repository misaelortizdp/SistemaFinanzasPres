using System.Globalization;

namespace SistemaFinanzasPres.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value?.ToString() ?? string.Empty;
        if (s.StartsWith("🔴") || s.StartsWith("⚠️")) return Color.FromArgb("#EF4444");
        if (s.StartsWith("🟡")) return Color.FromArgb("#F59E0B");
        if (s.StartsWith("🟢") || s.StartsWith("✅")) return Color.FromArgb("#10B981");
        if (s.StartsWith("⬜")) return Color.FromArgb("#9CA3AF");
        if (s.StartsWith("🙏")) return Color.FromArgb("#A855F7");
        return Color.FromArgb("#64748B");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PillarToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value?.ToString() ?? string.Empty;
        if (s.Contains("Necesidad") || s.Contains("Nec."))  return Color.FromArgb("#DBEAFE");
        if (s.Contains("Deseo")     || s.Contains("Des."))  return Color.FromArgb("#FFF0F6");
        if (s.Contains("Deuda")     || s.Contains("Deu."))  return Color.FromArgb("#FFEDD5");
        if (s.Contains("Ahorro")    || s.Contains("Aho."))  return Color.FromArgb("#D1FAE5");
        if (s.Contains("Primer")    || s.Contains("P.F."))  return Color.FromArgb("#FEF3C7");
        return Color.FromArgb("#F1F5F9");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InvariantBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;
}
