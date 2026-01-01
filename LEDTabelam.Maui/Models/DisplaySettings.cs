using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// LED tabela görüntüleme ayarları
/// </summary>
public partial class DisplaySettings : ObservableObject
{
    [ObservableProperty]
    private int _panelWidth = 160;

    [ObservableProperty]
    private int _panelHeight = 24;

    [ObservableProperty]
    private LedColorType _colorType = LedColorType.Amber;

    [ObservableProperty]
    private Color _customColor = Color.FromRgba(255, 176, 0, 255);

    [ObservableProperty]
    private int _brightness = 100;

    [ObservableProperty]
    private int _backgroundDarkness = 100;

    [ObservableProperty]
    private int _pixelSize = 4;

    [ObservableProperty]
    private PixelPitch _pitch = PixelPitch.P10;

    [ObservableProperty]
    private double _customPitchRatio = 0.7;

    [ObservableProperty]
    private PixelShape _shape = PixelShape.Round;

    [ObservableProperty]
    private int _zoomLevel = 100;

    [ObservableProperty]
    private bool _invertColors = false;

    [ObservableProperty]
    private int _agingPercent = 0;

    [ObservableProperty]
    private int _letterSpacing = 1;

    /// <summary>
    /// LED matris genişliği (piksel) - Pitch çarpanı uygulanmış gerçek çözünürlük
    /// </summary>
    public int Width
    {
        get => PanelWidth * Pitch.GetResolutionMultiplier();
        set => PanelWidth = value / Math.Max(1, Pitch.GetResolutionMultiplier());
    }

    /// <summary>
    /// LED matris yüksekliği (piksel) - Pitch çarpanı uygulanmış gerçek çözünürlük
    /// </summary>
    public int Height
    {
        get => PanelHeight * Pitch.GetResolutionMultiplier();
        set => PanelHeight = value / Math.Max(1, Pitch.GetResolutionMultiplier());
    }

    public int ActualWidth => Width;
    public int ActualHeight => Height;

    /// <summary>
    /// Seçili renk tipine göre LED rengini döndürür
    /// </summary>
    public Color GetLedColor()
    {
        return ColorType switch
        {
            LedColorType.Amber => Color.FromRgb(255, 176, 0),
            LedColorType.Red => Color.FromRgb(255, 0, 0),
            LedColorType.Green => Color.FromRgb(0, 255, 0),
            _ => CustomColor
        };
    }
}
