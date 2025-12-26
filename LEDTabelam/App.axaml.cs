using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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

            // Engine servisleri Facade'ı oluştur
            var engineServices = new EngineServices(
                fontLoader,
                ledRenderer,
                animationService,
                exportService,
                multiLineTextRenderer,
                previewRenderer);

            // Varsayılan profili oluştur (yoksa)
            _ = profileManager.GetOrCreateDefaultProfileAsync();

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(
                    profileManager,
                    slotManager,
                    zoneManager,
                    engineServices),
            };
            
            // Servisleri MainWindow'a enjekte et (keyboard shortcuts için)
            mainWindow.SetServices(exportService, fontLoader);
            
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Varsayılan font PNG dosyasının varlığını kontrol eder ve yoksa oluşturur
    /// Requirements: 4.13
    /// </summary>
    private static void EnsureDefaultFontExists()
    {
        try
        {
            // Uygulama dizinini bul
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var fontsDir = Path.Combine(appDir, "Assets", "Fonts");
            var fontPngPath = Path.Combine(fontsDir, "PixelFont8.png");

            // Fonts dizini yoksa oluştur
            if (!Directory.Exists(fontsDir))
            {
                Directory.CreateDirectory(fontsDir);
            }

            // Font PNG dosyası yoksa oluştur
            if (!File.Exists(fontPngPath))
            {
                FontImageGenerator.SaveFontImage(fontPngPath);
            }
        }
        catch (Exception)
        {
            // Font oluşturma hatası sessizce yoksayılır
            // Uygulama font olmadan da çalışabilir
        }
    }
}