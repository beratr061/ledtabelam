using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LEDTabelam.ViewModels;
using LEDTabelam.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace LEDTabelam.Views;

/// <summary>
/// Export paneli - PNG, GIF, WebP dışa aktarma
/// Requirements: 7.1, 7.5, 7.6, 12.3
/// </summary>
public partial class ExportPanel : UserControl
{
    private IExportService? _exportService;
    
    public ExportPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// ExportService'i enjekte et
    /// </summary>
    public void SetExportService(IExportService exportService)
    {
        _exportService = exportService;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        var savePngButton = this.FindControl<Button>("SavePngButton");
        var saveGifButton = this.FindControl<Button>("SaveGifButton");
        var saveWebPButton = this.FindControl<Button>("SaveWebPButton");
        
        if (savePngButton != null)
            savePngButton.Click += OnSavePngClick;
        if (saveGifButton != null)
            saveGifButton.Click += OnSaveGifClick;
        if (saveWebPButton != null)
            saveWebPButton.Click += OnSaveWebPClick;
    }

    /// <summary>
    /// PNG kaydetme - SaveFileDialog ile
    /// Requirements: 7.1
    /// </summary>
    private async void OnSavePngClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "PNG Olarak Kaydet",
            DefaultExtension = "png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Resim")
                {
                    Patterns = new[] { "*.png" }
                }
            },
            SuggestedFileName = "led_tabela"
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            var mainVm = GetMainWindowViewModel();
            
            if (mainVm != null)
            {
                try
                {
                    var useZoom = this.FindControl<CheckBox>("PngUseZoomCheckBox")?.IsChecked ?? true;
                    var bitmap = useZoom 
                        ? mainVm.Preview.GetCurrentBitmap() 
                        : mainVm.Preview.GetOriginalResolutionBitmap();
                    
                    if (bitmap != null && _exportService != null)
                    {
                        await _exportService.ExportPngAsync(bitmap, path, useZoom, mainVm.Preview.ZoomLevel);
                        mainVm.StatusMessage = $"PNG kaydedildi: {path}";
                    }
                    else
                    {
                        mainVm.StatusMessage = "Kaydedilecek görüntü bulunamadı";
                    }
                }
                catch (Exception ex)
                {
                    mainVm.StatusMessage = $"PNG kaydetme hatası: {ex.Message}";
                    await ShowErrorAsync(topLevel, $"PNG kaydedilirken hata oluştu:\n{ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// GIF kaydetme - SaveFileDialog ile
    /// Requirements: 7.5
    /// </summary>
    private async void OnSaveGifClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "GIF Olarak Kaydet",
            DefaultExtension = "gif",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("GIF Animasyon")
                {
                    Patterns = new[] { "*.gif" }
                }
            },
            SuggestedFileName = "led_tabela_animation"
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            var mainVm = GetMainWindowViewModel();
            
            if (mainVm != null)
            {
                try
                {
                    var fps = (int)(this.FindControl<NumericUpDown>("GifFpsUpDown")?.Value ?? 15);
                    var duration = (int)(this.FindControl<NumericUpDown>("GifDurationUpDown")?.Value ?? 3);
                    
                    // Animasyon frame'lerini oluştur
                    var frames = await GenerateAnimationFramesAsync(mainVm, fps, duration);
                    
                    if (frames.Count > 0 && _exportService != null)
                    {
                        await _exportService.ExportGifAsync(frames, path, fps);
                        mainVm.StatusMessage = $"GIF kaydedildi: {path}";
                        
                        // Frame'leri temizle
                        foreach (var frame in frames)
                        {
                            frame.Dispose();
                        }
                    }
                    else
                    {
                        mainVm.StatusMessage = "Animasyon frame'leri oluşturulamadı";
                    }
                }
                catch (Exception ex)
                {
                    mainVm.StatusMessage = $"GIF kaydetme hatası: {ex.Message}";
                    await ShowErrorAsync(topLevel, $"GIF kaydedilirken hata oluştu:\n{ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// WebP kaydetme - SaveFileDialog ile
    /// Requirements: 7.6
    /// </summary>
    private async void OnSaveWebPClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "WebP Olarak Kaydet",
            DefaultExtension = "webp",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("WebP Animasyon")
                {
                    Patterns = new[] { "*.webp" }
                }
            },
            SuggestedFileName = "led_tabela_animation"
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            var mainVm = GetMainWindowViewModel();
            
            if (mainVm != null)
            {
                try
                {
                    var fps = (int)(this.FindControl<NumericUpDown>("WebPFpsUpDown")?.Value ?? 15);
                    var duration = (int)(this.FindControl<NumericUpDown>("WebPDurationUpDown")?.Value ?? 3);
                    
                    // Animasyon frame'lerini oluştur
                    var frames = await GenerateAnimationFramesAsync(mainVm, fps, duration);
                    
                    if (frames.Count > 0 && _exportService != null)
                    {
                        await _exportService.ExportWebPAsync(frames, path, fps);
                        mainVm.StatusMessage = $"WebP kaydedildi: {path}";
                        
                        // Frame'leri temizle
                        foreach (var frame in frames)
                        {
                            frame.Dispose();
                        }
                    }
                    else
                    {
                        mainVm.StatusMessage = "Animasyon frame'leri oluşturulamadı";
                    }
                }
                catch (Exception ex)
                {
                    mainVm.StatusMessage = $"WebP kaydetme hatası: {ex.Message}";
                    await ShowErrorAsync(topLevel, $"WebP kaydedilirken hata oluştu:\n{ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// MainWindowViewModel'i al
    /// </summary>
    private MainWindowViewModel? GetMainWindowViewModel()
    {
        // DataContext doğrudan MainWindowViewModel olabilir
        if (DataContext is MainWindowViewModel mainVm)
            return mainVm;
        
        // Veya parent window'dan al
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window window && window.DataContext is MainWindowViewModel windowVm)
            return windowVm;
        
        return null;
    }

    /// <summary>
    /// Animasyon frame'lerini oluştur
    /// </summary>
    private async Task<List<SKBitmap>> GenerateAnimationFramesAsync(MainWindowViewModel vm, int fps, int durationSeconds)
    {
        var frames = new List<SKBitmap>();
        var totalFrames = fps * durationSeconds;
        
        // Mevcut bitmap'i al ve kopyala
        var currentBitmap = vm.Preview.GetCurrentBitmap();
        if (currentBitmap == null)
            return frames;
        
        // Basit implementasyon: aynı frame'i tekrarla
        // Gerçek animasyon için scroll offset'i değiştirmek gerekir
        for (int i = 0; i < totalFrames; i++)
        {
            var frameCopy = currentBitmap.Copy();
            if (frameCopy != null)
            {
                frames.Add(frameCopy);
            }
            
            // UI'ın donmaması için kısa bekleme
            if (i % 10 == 0)
            {
                await Task.Delay(1);
            }
        }
        
        return frames;
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
