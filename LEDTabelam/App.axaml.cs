using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LEDTabelam.Models;
using LEDTabelam.Services;
using LEDTabelam.ViewModels;
using LEDTabelam.Views;

namespace LEDTabelam;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private GlobalExceptionHandler? _exceptionHandler;
    private ILogger<App>? _logger;

    /// <summary>
    /// DI Service Provider - uygulama genelinde erişim için
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Serilog yapılandırması
        ServiceCollectionExtensions.ConfigureSerilog();

        // DI Container kurulumu
        var services = new ServiceCollection();
        services.AddLedTabelamServices();
        services.AddSingleton<GlobalExceptionHandler>();
        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        // Logger al
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("Uygulama başlatılıyor...");

        // Global exception handler
        _exceptionHandler = _serviceProvider.GetRequiredService<GlobalExceptionHandler>();
        _exceptionHandler.Register();

        // Varsayılan font dosyasını oluştur (yoksa)
        EnsureDefaultFontExists();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Servisleri DI'dan al
            var profileManager = _serviceProvider.GetRequiredService<IProfileManager>();
            var slotManager = _serviceProvider.GetRequiredService<ISlotManager>();
            var zoneManager = _serviceProvider.GetRequiredService<IZoneManager>();
            var engineServices = _serviceProvider.GetRequiredService<IEngineServices>();
            var programSequencer = _serviceProvider.GetRequiredService<IProgramSequencer>();
            var assetLibrary = _serviceProvider.GetRequiredService<IAssetLibrary>();
            var previewRenderer = _serviceProvider.GetRequiredService<IPreviewRenderer>();
            var exportService = _serviceProvider.GetRequiredService<IExportService>();
            var fontLoader = _serviceProvider.GetRequiredService<IFontLoader>();

            // PreviewRenderer'a AssetLibrary'yi bağla
            if (previewRenderer is PreviewRenderer pr)
            {
                pr.SetAssetLibrary(assetLibrary);
            }

            // Profil kontrolü ve uygulama başlatma
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    var profile = await GetOrCreateProfileAsync(profileManager);
                    if (profile == null)
                    {
                        _logger?.LogWarning("Profil oluşturulamadı, uygulama kapatılıyor");
                        desktop.Shutdown();
                        return;
                    }

                    var mainWindowViewModel = new MainWindowViewModel(
                        profileManager,
                        slotManager,
                        zoneManager,
                        engineServices,
                        programSequencer);
                    
                    mainWindowViewModel.LoadProfile(profile);
                    mainWindowViewModel.UnifiedEditor.SetAssetLibrary(assetLibrary);

                    var mainWindow = new MainWindow
                    {
                        DataContext = mainWindowViewModel,
                    };
                    
                    if (exportService is ExportService es && fontLoader is FontLoader fl)
                    {
                        mainWindow.SetServices(es, fl);
                    }
                    
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();

                    _logger?.LogInformation("Uygulama başarıyla başlatıldı");
                }
                catch (Exception ex)
                {
                    _logger?.LogCritical(ex, "Uygulama başlatılırken kritik hata");
                    desktop.Shutdown();
                }
            });

            // Uygulama kapanırken cleanup
            desktop.ShutdownRequested += (s, e) =>
            {
                _logger?.LogInformation("Uygulama kapatılıyor...");
                _exceptionHandler?.Unregister();
                Serilog.Log.CloseAndFlush();
                
                if (_serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Profil varlığını kontrol eder, yoksa oluşturma dialogu gösterir
    /// </summary>
    private async System.Threading.Tasks.Task<Profile?> GetOrCreateProfileAsync(IProfileManager profileManager)
    {
        try
        {
            var profiles = await profileManager.GetAllProfilesAsync();
            
            if (profiles.Count > 0)
            {
                _logger?.LogDebug("Mevcut profil bulundu: {ProfileName}", profiles[0].Name);
                return profiles[0];
            }

            _logger?.LogInformation("Profil bulunamadı, yeni profil oluşturma dialogu açılıyor");

            // Profil yok, oluşturma/import dialogu göster
            var dialog = new ProfileSetupDialog();
            dialog.SetProfileManager(profileManager);
            
            var tcs = new System.Threading.Tasks.TaskCompletionSource<Profile?>();
            
            dialog.Closed += async (s, e) => 
            {
                try
                {
                    if (dialog.IsImported && dialog.ImportedProfile != null)
                    {
                        await profileManager.SaveProfileAsync(dialog.ImportedProfile);
                        _logger?.LogInformation("Profil import edildi: {ProfileName}", dialog.ImportedProfile.Name);
                        tcs.TrySetResult(dialog.ImportedProfile);
                    }
                    else if (!string.IsNullOrWhiteSpace(dialog.ResultProfileName))
                    {
                        var profile = new Profile
                        {
                            Name = dialog.ResultProfileName,
                            Settings = new DisplaySettings
                            {
                                PanelWidth = 160,
                                PanelHeight = 24,
                                ColorType = LedColorType.Amber,
                                Pitch = PixelPitch.P10,
                                Shape = PixelShape.Round,
                                Brightness = 100,
                                BackgroundDarkness = 100,
                                PixelSize = 8
                            },
                            FontName = "PixelFont8",
                            CreatedAt = DateTime.UtcNow,
                            ModifiedAt = DateTime.UtcNow
                        };
                        
                        await profileManager.SaveProfileAsync(profile);
                        _logger?.LogInformation("Yeni profil oluşturuldu: {ProfileName}", profile.Name);
                        tcs.TrySetResult(profile);
                    }
                    else
                    {
                        tcs.TrySetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Profil kaydetme hatası");
                    tcs.TrySetResult(null);
                }
            };
            
            dialog.Show();
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Profil yükleme/oluşturma hatası");
            return null;
        }
    }

    private void EnsureDefaultFontExists()
    {
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var fontsDir = Path.Combine(appDir, "Assets", "Fonts");
            var fontPngPath = Path.Combine(fontsDir, "PixelFont8.png");

            if (!Directory.Exists(fontsDir))
            {
                Directory.CreateDirectory(fontsDir);
            }

            if (!File.Exists(fontPngPath))
            {
                FontImageGenerator.SaveFontImage(fontPngPath);
                _logger?.LogDebug("Varsayılan font dosyası oluşturuldu: {Path}", fontPngPath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Varsayılan font dosyası oluşturulamadı");
        }
    }
}
