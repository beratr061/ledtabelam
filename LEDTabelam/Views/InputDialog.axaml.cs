using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LEDTabelam.Views;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
    }

    public InputDialog(string title, string message, string defaultValue = "")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        InputTextBox.Text = defaultValue;
        InputTextBox.AttachedToVisualTree += (s, e) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
        InputTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                OnOkClick(s, new RoutedEventArgs());
            else if (e.Key == Avalonia.Input.Key.Escape)
                OnCancelClick(s, new RoutedEventArgs());
        };
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(InputTextBox.Text?.Trim());
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
