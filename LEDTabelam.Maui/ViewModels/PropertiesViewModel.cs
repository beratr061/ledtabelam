using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.ViewModels;

/// <summary>
/// Çerçeve stili seçenekleri
/// </summary>
public enum BorderStyle
{
    None,
    Solid,
    Dashed,
    Custom
}

/// <summary>
/// Özellikler paneli ViewModel'i
/// Seçili içeriğin efekt, süre ve görünüm özelliklerini yönetir
/// Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9
/// </summary>
public partial class PropertiesViewModel : ObservableObject
{
    [ObservableProperty]
    private ContentItem? _selectedContent;

    [ObservableProperty]
    private EffectType _selectedEntryEffect = EffectType.Immediate;

    [ObservableProperty]
    private EffectType _selectedExitEffect = EffectType.Immediate;

    [ObservableProperty]
    private EffectDirection _selectedEntryDirection = EffectDirection.Left;

    [ObservableProperty]
    private EffectDirection _selectedExitDirection = EffectDirection.Left;

    [ObservableProperty]
    private int _effectSpeed = 500;

    [ObservableProperty]
    private int _displayDuration = 3000;

    [ObservableProperty]
    private bool _showImmediately = true;

    [ObservableProperty]
    private bool _isTimed = false;

    [ObservableProperty]
    private BorderStyle _borderStyle = BorderStyle.None;

    [ObservableProperty]
    private Color _backgroundColor = Colors.Transparent;

    [ObservableProperty]
    private bool _hasSelection = false;

    /// <summary>
    /// Kullanılabilir giriş efektleri
    /// </summary>
    public IReadOnlyList<EffectType> AvailableEntryEffects { get; } = new[]
    {
        EffectType.Immediate,
        EffectType.SlideIn,
        EffectType.FadeIn,
        EffectType.None
    };

    /// <summary>
    /// Kullanılabilir çıkış efektleri
    /// </summary>
    public IReadOnlyList<EffectType> AvailableExitEffects { get; } = new[]
    {
        EffectType.Immediate,
        EffectType.SlideIn,
        EffectType.FadeIn,
        EffectType.None
    };

    /// <summary>
    /// Kullanılabilir efekt yönleri
    /// </summary>
    public IReadOnlyList<EffectDirection> AvailableDirections { get; } = new[]
    {
        EffectDirection.Left,
        EffectDirection.Right,
        EffectDirection.Up,
        EffectDirection.Down
    };

    /// <summary>
    /// Kullanılabilir çerçeve stilleri
    /// </summary>
    public IReadOnlyList<BorderStyle> AvailableBorderStyles { get; } = new[]
    {
        BorderStyle.None,
        BorderStyle.Solid,
        BorderStyle.Dashed,
        BorderStyle.Custom
    };

    /// <summary>
    /// Efekt tipi için Türkçe isim döndürür
    /// </summary>
    public static string GetEffectTypeName(EffectType effectType)
    {
        return effectType switch
        {
            EffectType.Immediate => "Hemen Göster",
            EffectType.SlideIn => "Kayarak Gir",
            EffectType.FadeIn => "Solarak Gir",
            EffectType.None => "Efekt Yok",
            _ => effectType.ToString()
        };
    }

    /// <summary>
    /// Efekt yönü için Türkçe isim döndürür
    /// </summary>
    public static string GetDirectionName(EffectDirection direction)
    {
        return direction switch
        {
            EffectDirection.Left => "Soldan",
            EffectDirection.Right => "Sağdan",
            EffectDirection.Up => "Yukarıdan",
            EffectDirection.Down => "Aşağıdan",
            _ => direction.ToString()
        };
    }

    /// <summary>
    /// Çerçeve stili için Türkçe isim döndürür
    /// </summary>
    public static string GetBorderStyleName(BorderStyle style)
    {
        return style switch
        {
            BorderStyle.None => "Yok",
            BorderStyle.Solid => "Düz",
            BorderStyle.Dashed => "Kesikli",
            BorderStyle.Custom => "Özel",
            _ => style.ToString()
        };
    }

    partial void OnSelectedContentChanged(ContentItem? value)
    {
        HasSelection = value != null;
        
        if (value != null)
        {
            LoadContentProperties(value);
        }
        else
        {
            ResetToDefaults();
        }
    }

    partial void OnSelectedEntryEffectChanged(EffectType value)
    {
        if (SelectedContent != null)
        {
            SelectedContent.EntryEffect.EffectType = value;
        }
    }

    partial void OnSelectedExitEffectChanged(EffectType value)
    {
        if (SelectedContent != null)
        {
            SelectedContent.ExitEffect.EffectType = value;
        }
    }

    partial void OnSelectedEntryDirectionChanged(EffectDirection value)
    {
        if (SelectedContent != null)
        {
            SelectedContent.EntryEffect.Direction = value;
        }
    }

    partial void OnSelectedExitDirectionChanged(EffectDirection value)
    {
        if (SelectedContent != null)
        {
            SelectedContent.ExitEffect.Direction = value;
        }
    }

    partial void OnEffectSpeedChanged(int value)
    {
        if (SelectedContent != null)
        {
            SelectedContent.EntryEffect.SpeedMs = value;
            SelectedContent.ExitEffect.SpeedMs = value;
        }
    }

    partial void OnDisplayDurationChanged(int value)
    {
        if (SelectedContent != null)
        {
            SelectedContent.DurationMs = value;
        }
    }

    partial void OnShowImmediatelyChanged(bool value)
    {
        if (SelectedContent != null)
        {
            SelectedContent.ShowImmediately = value;
            if (value)
            {
                IsTimed = false;
            }
        }
    }

    partial void OnIsTimedChanged(bool value)
    {
        if (value)
        {
            ShowImmediately = false;
        }
    }

    private void LoadContentProperties(ContentItem content)
    {
        // Efekt ayarları
        SelectedEntryEffect = content.EntryEffect.EffectType;
        SelectedExitEffect = content.ExitEffect.EffectType;
        SelectedEntryDirection = content.EntryEffect.Direction;
        SelectedExitDirection = content.ExitEffect.Direction;
        EffectSpeed = content.EntryEffect.SpeedMs;
        
        // Süre ayarları
        DisplayDuration = content.DurationMs;
        ShowImmediately = content.ShowImmediately;
        IsTimed = !content.ShowImmediately && content.DurationMs > 0;
    }

    private void ResetToDefaults()
    {
        SelectedEntryEffect = EffectType.Immediate;
        SelectedExitEffect = EffectType.Immediate;
        SelectedEntryDirection = EffectDirection.Left;
        SelectedExitDirection = EffectDirection.Left;
        EffectSpeed = 500;
        DisplayDuration = 3000;
        ShowImmediately = true;
        IsTimed = false;
        BorderStyle = BorderStyle.None;
        BackgroundColor = Colors.Transparent;
    }

    /// <summary>
    /// Giriş efektini hemen göster olarak ayarlar
    /// Requirement: 5.6
    /// </summary>
    [RelayCommand]
    public void SetImmediateEntry()
    {
        SelectedEntryEffect = EffectType.Immediate;
    }

    /// <summary>
    /// Çıkış efektini hemen kapat olarak ayarlar
    /// </summary>
    [RelayCommand]
    public void SetImmediateExit()
    {
        SelectedExitEffect = EffectType.Immediate;
    }

    /// <summary>
    /// Efekt hızını artırır
    /// Requirement: 5.5
    /// </summary>
    [RelayCommand]
    public void IncreaseSpeed()
    {
        EffectSpeed = Math.Min(5000, EffectSpeed + 100);
    }

    /// <summary>
    /// Efekt hızını azaltır
    /// Requirement: 5.5
    /// </summary>
    [RelayCommand]
    public void DecreaseSpeed()
    {
        EffectSpeed = Math.Max(100, EffectSpeed - 100);
    }

    /// <summary>
    /// Gösterim süresini artırır
    /// Requirement: 5.7
    /// </summary>
    [RelayCommand]
    public void IncreaseDuration()
    {
        DisplayDuration = Math.Min(60000, DisplayDuration + 500);
    }

    /// <summary>
    /// Gösterim süresini azaltır
    /// Requirement: 5.7
    /// </summary>
    [RelayCommand]
    public void DecreaseDuration()
    {
        DisplayDuration = Math.Max(500, DisplayDuration - 500);
    }

    /// <summary>
    /// Arka plan rengini seçer
    /// Requirement: 5.3
    /// </summary>
    [RelayCommand]
    public void SelectBackgroundColor()
    {
        // Renk seçici dialogu açılacak (View tarafında handle edilir)
    }

    /// <summary>
    /// Özel çerçeve seçer
    /// Requirement: 5.9
    /// </summary>
    [RelayCommand]
    public void SelectCustomBorder()
    {
        BorderStyle = BorderStyle.Custom;
        // Özel çerçeve seçici dialogu açılacak (View tarafında handle edilir)
    }

    /// <summary>
    /// Tüm ayarları varsayılana sıfırlar
    /// </summary>
    [RelayCommand]
    public void ResetAll()
    {
        if (SelectedContent != null)
        {
            SelectedContent.EntryEffect = new EffectConfig();
            SelectedContent.ExitEffect = new EffectConfig();
            SelectedContent.DurationMs = 3000;
            SelectedContent.ShowImmediately = true;
            
            LoadContentProperties(SelectedContent);
        }
    }
}
