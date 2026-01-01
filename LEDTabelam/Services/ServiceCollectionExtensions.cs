using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using LEDTabelam.ViewModels;
using System;

namespace LEDTabelam.Services;

/// <summary>
/// DI container için servis kayıt extension metodları
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Tüm uygulama servislerini DI container'a kaydeder
    /// </summary>
    public static IServiceCollection AddLedTabelamServices(this IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Core Services - Singleton (uygulama boyunca tek instance)
        services.AddSingleton<IProfileManager, ProfileManager>();
        services.AddSingleton<ISlotManager, SlotManager>();
        services.AddSingleton<IFontLoader, FontLoader>();
        services.AddSingleton<ILedRenderer, LedRenderer>();
        services.AddSingleton<IAnimationService, AnimationService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IZoneManager, ZoneManager>();
        services.AddSingleton<ISvgRenderer, SvgRenderer>();
        services.AddSingleton<IProgramSequencer, ProgramSequencer>();

        // Scoped Services - FontLoader'a bağımlı
        services.AddSingleton<IMultiLineTextRenderer>(sp =>
            new MultiLineTextRenderer(sp.GetRequiredService<IFontLoader>()));

        services.AddSingleton<IPreviewRenderer>(sp =>
            new PreviewRenderer(
                sp.GetRequiredService<IFontLoader>(),
                sp.GetRequiredService<IMultiLineTextRenderer>()));

        // Asset Library
        services.AddSingleton<IAssetLibrary>(sp =>
            new AssetLibrary(sp.GetRequiredService<ISvgRenderer>()));

        // Engine Services Facade
        services.AddSingleton<IEngineServices>(sp =>
            new EngineServices(
                sp.GetRequiredService<IFontLoader>(),
                sp.GetRequiredService<ILedRenderer>(),
                sp.GetRequiredService<IAnimationService>(),
                sp.GetRequiredService<IExportService>(),
                sp.GetRequiredService<IMultiLineTextRenderer>(),
                sp.GetRequiredService<IPreviewRenderer>()));

        // ViewModels - Transient (her istek için yeni instance)
        services.AddTransient<MainWindowViewModel>();

        return services;
    }

    /// <summary>
    /// Serilog yapılandırmasını oluşturur
    /// </summary>
    public static void ConfigureSerilog()
    {
        var logPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LEDTabelam", "logs", "app-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
#if DEBUG
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
#endif
            .CreateLogger();
    }
}
