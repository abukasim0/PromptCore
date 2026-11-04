using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PromptCore.UI;

/// <summary>Formats stored UTC timestamps for the current culture, e.g. "04/11/2026 18:42".</summary>
public sealed class LocalDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime d && d != DateTime.MinValue
            ? d.ToLocalTime().ToString("g", culture)
            : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Maps a favorite flag to the localized star glyph (★ / ☆).</summary>
public sealed class StarTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var on = value is bool b && b;
        return Application.Current.TryFindResource(on ? "Library.FavoriteOn" : "Library.FavoriteOff") as string ?? (on ? "★" : "☆");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Renders the library count as a localized string ("{0} prompts" / "{0} برومبت").</summary>
public sealed class CountTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var template = Application.Current.TryFindResource("Library.Count") as string ?? "{0}";
        return string.Format(culture, template, value ?? 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
