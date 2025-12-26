using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LEDTabelam.ViewModels;
using System;
using System.Reactive.Linq;

namespace LEDTabelam.Views;

public partial class SlotEditor : UserControl
{
    public SlotEditor()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        var selectIconButton = this.FindControl<Button>("SelectIconButton");
        if (selectIconButton != null)
        {
            selectIconButton.Click += OnSelectIconClick;
        }
    }

    private async void OnSelectIconClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "İkon Dosyası Seç",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Resim Dosyaları")
                {
                    Patterns = new[] { "*.svg", "*.png", "*.jpg", "*.jpeg" }
                },
                new FilePickerFileType("SVG")
                {
                    Patterns = new[] { "*.svg" }
                },
                new FilePickerFileType("PNG")
                {
                    Patterns = new[] { "*.png" }
                }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var path = file.Path.LocalPath;
            
            if (DataContext is SlotEditorViewModel vm)
            {
                vm.SelectIconCommand.Execute(path).Subscribe();
            }
        }
    }
}
