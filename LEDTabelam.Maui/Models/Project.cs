using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Proje model sınıfı - Tüm ekran, program ve içerikleri içerir
/// </summary>
public partial class Project : ObservableObject
{
    [ObservableProperty]
    private string _name = "Yeni Proje";

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ScreenNode> _screens = new();

    [ObservableProperty]
    private DisplaySettings _globalSettings = new();

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime _modifiedAt = DateTime.Now;

    /// <summary>
    /// Projeyi değiştirildi olarak işaretler
    /// </summary>
    public void MarkAsModified()
    {
        ModifiedAt = DateTime.Now;
    }
}
