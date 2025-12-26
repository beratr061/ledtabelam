using Avalonia.Controls;
using Avalonia.Interactivity;
using LEDTabelam.ViewModels;
using System;
using System.Reactive.Linq;

namespace LEDTabelam.Views;

public partial class PlaylistPanel : UserControl
{
    public PlaylistPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        var addButton = this.FindControl<Button>("AddMessageButton");
        var textBox = this.FindControl<TextBox>("NewMessageTextBox");
        
        if (addButton != null && textBox != null)
        {
            addButton.Click += (s, args) =>
            {
                if (DataContext is PlaylistViewModel vm && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    vm.AddItemCommand.Execute(textBox.Text).Subscribe();
                    textBox.Text = string.Empty;
                }
            };
            
            // Enter tuşu ile de ekle
            textBox.KeyDown += (s, args) =>
            {
                if (args.Key == Avalonia.Input.Key.Enter && DataContext is PlaylistViewModel vm && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    vm.AddItemCommand.Execute(textBox.Text).Subscribe();
                    textBox.Text = string.Empty;
                }
            };
        }
    }
}
