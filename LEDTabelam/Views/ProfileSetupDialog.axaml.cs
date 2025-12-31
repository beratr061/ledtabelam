using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.Views;

public partial class ProfileSetupDialog : Window
{
    public string? ResultProfileName { get; private set; }
    public Profile? ImportedProfile { get; private set; }
    public bool IsImported { get; private set; }

    private IProfileManager? _profileManager;

    public ProfileSetupDialog()
    {
        InitializeComponent();
        ProfileNameTextBox.AttachedToVisualTree += (s, e) => ProfileNameTextBox.Focus();
        ProfileNameTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                OnCreateClick(s, new RoutedEventArgs());
            }
        };
    }

    public void SetProfileManager(IProfileManager profileManager)
    {
        _profileManager = profileManager;
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        var name = ProfileNameTextBox.Text?.Trim();
        
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorText.Text = "Profil adı boş olamaz.";
            ErrorText.IsVisible = true;
            return;
        }

        if (name.Length < 2)
        {
            ErrorText.Text = "Profil adı en az 2 karakter olmalıdır.";
            ErrorText.IsVisible = true;
            return;
        }

        ResultProfileName = name;
        IsImported = false;
        Close(true);
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Profil Dosyası Seç",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("LED Tabelam Profil")
                    {
                        Patterns = new[] { "*.ledprofile", "*.json" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var filePath = files[0].Path.LocalPath;
                await ImportProfileFromFileAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"İçe aktarma hatası: {ex.Message}";
            ErrorText.IsVisible = true;
        }
    }

    private async Task ImportProfileFromFileAsync(string filePath)
    {
        try
        {
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new ColorJsonConverter());

            var profile = System.Text.Json.JsonSerializer.Deserialize<Profile>(json, options);
            
            if (profile == null || string.IsNullOrEmpty(profile.Name))
            {
                ErrorText.Text = "Geçersiz profil dosyası.";
                ErrorText.IsVisible = true;
                return;
            }

            ImportedProfile = profile;
            IsImported = true;
            Close(true);
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Dosya okunamadı: {ex.Message}";
            ErrorText.IsVisible = true;
        }
    }
}
