using Microsoft.Extensions.DependencyInjection;

namespace LEDTabelam.Maui;

public partial class App : Application
{
	private readonly IServiceProvider _serviceProvider;

	public App(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
		
		try
		{
			InitializeComponent();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"App InitializeComponent Error: {ex}");
			throw;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			// Get MainPage from DI container to ensure proper dependency injection
			var mainPage = _serviceProvider.GetRequiredService<MainPage>();
			return new Window(mainPage) { Title = "HD2020 - LEDTabelam" };
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"CreateWindow Error: {ex}");
			System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().Name}");
			System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
			
			// Show error details in the simple page
			var errorPage = new ContentPage
			{
				Title = "LEDTabelam - Hata",
				BackgroundColor = Color.FromArgb("#1E1E1E"),
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 20,
						Spacing = 10,
						Children =
						{
							new Label { Text = "Uygulama Yüklenirken Hata Oluştu", FontSize = 24, TextColor = Colors.Red },
							new Label { Text = ex.Message, FontSize = 14, TextColor = Colors.White },
							new Label { Text = "Inner Exception:", FontSize = 12, TextColor = Colors.Gray },
							new Label { Text = ex.InnerException?.Message ?? "Yok", FontSize = 12, TextColor = Colors.Orange },
							new Label { Text = "Stack Trace:", FontSize = 12, TextColor = Colors.Gray },
							new Label { Text = ex.StackTrace ?? "Yok", FontSize = 10, TextColor = Colors.LightGray }
						}
					}
				}
			};
			return new Window(errorPage) { Title = "HD2020 - LEDTabelam (Error)" };
		}
	}
}