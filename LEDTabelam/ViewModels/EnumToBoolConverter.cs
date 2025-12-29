using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Enum değerlerini bool'a dönüştüren converter (RadioButton binding için)
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter != null)
        {
            return parameter;
        }

        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// String eşitlik kontrolü için converter (Segmented butonlar için)
/// </summary>
public class StringEqualityConverter : IValueConverter
{
    public static readonly StringEqualityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter != null)
        {
            return parameter.ToString();
        }

        return Avalonia.Data.BindingOperations.DoNothing;
    }
}

/// <summary>
/// Bool değerini BorderThickness'a dönüştüren converter (Renk paleti seçimi için)
/// </summary>
public class BoolToThicknessConverter : IValueConverter
{
    public static readonly BoolToThicknessConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked)
        {
            return new Thickness(3);
        }
        return new Thickness(1);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
