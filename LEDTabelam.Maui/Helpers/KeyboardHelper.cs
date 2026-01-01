using LEDTabelam.Maui.Services;

namespace LEDTabelam.Maui.Helpers;

/// <summary>
/// Platform-agnostic keyboard helper
/// Provides keyboard event handling for the application
/// Requirements: 15.1-15.8
/// </summary>
public static class KeyboardHelper
{
    private static IKeyboardShortcutService? _shortcutService;
    private static MainPage? _mainPage;

    /// <summary>
    /// Initializes the keyboard helper with the shortcut service
    /// </summary>
    public static void Initialize(IKeyboardShortcutService shortcutService, MainPage mainPage)
    {
        _shortcutService = shortcutService;
        _mainPage = mainPage;
    }

    /// <summary>
    /// Handles a key press event
    /// </summary>
    /// <param name="key">The key that was pressed</param>
    /// <param name="modifiers">The modifier keys that were held</param>
    /// <returns>True if the key was handled</returns>
    public static bool HandleKeyPress(string key, KeyModifiers modifiers)
    {
        if (_shortcutService == null)
            return false;

        return _shortcutService.HandleKeyPress(key, modifiers);
    }

    /// <summary>
    /// Converts platform-specific key codes to string representation
    /// </summary>
    public static string NormalizeKey(int virtualKey)
    {
        // Common virtual key codes
        return virtualKey switch
        {
            // Letters A-Z (65-90)
            >= 65 and <= 90 => ((char)virtualKey).ToString(),
            
            // Numbers 0-9 (48-57)
            >= 48 and <= 57 => ((char)virtualKey).ToString(),
            
            // Function keys
            112 => "F1",
            113 => "F2",
            114 => "F3",
            115 => "F4",
            116 => "F5",
            117 => "F6",
            118 => "F7",
            119 => "F8",
            120 => "F9",
            121 => "F10",
            122 => "F11",
            123 => "F12",
            
            // Special keys
            46 => "DELETE",
            8 => "BACKSPACE",
            13 => "ENTER",
            27 => "ESCAPE",
            32 => "SPACE",
            9 => "TAB",
            
            // Arrow keys
            37 => "LEFT",
            38 => "UP",
            39 => "RIGHT",
            40 => "DOWN",
            
            // Other
            _ => $"KEY_{virtualKey}"
        };
    }
}
