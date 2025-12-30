using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Media;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Profil yönetimi servisi implementasyonu
/// Requirements: 9.7, 9.12
/// </summary>
public class ProfileManager : IProfileManager
{
    private readonly string _profilesDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string DefaultProfileName = "Varsayılan";
    private const string ProfileExtension = ".json";

    public ProfileManager() : this(GetDefaultProfilesDirectory())
    {
    }

    public ProfileManager(string profilesDirectory)
    {
        _profilesDirectory = profilesDirectory;
        _jsonOptions = CreateJsonOptions();
        EnsureDirectoryExists();
    }

    private static string GetDefaultProfilesDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "LEDTabelam", "Profiles");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new ColorJsonConverter());
        return options;
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_profilesDirectory))
        {
            Directory.CreateDirectory(_profilesDirectory);
        }
    }

    private string GetProfilePath(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_profilesDirectory, safeName + ProfileExtension);
    }

    /// <inheritdoc/>
    public async Task<List<Profile>> GetAllProfilesAsync()
    {
        EnsureDirectoryExists();
        var profiles = new List<Profile>();
        var files = Directory.GetFiles(_profilesDirectory, "*" + ProfileExtension);

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var profile = JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }
            catch (Exception)
            {
                // Bozuk profil dosyasını atla
            }
        }

        return profiles.OrderBy(p => p.Name).ToList();
    }

    /// <inheritdoc/>
    public async Task<Profile?> LoadProfileAsync(string name)
    {
        var path = GetProfilePath(name);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveProfileAsync(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new ArgumentException("Profil adı boş olamaz.", nameof(profile));
        }

        profile.ModifiedAt = DateTime.UtcNow;
        var path = GetProfilePath(profile.Name);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteProfileAsync(string name)
    {
        if (name == DefaultProfileName)
        {
            return false; // Varsayılan profil silinemez
        }

        var path = GetProfilePath(name);
        if (!File.Exists(path))
        {
            return false;
        }

        await Task.Run(() => File.Delete(path));
        return true;
    }

    /// <inheritdoc/>
    public async Task<Profile> DuplicateProfileAsync(string sourceName, string newName)
    {
        var source = await LoadProfileAsync(sourceName);
        if (source == null)
        {
            throw new FileNotFoundException($"Kaynak profil bulunamadı: {sourceName}");
        }

        if (!await IsProfileNameAvailableAsync(newName))
        {
            throw new InvalidOperationException($"Bu isimde bir profil zaten var: {newName}");
        }

        var duplicate = CloneProfile(source);
        duplicate.Name = newName;
        duplicate.CreatedAt = DateTime.UtcNow;
        duplicate.ModifiedAt = DateTime.UtcNow;

        await SaveProfileAsync(duplicate);
        return duplicate;
    }

    /// <inheritdoc/>
    public async Task ExportProfileAsync(Profile profile, string filePath)
    {
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <inheritdoc/>
    public async Task<Profile> ImportProfileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Dosya bulunamadı.", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        var profile = JsonSerializer.Deserialize<Profile>(json, _jsonOptions);
        
        if (profile == null)
        {
            throw new InvalidOperationException("Geçersiz profil dosyası.");
        }

        // Aynı isimde profil varsa yeni isim oluştur
        var originalName = profile.Name;
        var counter = 1;
        while (!await IsProfileNameAvailableAsync(profile.Name))
        {
            profile.Name = $"{originalName} ({counter++})";
        }

        profile.ModifiedAt = DateTime.UtcNow;
        await SaveProfileAsync(profile);
        return profile;
    }

    /// <inheritdoc/>
    public async Task<Profile> GetOrCreateDefaultProfileAsync()
    {
        var existing = await LoadProfileAsync(DefaultProfileName);
        if (existing != null)
        {
            return existing;
        }

        var defaultProfile = CreateDefaultProfile();
        await SaveProfileAsync(defaultProfile);
        return defaultProfile;
    }

    /// <inheritdoc/>
    public async Task<bool> IsProfileNameAvailableAsync(string name)
    {
        var path = GetProfilePath(name);
        return await Task.FromResult(!File.Exists(path));
    }

    /// <summary>
    /// Varsayılan profili oluşturur
    /// Requirements: 9.12
    /// </summary>
    private static Profile CreateDefaultProfile()
    {
        return new Profile
        {
            Name = DefaultProfileName,
            Settings = new DisplaySettings
            {
                PanelWidth = 160,
                PanelHeight = 24,
                ColorType = LedColorType.Amber,
                Pitch = PixelPitch.P10,
                Shape = PixelShape.Round,
                Brightness = 100,
                BackgroundDarkness = 100,
                PixelSize = 8
            },
            FontName = "PixelFont8", // Varsayılan font
            DefaultZones = new List<Zone>
            {
                new Zone { Index = 0, WidthPercent = 100, ContentType = ZoneContentType.Text }
            },
            Slots = new Dictionary<int, TabelaSlot>(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Profili derin kopyalar
    /// </summary>
    private Profile CloneProfile(Profile source)
    {
        var json = JsonSerializer.Serialize(source, _jsonOptions);
        return JsonSerializer.Deserialize<Profile>(json, _jsonOptions)!;
    }
}


/// <summary>
/// Avalonia Color için JSON converter
/// </summary>
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        if (string.IsNullOrEmpty(hex))
        {
            return Colors.Black;
        }

        // #AARRGGBB veya #RRGGBB formatını destekle
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        if (hex.Length == 6)
        {
            // RRGGBB
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromRgb(r, g, b);
        }
        else if (hex.Length == 8)
        {
            // AARRGGBB
            var a = Convert.ToByte(hex.Substring(0, 2), 16);
            var r = Convert.ToByte(hex.Substring(2, 2), 16);
            var g = Convert.ToByte(hex.Substring(4, 2), 16);
            var b = Convert.ToByte(hex.Substring(6, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }

        return Colors.Black;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }
}
