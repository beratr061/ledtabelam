using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Ara durak ayarları - bir metin öğesinin ara durak konfigürasyonu
/// Requirements: 5.1, 5.2, 6.2, 6.5
/// </summary>
public class IntermediateStopSettings : ReactiveObject
{
    private bool _isEnabled = false;
    private ObservableCollection<IntermediateStop> _stops = new();
    private double _durationSeconds = 2.0;
    private StopAnimationType _animation = StopAnimationType.Direct;
    private int _animationDurationMs = 200;
    private bool _autoCalculateDuration = false;

    /// <summary>
    /// Ara durak sistemi aktif mi
    /// Varsayılan: false
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    /// <summary>
    /// Ara durak listesi
    /// </summary>
    public ObservableCollection<IntermediateStop> Stops
    {
        get => _stops;
        set => this.RaiseAndSetIfChanged(ref _stops, value ?? new ObservableCollection<IntermediateStop>());
    }

    /// <summary>
    /// Her durağın ekranda kalma süresi (saniye)
    /// Varsayılan: 2.0 saniye
    /// Geçerli aralık: 0.5 - 10 saniye
    /// Requirements: 5.2, 5.3
    /// </summary>
    public double DurationSeconds
    {
        get => _durationSeconds;
        set => this.RaiseAndSetIfChanged(ref _durationSeconds, Math.Clamp(value, 0.5, 10.0));
    }

    /// <summary>
    /// Duraklar arası geçiş animasyonu
    /// Varsayılan: Direct (kesme)
    /// Requirements: 6.2
    /// </summary>
    public StopAnimationType Animation
    {
        get => _animation;
        set => this.RaiseAndSetIfChanged(ref _animation, value);
    }

    /// <summary>
    /// Animasyon süresi (milisaniye)
    /// Varsayılan: 200ms
    /// Geçerli aralık: 100 - 500ms
    /// Requirements: 6.4, 6.5
    /// </summary>
    public int AnimationDurationMs
    {
        get => _animationDurationMs;
        set => this.RaiseAndSetIfChanged(ref _animationDurationMs, Math.Clamp(value, 100, 500));
    }

    /// <summary>
    /// Otomatik süre hesaplama aktif mi
    /// true ise: durak_süresi = program_süresi / durak_sayısı
    /// Requirements: 8.4
    /// </summary>
    public bool AutoCalculateDuration
    {
        get => _autoCalculateDuration;
        set => this.RaiseAndSetIfChanged(ref _autoCalculateDuration, value);
    }
}
