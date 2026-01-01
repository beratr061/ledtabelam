using System.Globalization;
using LEDTabelam.Maui.Models;
using LEDTabelam.Maui.ViewModels;
using HorizontalAlignment = LEDTabelam.Maui.Models.HorizontalAlignment;
using VerticalAlignment = LEDTabelam.Maui.Models.VerticalAlignment;

namespace LEDTabelam.Maui.Converters;

/// <summary>
/// Boolean değerini tersine çevirir
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// EffectType enum değerini Türkçe isme dönüştürür
/// Requirement: 5.4
/// </summary>
public class EffectTypeToNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EffectType effectType)
        {
            return PropertiesViewModel.GetEffectTypeName(effectType);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// EffectDirection enum değerini Türkçe isme dönüştürür
/// Requirement: 5.4
/// </summary>
public class DirectionToNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EffectDirection direction)
        {
            return PropertiesViewModel.GetDirectionName(direction);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// EffectType'ın SlideIn olup olmadığını kontrol eder (yön seçici görünürlüğü için)
/// Requirement: 5.4
/// </summary>
public class EffectTypeToDirectionVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EffectType effectType)
        {
            return effectType == EffectType.SlideIn;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// BorderStyle enum değerini Türkçe isme dönüştürür
/// Requirement: 5.8
/// </summary>
public class BorderStyleToNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BorderStyle borderStyle)
        {
            return PropertiesViewModel.GetBorderStyleName(borderStyle);
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// BorderStyle'ın Custom olup olmadığını kontrol eder
/// Requirement: 5.9
/// </summary>
public class BorderStyleToCustomVisibleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BorderStyle borderStyle)
        {
            return borderStyle == BorderStyle.Custom;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// BorderStyle'ı çerçeve kalınlığına dönüştürür
/// Requirement: 5.8
/// </summary>
public class BorderStyleToThicknessConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BorderStyle borderStyle)
        {
            return borderStyle switch
            {
                BorderStyle.None => 0.0,
                BorderStyle.Solid => 2.0,
                BorderStyle.Dashed => 2.0,
                BorderStyle.Custom => 2.0,
                _ => 0.0
            };
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// BorderStyle'ı dash array'e dönüştürür
/// Requirement: 5.8
/// </summary>
public class BorderStyleToDashArrayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BorderStyle borderStyle)
        {
            return borderStyle switch
            {
                BorderStyle.Dashed => new DoubleCollection { 4, 2 },
                _ => new DoubleCollection()
            };
        }
        return new DoubleCollection();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Milisaniyeyi saniyeye dönüştürür (iki yönlü)
/// Requirement: 5.7
/// </summary>
public class MillisecondsToSecondsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int milliseconds)
        {
            return (milliseconds / 1000.0).ToString("F1", culture);
        }
        return "0";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && double.TryParse(str, NumberStyles.Any, culture, out var seconds))
        {
            return (int)(seconds * 1000);
        }
        return 0;
    }
}


/// <summary>
/// Boolean değerini seçili/seçilmemiş arka plan rengine dönüştürür
/// Requirement: 6.6 - Stil butonları için görsel geri bildirim
/// </summary>
public class BoolToSelectedColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Color.FromArgb("#094771"); // TreeViewSelected color
        }
        return Color.FromArgb("#3C3C3C"); // ButtonSecondary color
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// HorizontalAlignment değerini sol hizalama buton rengine dönüştürür
/// </summary>
public class HAlignToLeftColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HorizontalAlignment align && align == HorizontalAlignment.Left)
        {
            return Color.FromArgb("#4A6FA5");
        }
        return Color.FromArgb("#3D3D3D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// HorizontalAlignment değerini orta hizalama buton rengine dönüştürür
/// </summary>
public class HAlignToCenterColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HorizontalAlignment align && align == HorizontalAlignment.Center)
        {
            return Color.FromArgb("#4A6FA5");
        }
        return Color.FromArgb("#3D3D3D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// HorizontalAlignment değerini sağ hizalama buton rengine dönüştürür
/// </summary>
public class HAlignToRightColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HorizontalAlignment align && align == HorizontalAlignment.Right)
        {
            return Color.FromArgb("#4A6FA5");
        }
        return Color.FromArgb("#3D3D3D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// VerticalAlignment değerini üst hizalama buton rengine dönüştürür
/// </summary>
public class VAlignToTopColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VerticalAlignment align && align == VerticalAlignment.Top)
        {
            return Color.FromArgb("#4A6FA5");
        }
        return Color.FromArgb("#3D3D3D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// VerticalAlignment değerini orta hizalama buton rengine dönüştürür
/// </summary>
public class VAlignToMiddleColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VerticalAlignment align && align == VerticalAlignment.Center)
        {
            return Color.FromArgb("#4A6FA5");
        }
        return Color.FromArgb("#3D3D3D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// VerticalAlignment değerini alt hizalama buton rengine dönüştürür
/// </summary>
public class VAlignToBottomColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VerticalAlignment align && align == VerticalAlignment.Bottom)
        {
            return Color.FromArgb("#4A6FA5");
        }
        return Color.FromArgb("#3D3D3D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
