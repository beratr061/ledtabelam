using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Program düğümü - Bir veya daha fazla içerik öğesi içerir
/// </summary>
public partial class ProgramNode : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Program1";

    [ObservableProperty]
    private ObservableCollection<ContentItem> _contents = new();

    [ObservableProperty]
    private bool _isLoop = true;

    [ObservableProperty]
    private TransitionType _transitionType = TransitionType.None;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;
}
