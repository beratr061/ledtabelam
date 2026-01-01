using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Ekran düğümü - Bir veya daha fazla program içerir
/// </summary>
public partial class ScreenNode : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Ekran1";

    [ObservableProperty]
    private int _width = 128;

    [ObservableProperty]
    private int _height = 32;

    [ObservableProperty]
    private ObservableCollection<ProgramNode> _programs = new();

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;
}
