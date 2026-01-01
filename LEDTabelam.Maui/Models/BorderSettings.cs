using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Öğe çerçeve ayarları
/// </summary>
public partial class BorderSettings : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = false;

    [ObservableProperty]
    private int _horizontalLines = 1;

    [ObservableProperty]
    private int _verticalLines = 1;

    [ObservableProperty]
    private int _padding = 1;

    [ObservableProperty]
    private Color _color = Color.FromRgb(255, 0, 0);

    public int TotalHorizontalThickness => HorizontalLines + Padding;
    public int TotalVerticalThickness => VerticalLines + Padding;

    partial void OnHorizontalLinesChanging(int value)
    {
        if (value < 1) _horizontalLines = 1;
        else if (value > 5) _horizontalLines = 5;
    }

    partial void OnVerticalLinesChanging(int value)
    {
        if (value < 1) _verticalLines = 1;
        else if (value > 5) _verticalLines = 5;
    }

    partial void OnPaddingChanging(int value)
    {
        if (value < 0) _padding = 0;
        else if (value > 10) _padding = 10;
    }

    public static BorderSettings CreateDefault()
    {
        return new BorderSettings
        {
            IsEnabled = false,
            HorizontalLines = 1,
            VerticalLines = 1,
            Padding = 1,
            Color = Color.FromRgb(255, 0, 0)
        };
    }

    public BorderSettings Clone()
    {
        return new BorderSettings
        {
            IsEnabled = IsEnabled,
            HorizontalLines = HorizontalLines,
            VerticalLines = VerticalLines,
            Padding = Padding,
            Color = Color
        };
    }
}
