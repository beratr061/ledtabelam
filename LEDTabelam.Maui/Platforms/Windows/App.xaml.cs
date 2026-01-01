using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using LEDTabelam.Maui.Helpers;
using LEDTabelam.Maui.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LEDTabelam.Maui.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// Requirements: 15.1-15.8 (Keyboard shortcuts)
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	/// <summary>
	/// Called when the main window is created
	/// Sets up keyboard event handling
	/// </summary>
	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		base.OnLaunched(args);

		// Delay keyboard handler setup to ensure window is ready
		try
		{
			if (Application.Windows.Count > 0)
			{
				var window = Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
				if (window?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
				{
					rootElement.KeyDown += OnKeyDown;
				}
			}
		}
		catch
		{
			// Ignore keyboard setup errors - app should still work
		}
	}

	/// <summary>
	/// Handles keyboard events at the application level
	/// Requirements: 15.1-15.8
	/// </summary>
	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		// Get modifier states
		var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
		var shiftState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift);
		var altState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu);

		bool isCtrlPressed = ctrlState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
		bool isShiftPressed = shiftState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
		bool isAltPressed = altState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

		// Build modifiers
		var modifiers = KeyModifiers.None;
		if (isCtrlPressed) modifiers |= KeyModifiers.Control;
		if (isShiftPressed) modifiers |= KeyModifiers.Shift;
		if (isAltPressed) modifiers |= KeyModifiers.Alt;

		// Convert virtual key to string
		var key = KeyboardHelper.NormalizeKey((int)e.Key);

		// Try to handle the key press
		if (KeyboardHelper.HandleKeyPress(key, modifiers))
		{
			e.Handled = true;
		}
	}
}
