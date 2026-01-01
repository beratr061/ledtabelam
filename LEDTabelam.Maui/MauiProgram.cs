using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LEDTabelam.Maui.Services;
using LEDTabelam.Maui.ViewModels;

namespace LEDTabelam.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		try
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseSkiaSharp()
				.UseMauiCommunityToolkit()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

			// Register Platform Services (CommunityToolkit.Maui)
			builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);

			// Register Services
			builder.Services.AddSingleton<ILedRenderer, LedRenderer>();
			builder.Services.AddSingleton<IFontLoader, FontLoader>();
			builder.Services.AddSingleton<IProjectManager, ProjectManager>();
			builder.Services.AddSingleton<IContentManager, ContentManager>();
			builder.Services.AddSingleton<IEffectService, EffectService>();
			builder.Services.AddSingleton<IExportService, ExportService>();
			builder.Services.AddSingleton<IKeyboardShortcutService, KeyboardShortcutService>();

			// Register ViewModels
			builder.Services.AddTransient<TreeViewModel>();
			builder.Services.AddTransient<PreviewViewModel>();
			builder.Services.AddTransient<PropertiesViewModel>();
			builder.Services.AddTransient<EditorViewModel>();
			builder.Services.AddTransient<MainViewModel>();

			// Register Pages
			builder.Services.AddTransient<MainPage>();

#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"MauiProgram Error: {ex}");
			throw;
		}
	}
}
