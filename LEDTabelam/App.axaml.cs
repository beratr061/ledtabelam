using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LEDTabelam.Models;
using LEDTabelam.Services;
using LEDTabelam.ViewModels;
using LEDTabelam.Views;

namespace LEDTabelam;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Varsayılan font dosyasını oluştur (yoksa)
        EnsureDefaultFontExists();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Servisleri oluştur
            var profileManager = new ProfileManager();
            var slotManager = new SlotManager();
            var fontLoader = new FontLoader();
            var ledRenderer = new LedRenderer();
            var animationService = new AnimationService();
            var exportService = new ExportService();
            var zoneManager = new ZoneManager();
            var multiLineTextRenderer = new MultiLineTextRenderer(fontLoader);
            var previewRenderer = new PreviewRenderer(fontLoader, multiLineTextRenderer);
            var programSequencer = new ProgramSequencer();
            
            // SVG Renderer ve Asset Library oluştur
            var svgRenderer = new SvgRenderer();
            var assetLibrary = new AssetLibrary(svgRenderer);
            
            // PreviewRenderer'a AssetLibrary'yi bağla
            previewRenderer.SetAssetLibrary(assetLibrary);

            // Engine servisleri Facade'ı oluştur
            var engineServices = new EngineServices(
                fontLoader,
                ledRenderer,
                animationService,
                exportService,
                multiLineTextRenderer,
                previewRenderer);

            // Profil kontrolü ve uygulama başlatma
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var profile = await GetOrCreateProfileAsync(profileManager);
                if (profile == null)
                {
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
                
                mainWindow.SetServices(exportService, fontLoader);
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Profil varlığını kontrol eder, yoksa oluşturma dialogu gösterir
    /// </summary>
    private static async System.Threading.Tasks.Task<Profile?> GetOrCreateProfileAsync(IProfileManager profileManager)
    {
        var profiles = await profileManager.GetAllProfilesAsync();
        
        if (profiles.Count > 0)
        {
            return profiles[0];
        }

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
                    // Import edilen profili kaydet
                    await profileManager.SaveProfileAsync(dialog.ImportedProfile);
                    tcs.TrySetResult(dialog.ImportedProfile);
                }
                else if (!string.IsNullOrWhiteSpace(dialog.ResultProfileName))
                {
                    // Yeni profil oluştur
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
                    tcs.TrySetResult(profile);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Profil kaydetme hatası: {ex.Message}");
                tcs.TrySetResult(null);
            }
        };
        
        dialog.Show();
        return await tcs.Task;
    }

    private static void EnsureDefaultFontExists()
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
            }
        }
        catch (Exception)
        {
            // Sessizce yoksay
        }
    }
}
