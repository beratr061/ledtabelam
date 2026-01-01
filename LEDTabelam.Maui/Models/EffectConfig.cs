using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Efekt tipi seçenekleri
/// </summary>
public enum EffectType
{
    /// <summary>Hemen Göster</summary>
    Immediate,
    /// <summary>Kayarak Gir</summary>
    SlideIn,
    /// <summary>Solarak Gir</summary>
    FadeIn,
    /// <summary>Efekt Yok</summary>
    None
}

/// <summary>
/// Efekt yönü seçenekleri
/// </summary>
public enum EffectDirection
{
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// Efekt yapılandırması
/// </summary>
public partial class EffectConfig : ObservableObject
{
    [ObservableProperty]
    private EffectType _effectType = EffectType.Immediate;

    [ObservableProperty]
    private int _speedMs = 500;

    [ObservableProperty]
    private EffectDirection _direction = EffectDirection.Left;
}
