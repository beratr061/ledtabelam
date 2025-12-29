using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using LEDTabelam.Models;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Zone düzenleyici için yardımcı sınıf
/// </summary>
public static class ZoneEditorHelper
{
    public static ZoneContentType[] ContentTypes { get; } = Enum.GetValues<ZoneContentType>();
    public static HorizontalAlignment[] HAlignOptions { get; } = Enum.GetValues<HorizontalAlignment>();
    public static VerticalAlignment[] VAlignOptions { get; } = Enum.GetValues<VerticalAlignment>();
}

/// <summary>
/// Zone indeksine göre renk döndüren converter
/// </summary>
public class ZoneColorConverter : IValueConverter
{
    public static readonly ZoneColorConverter Instance = new();

    private static readonly IBrush[] Colors = new IBrush[]
    {
        new SolidColorBrush(Color.FromRgb(0, 120, 212)),   // Mavi
        new SolidColorBrush(Color.FromRgb(16, 124, 16)),   // Yeşil
        new SolidColorBrush(Color.FromRgb(202, 80, 16)),   // Turuncu
        new SolidColorBrush(Color.FromRgb(136, 23, 152)),  // Mor
        new SolidColorBrush(Color.FromRgb(0, 153, 188)),   // Cyan
        new SolidColorBrush(Color.FromRgb(177, 70, 194)),  // Pembe
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return Colors[index % Colors.Length];
        }
        return Colors[0];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Avalonia Color'ı Brush'a dönüştüren converter
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Color.FromRgb(255, 176, 0)); // Varsayılan Amber
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Yüzde değerini genişliğe dönüştüren converter
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public static readonly PercentToWidthConverter Instance = new();
    
    // Toplam genişlik (piksel olarak)
    private const double TotalWidth = 700;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            return percent / 100.0 * TotalWidth;
        }
        return 100.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// ContentType'a göre görünürlük döndüren converter (ScrollingText için)
/// </summary>
public class ContentTypeToVisibilityConverter : IValueConverter
{
    public static readonly ContentTypeToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ZoneContentType contentType)
        {
            return contentType == ZoneContentType.Text || contentType == ZoneContentType.ScrollingText;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Yüzde değerini GridLength'e dönüştüren converter
/// </summary>
public class PercentToGridLengthConverter : IValueConverter
{
    public static readonly PercentToGridLengthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int percent)
        {
            return new Avalonia.Controls.GridLength(percent, Avalonia.Controls.GridUnitType.Star);
        }
        if (value is double percentDouble)
        {
            return new Avalonia.Controls.GridLength(percentDouble, Avalonia.Controls.GridUnitType.Star);
        }
        return new Avalonia.Controls.GridLength(1, Avalonia.Controls.GridUnitType.Star);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Bool değerini seçim arka plan rengine dönüştüren converter
/// </summary>
public class BoolToSelectionBrushConverter : IValueConverter
{
    public static readonly BoolToSelectionBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return new SolidColorBrush(Color.FromRgb(0, 90, 158)); // Mavi seçim
        }
        return new SolidColorBrush(Color.FromRgb(45, 45, 45)); // Normal arka plan
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// HorizontalAlignment'ı ComboBox index'ine dönüştüren converter
/// </summary>
public class HAlignToIndexConverter : IValueConverter
{
    public static readonly HAlignToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Models.HorizontalAlignment align)
        {
            return align switch
            {
                Models.HorizontalAlignment.Left => 0,
                Models.HorizontalAlignment.Center => 1,
                Models.HorizontalAlignment.Right => 2,
                _ => 1
            };
        }
        return 1;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => Models.HorizontalAlignment.Left,
                1 => Models.HorizontalAlignment.Center,
                2 => Models.HorizontalAlignment.Right,
                _ => Models.HorizontalAlignment.Center
            };
        }
        return Models.HorizontalAlignment.Center;
    }
}

/// <summary>
/// VerticalAlignment'ı ComboBox index'ine dönüştüren converter
/// </summary>
public class VAlignToIndexConverter : IValueConverter
{
    public static readonly VAlignToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Models.VerticalAlignment align)
        {
            return align switch
            {
                Models.VerticalAlignment.Top => 0,
                Models.VerticalAlignment.Center => 1,
                Models.VerticalAlignment.Bottom => 2,
                _ => 1
            };
        }
        return 1;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index switch
            {
                0 => Models.VerticalAlignment.Top,
                1 => Models.VerticalAlignment.Center,
                2 => Models.VerticalAlignment.Bottom,
                _ => Models.VerticalAlignment.Center
            };
        }
        return Models.VerticalAlignment.Center;
    }
}

/// <summary>
/// ScrollDirection'ı ComboBox index'ine dönüştüren converter
/// </summary>
public class ScrollDirToIndexConverter : IValueConverter
{
    public static readonly ScrollDirToIndexConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Models.ScrollDirection dir)
        {
            return (int)dir;
        }
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return (Models.ScrollDirection)index;
        }
        return Models.ScrollDirection.Left;
    }
}

/// <summary>
/// TabelaItemType'ı ikon karakterine dönüştüren converter
/// </summary>
public class ItemTypeToIconConverter : IValueConverter
{
    public static readonly ItemTypeToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TabelaItemType itemType)
        {
            return itemType switch
            {
                TabelaItemType.Text => "T",
                TabelaItemType.Symbol => "◆",
                TabelaItemType.Image => "🖼",
                TabelaItemType.Clock => "⏰",
                TabelaItemType.Date => "📅",
                _ => "?"
            };
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// TabelaItem içeriğini görüntüleme için dönüştüren multi-value converter
/// </summary>
public class ItemContentDisplayConverter : IMultiValueConverter
{
    public static readonly ItemContentDisplayConverter Instance = new();

    public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 3)
        {
            var itemType = values[0] as TabelaItemType? ?? TabelaItemType.Text;
            var content = values[1] as string ?? "";
            var symbolName = values[2] as string ?? "";

            if (itemType == TabelaItemType.Symbol)
            {
                return string.IsNullOrEmpty(symbolName) ? "(sembol seçilmedi)" : $"[{symbolName}]";
            }
            
            return string.IsNullOrEmpty(content) ? "(boş)" : content;
        }
        return "";
    }
}
