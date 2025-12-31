using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.Views;

public partial class ProfileManagerDialog : Window
{
    private readonly IProfileManager _profileManager;
    public ObservableCollection<Profile> Profiles { get; } = new();
    public Profile? SelectedProfile => ProfileListBox.SelectedItem as Profile;
    public bool ProfilesChanged { get; private set; }
    
    /// <summary>
    /// Yüklenecek profil - dialog kapandıktan sonra bu profil ana uygulamaya yüklenir
    /// </summary>
    public Profile? ProfileToLoad { get; private set; }

    public ProfileManagerDialog() : this(null!)
    {
    }

    public ProfileManagerDialog(IProfileManager profileManager)
    {
        InitializeComponent();
        _profileManager = profileManager;
        ProfileListBox.ItemsSource = Profiles;
        Loaded += async (s, e) => await LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        Profiles.Clear();
        if (_profileManager == null) return;
        
        var profiles = await _profileManager.GetAllProfilesAsync();
        foreach (var profile in profiles)
        {
            Profiles.Add(profile);
        }
        
        // Otomatik seçim yapma - kullanıcı kendisi seçsin
    }

    private async void OnRenameClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedProfile == null) return;

        var dialog = new InputDialog("Profili Yeniden Adlandır", "Yeni profil adı:", SelectedProfile.Name);
        var result = await dialog.ShowDialog<string?>(this);
        
        if (!string.IsNullOrWhiteSpace(result) && result != SelectedProfile.Name)
        {
            if (!await _profileManager.IsProfileNameAvailableAsync(result))
            {
                await ShowMessageAsync("Hata", "Bu isimde bir profil zaten var.");
                return;
            }

            var oldName = SelectedProfile.Name;
            await _profileManager.DeleteProfileAsync(oldName);
            SelectedProfile.Name = result;
            await _profileManager.SaveProfileAsync(SelectedProfile);
            ProfilesChanged = true;
            await LoadProfilesAsync();
        }
    }

    private async void OnDuplicateClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedProfile == null) return;

        var dialog = new InputDialog("Profili Kopyala", "Yeni profil adı:", $"{SelectedProfile.Name} - Kopya");
        var result = await dialog.ShowDialog<string?>(this);
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            if (!await _profileManager.IsProfileNameAvailableAsync(result))
            {
                await ShowMessageAsync("Hata", "Bu isimde bir profil zaten var.");
                return;
            }

            await _profileManager.DuplicateProfileAsync(SelectedProfile.Name, result);
            ProfilesChanged = true;
            await LoadProfilesAsync();
        }
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedProfile == null) return;

        if (Profiles.Count <= 1)
        {
            await ShowMessageAsync("Uyarı", "En az bir profil bulunmalıdır.");
            return;
        }

        var confirm = await ShowConfirmAsync("Profili Sil", $"'{SelectedProfile.Name}' profilini silmek istediğinize emin misiniz?");
        if (confirm)
        {
            await _profileManager.DeleteProfileAsync(SelectedProfile.Name);
            ProfilesChanged = true;
            await LoadProfilesAsync();
        }
    }

    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedProfile == null) 
        {
            await ShowMessageAsync("Uyarı", "Lütfen dışa aktarmak için bir profil seçin.");
            return;
        }

        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Profili Dışa Aktar",
                DefaultExtension = "ledprofile",
                SuggestedFileName = SelectedProfile.Name,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("LED Tabelam Profil")
                    {
                        Patterns = new[] { "*.ledprofile" }
                    }
                }
            });

            if (file != null)
            {
                await _profileManager.ExportProfileAsync(SelectedProfile, file.Path.LocalPath);
                await ShowMessageAsync("Başarılı", $"Profil dışa aktarıldı:\n{file.Path.LocalPath}");
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Hata", $"Dışa aktarma hatası: {ex.Message}");
        }
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
                var imported = await _profileManager.ImportProfileAsync(files[0].Path.LocalPath);
                ProfilesChanged = true;
                await LoadProfilesAsync();
                await ShowMessageAsync("Başarılı", $"'{imported.Name}' profili içe aktarıldı.");
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Hata", $"İçe aktarma hatası: {ex.Message}");
        }
    }

    private async void OnNewProfileClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new InputDialog("Yeni Profil", "Profil adı:", "");
        var result = await dialog.ShowDialog<string?>(this);
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            if (!await _profileManager.IsProfileNameAvailableAsync(result))
            {
                await ShowMessageAsync("Hata", "Bu isimde bir profil zaten var.");
                return;
            }

            var profile = new Profile
            {
                Name = result,
                Settings = new DisplaySettings(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            await _profileManager.SaveProfileAsync(profile);
            ProfilesChanged = true;
            await LoadProfilesAsync();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void OnLoadProfileClick(object? sender, RoutedEventArgs e)
    {
        if (SelectedProfile == null)
        {
            await ShowMessageAsync("Uyarı", "Lütfen yüklemek için bir profil seçin.");
            return;
        }

        ProfileToLoad = SelectedProfile;
        Close();
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var stackPanel = new StackPanel { Spacing = 16 };
        stackPanel.Children.Add(new TextBlock 
        { 
            Text = message, 
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = Avalonia.Media.Brushes.White
        });
        var okButton = new Button 
        { 
            Content = "Tamam", 
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Padding = new Avalonia.Thickness(20, 8)
        };
        stackPanel.Children.Add(okButton);
        
        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Avalonia.Media.Brushes.Transparent,
            Content = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(30, 30, 30)),
                Padding = new Avalonia.Thickness(24),
                CornerRadius = new Avalonia.CornerRadius(8),
                Child = stackPanel
            }
        };
        okButton.Click += (s, e) => dialog.Close();
        await dialog.ShowDialog(this);
    }

    private async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = false;
        
        var cancelButton = new Button { Content = "İptal" };
        var deleteButton = new Button { Content = "Sil", Background = Avalonia.Media.Brushes.Red, Foreground = Avalonia.Media.Brushes.White };
        
        var buttonsPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8
        };
        buttonsPanel.Children.Add(cancelButton);
        buttonsPanel.Children.Add(deleteButton);
        
        var mainPanel = new StackPanel { Spacing = 16 };
        mainPanel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        mainPanel.Children.Add(buttonsPanel);
        
        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Padding = new Avalonia.Thickness(20),
                Child = mainPanel
            }
        };
        
        cancelButton.Click += (s, e) => dialog.Close();
        deleteButton.Click += (s, e) => { result = true; dialog.Close(); };
        await dialog.ShowDialog(this);
        return result;
    }
}
