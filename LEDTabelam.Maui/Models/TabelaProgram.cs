using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Tabela programı - bir içerik konfigürasyonu
/// </summary>
public partial class TabelaProgram : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = "Program 1";

    [ObservableProperty]
    private int _durationSeconds = 5;

    [ObservableProperty]
    private ProgramTransitionType _transition = ProgramTransitionType.Direct;

    [ObservableProperty]
    private int _transitionDurationMs = 300;

    [ObservableProperty]
    private ObservableCollection<TabelaItem> _items = new();

    [ObservableProperty]
    private bool _isActive = false;

    partial void OnDurationSecondsChanging(int value)
    {
        if (value < 1) _durationSeconds = 1;
        else if (value > 60) _durationSeconds = 60;
    }

    partial void OnTransitionDurationMsChanging(int value)
    {
        if (value < 200) _transitionDurationMs = 200;
        else if (value > 1000) _transitionDurationMs = 1000;
    }
}
