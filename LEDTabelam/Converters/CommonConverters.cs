using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LEDTabelam.Models;

namespace LEDTabelam.Converters;

/// <summary>
/// Bool değerini "Açık"/"Kapalı" metnine dönüştürür
/// </summary>
public class BoolToOnOffConverter : IValueConverter
{
    public static readonly BoolToOnOffConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "Açık" : "Kapalı";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == "Açık";
    }
}

/// <summary>
/// Bool değerini seçim arka plan rengine dönüştürür
/// </summary>
public class BoolToSelectionBrushConverter : IValueConverter
{
    public static readonly BoolToSelectionBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.FromArgb(40, 0, 120, 215))
            : Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// TabelaItemType'ı ikon karakterine dönüştürür
/// </summary>
public class ItemTypeToIconConverter : IValueConverter
{
    public static readonly ItemTypeToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            TabelaItemType.Text => "T",
            TabelaItemType.Symbol => "★",
            TabelaItemType.Image => "🖼",
            TabelaItemType.Clock => "⏰",
            TabelaItemType.Date => "📅",
            _ => "?"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Color'ı SolidColorBrush'a dönüştürür
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return new SolidColorBrush(color);
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Bool değerini play/pause renk durumuna dönüştürür
/// </summary>
public class BoolToPlayColorConverter : IValueConverter
{
    public static readonly BoolToPlayColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Yeşil - oynatılıyor
            : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gri - duraklatıldı
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Bool değerini play/pause ikonuna dönüştürür
/// Requirements: 7.1
/// </summary>
public class BoolToPlayPauseIconConverter : IValueConverter
{
    public static readonly BoolToPlayPauseIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "⏸" : "▶";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
