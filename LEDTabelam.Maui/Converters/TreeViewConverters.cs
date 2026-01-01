using System.Globalization;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Converters;

/// <summary>
/// Boolean değeri genişlet/daralt ikonuna dönüştürür
/// </summary>
public class BoolToExpandIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "▼" : "▶";
        }
        return "▶";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// ContentType enum değerini emoji ikona dönüştürür
/// </summary>
public class ContentTypeToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ContentType contentType)
        {
            return contentType switch
            {
                ContentType.Text => "✏",
                ContentType.Image => "🖼",
                ContentType.Clock => "🕐",
                ContentType.Date => "📅",
                ContentType.Countdown => "⏱",
                _ => "📄"
            };
        }
        return "📄";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Sıfır değerini görünürlüğe dönüştürür (0 = Visible, >0 = Collapsed)
/// </summary>
public class ZeroToVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Seçili öğeyi arka plan rengine dönüştürür
/// </summary>
public class SelectedToBackgroundConverter : IValueConverter
{
    private static readonly Color SelectedColor = Color.FromArgb("#4A6FA5");
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return SelectedColor;
        }
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// İki değeri karşılaştırarak boolean döndürür (seçim kontrolü için)
/// </summary>
public class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2)
        {
            return values[0] == values[1];
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
