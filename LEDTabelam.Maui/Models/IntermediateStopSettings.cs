using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Ara durak ayarları - bir metin öğesinin ara durak konfigürasyonu
/// </summary>
public partial class IntermediateStopSettings : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = false;

    [ObservableProperty]
    private ObservableCollection<IntermediateStop> _stops = new();

    [ObservableProperty]
    private double _durationSeconds = 2.0;

    [ObservableProperty]
    private StopAnimationType _animation = StopAnimationType.Direct;

    [ObservableProperty]
    private int _animationDurationMs = 200;

    [ObservableProperty]
    private bool _autoCalculateDuration = false;

    partial void OnDurationSecondsChanging(double value)
    {
        if (value < 0.5) _durationSeconds = 0.5;
        else if (value > 10.0) _durationSeconds = 10.0;
    }

    partial void OnAnimationDurationMsChanging(int value)
    {
        if (value < 100) _animationDurationMs = 100;
        else if (value > 500) _animationDurationMs = 500;
    }
}
