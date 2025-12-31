using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LEDTabelam.ViewModels;
using LEDTabelam.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace LEDTabelam.Views;

/// <summary>
/// Ana pencere - Keyboard shortcuts ve file dialog'ları yönetir
/// Requirements: 13.1, 13.2, 13.3, 13.4, 4.2, 7.1, 12.3
/// </summary>
public partial class MainWindow : Window
{
    private IExportService? _exportService;
    private IFontLoader? _fontLoader;

    public MainWindow()
    {
        InitializeComponent();
        
        // KeyDown event'ini dinle (Space tuşu için özel işlem)
        KeyDown += OnKeyDown;
        
        // Profil yönetimi butonunu bağla
        var manageProfilesButton = this.FindControl<Button>("ManageProfilesButton");
        if (manageProfilesButton != null)
        {
            manageProfilesButton.Click += OnManageProfilesClick;
        }
    }

    /// <summary>
    /// Servisleri enjekte et (App.axaml.cs'den çağrılır)
    /// </summary>
    public void SetServices(IExportService exportService, IFontLoader fontLoader)
    {
        _exportService = exportService;
        _fontLoader = fontLoader;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // ViewModel'deki komutları file dialog'larla bağla
        if (DataContext is MainWindowViewModel vm)
        {
            // SavePngCommand'ı file dialog ile bağla
            vm.SavePngCommand.Subscribe(async _ => await SavePngWithDialogAsync());
            
            // LoadFontCommand'ı file dialog ile bağla
            vm.LoadFontCommand.Subscribe(async _ => await LoadFontWithDialogAsync());
            
            // Başlangıç verilerini yükle (fontlar ve profiller)
            try
            {
                await vm.ControlPanel.InitializeAsync();
                vm.StatusMessage = "Hazır";
            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"Başlatma hatası: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Keyboard event handler - Space tuşu için özel işlem
    /// Requirements: 13.3
    /// </summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Space tuşu TextBox içinde değilse animasyon toggle
        if (e.Key == Key.Space)
        {
            // Eğer focus TextBox'ta ise, Space tuşunu engelleme
            if (FocusManager?.GetFocusedElement() is TextBox)
            {
                return;
            }
            
            // Animasyon toggle
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ToggleAnimationCommand.Execute().Subscribe();
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// PNG kaydetme - SaveFileDialog ile
    /// Requirements: 7.1, 13.1
    /// </summary>
    private async Task SavePngWithDialogAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
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

        if (file != null && DataContext is MainWindowViewModel vm)
        {
            try
            {
                var path = file.Path.LocalPath;
                var bitmap = vm.Preview.GetCurrentBitmap();
                
                if (bitmap != null && _exportService != null)
                {
                    await _exportService.ExportPngAsync(bitmap, path, true);
                    vm.StatusMessage = $"PNG kaydedildi: {path}";
                }
                else
                {
                    vm.StatusMessage = "Kaydedilecek görüntü bulunamadı";
                }
            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"PNG kaydetme hatası: {ex.Message}";
                await ShowErrorAsync($"PNG kaydedilirken hata oluştu:\n{ex.Message}");
            }
        }
    }

    /// <summary>
    /// Font yükleme - OpenFileDialog ile
    /// Requirements: 4.2, 13.2
    /// </summary>
    private async Task LoadFontWithDialogAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
                }
            }
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var filePath = files[0].Path.LocalPath;
            
            try
            {
                vm.ControlPanel.LoadFontCommand.Execute(filePath).Subscribe();
                vm.StatusMessage = "Font yüklendi";
            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"Font yükleme hatası: {ex.Message}";
                await ShowErrorAsync($"Font yüklenirken hata oluştu:\n{ex.Message}");
            }
        }
    }

    /// <summary>
    /// Hata mesajı göster
    /// </summary>
    private async Task ShowErrorAsync(string message)
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
        await messageBox.ShowDialog(this);
    }

    /// <summary>
    /// Profil yönetimi dialog'unu aç
    /// </summary>
    private async void OnManageProfilesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var dialog = new ProfileManagerDialog(vm.ProfileManager);
            await dialog.ShowDialog(this);
            
            // Seçilen profili yükle
            if (dialog.ProfileToLoad != null)
            {
                // LoadProfile metodu hem CurrentProfile hem ControlPanel.SelectedProfile'ı günceller
                vm.LoadProfile(dialog.ProfileToLoad);
                vm.StatusMessage = $"'{dialog.ProfileToLoad.Name}' profili yüklendi";
            }
            // Dialog kapandıktan sonra profilleri yeniden yükle (değişiklik varsa)
            else if (dialog.ProfilesChanged)
            {
                await vm.ControlPanel.LoadProfilesAsync();
            }
        }
    }
}