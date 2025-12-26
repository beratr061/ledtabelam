using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LEDTabelam.ViewModels;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace LEDTabelam.Views;

/// <summary>
/// Kontrol paneli - Font yükleme ve ayarlar
/// Requirements: 4.2, 12.3
/// </summary>
public partial class ControlPanel : UserControl
{
    public ControlPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Font yükleme butonuna event handler ekle
        var loadFontButton = this.FindControl<Button>("LoadFontButton");
        if (loadFontButton != null)
        {
            loadFontButton.Click += OnLoadFontClick;
        }
    }

    /// <summary>
    /// Font yükleme - OpenFileDialog ile
    /// Requirements: 4.2
    /// </summary>
    private async void OnLoadFontClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Platform native OpenFileDialog
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Font Dosyası Seç",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Font Dosyaları")
                {
                    Patterns = new[] { "*.fnt", "*.json" }
                },
                new FilePickerFileType("BMFont XML")
                {
                    Patterns = new[] { "*.fnt" }
                },
                new FilePickerFileType("JSON Font")
                {
                    Patterns = new[] { "*.json" }
                },
                new FilePickerFileType("Tüm Dosyalar")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var path = file.Path.LocalPath;
            
            if (DataContext is ControlPanelViewModel vm)
            {
                try
                {
                    await Task.Run(() => vm.LoadFontCommand.Execute(path).Subscribe());
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync(topLevel, $"Font yüklenirken hata oluştu:\n{ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Hata mesajı göster
    /// </summary>
    private async Task ShowErrorAsync(TopLevel topLevel, string message)
    {
        var messageBox = new Window
        {
            Title = "Hata",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Avalonia.Thickness(20),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
        await messageBox.ShowDialog(topLevel as Window ?? throw new InvalidOperationException());
    }
}
