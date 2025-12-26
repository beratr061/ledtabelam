using System;
using System.Globalization;
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
