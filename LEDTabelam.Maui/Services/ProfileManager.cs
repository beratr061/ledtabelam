using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;
using Microsoft.Maui.Graphics;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Profil yönetimi servisi implementasyonu
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
        options.Converters.Add(new MauiColorJsonConverter());
        return options;
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_profilesDirectory))
            Directory.CreateDirectory(_profilesDirectory);
    }

    private string GetProfilePath(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_profilesDirectory, safeName + ProfileExtension);
    }

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
                    profile.EnsureMinimumProgram();
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

    public async Task<Profile?> LoadProfileAsync(string name)
    {
        var path = GetProfilePath(name);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            var profile = JsonSerializer.Deserialize<Profile>(json, _jsonOptions);

            if (profile != null)
                profile.EnsureMinimumProgram();

            return profile;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SaveProfileAsync(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
            throw new ArgumentException("Profil adı boş olamaz.", nameof(profile));

        profile.ModifiedAt = DateTime.UtcNow;
        var path = GetProfilePath(profile.Name);
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<bool> DeleteProfileAsync(string name)
    {
        if (name == DefaultProfileName)
            return false;

        var path = GetProfilePath(name);
        if (!File.Exists(path))
            return false;

        await Task.Run(() => File.Delete(path));
        return true;
    }

    public async Task<Profile> DuplicateProfileAsync(string sourceName, string newName)
    {
        var source = await LoadProfileAsync(sourceName);
        if (source == null)
            throw new FileNotFoundException($"Kaynak profil bulunamadı: {sourceName}");

        if (!await IsProfileNameAvailableAsync(newName))
            throw new InvalidOperationException($"Bu isimde bir profil zaten var: {newName}");

        var duplicate = CloneProfile(source);
        duplicate.Name = newName;
        duplicate.CreatedAt = DateTime.UtcNow;
        duplicate.ModifiedAt = DateTime.UtcNow;

        await SaveProfileAsync(duplicate);
        return duplicate;
    }

    public async Task ExportProfileAsync(Profile profile, string filePath)
    {
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<Profile> ImportProfileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Dosya bulunamadı.", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var profile = JsonSerializer.Deserialize<Profile>(json, _jsonOptions);

        if (profile == null)
            throw new InvalidOperationException("Geçersiz profil dosyası.");

        profile.EnsureMinimumProgram();

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

    public async Task<Profile> GetOrCreateDefaultProfileAsync()
    {
        var existing = await LoadProfileAsync(DefaultProfileName);
        if (existing != null)
            return existing;

        var defaultProfile = CreateDefaultProfile();
        await SaveProfileAsync(defaultProfile);
        return defaultProfile;
    }

    public async Task<bool> IsProfileNameAvailableAsync(string name)
    {
        var path = GetProfilePath(name);
        return await Task.FromResult(!File.Exists(path));
    }

    private static Profile CreateDefaultProfile()
    {
        var profile = new Profile
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
            FontName = "PixelFont8",
            DefaultZones = new List<Zone>
            {
                new Zone { Index = 0, WidthPercent = 100, ContentType = ZoneContentType.Text }
            },
            Slots = new Dictionary<int, TabelaSlot>(),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        profile.EnsureMinimumProgram();
        return profile;
    }

    private Profile CloneProfile(Profile source)
    {
        var json = JsonSerializer.Serialize(source, _jsonOptions);
        return JsonSerializer.Deserialize<Profile>(json, _jsonOptions)!;
    }
}

/// <summary>
/// MAUI Color için JSON converter
/// </summary>
public class MauiColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        if (string.IsNullOrEmpty(hex))
            return Colors.Black;

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length == 6)
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromRgb(r, g, b);
        }
        else if (hex.Length == 8)
        {
            var a = Convert.ToByte(hex.Substring(0, 2), 16);
            var r = Convert.ToByte(hex.Substring(2, 2), 16);
            var g = Convert.ToByte(hex.Substring(4, 2), 16);
            var b = Convert.ToByte(hex.Substring(6, 2), 16);
            return Color.FromRgba(r, g, b, a);
        }

        return Colors.Black;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        var a = (byte)(value.Alpha * 255);
        var r = (byte)(value.Red * 255);
        var g = (byte)(value.Green * 255);
        var b = (byte)(value.Blue * 255);
        writer.WriteStringValue($"#{a:X2}{r:X2}{g:X2}{b:X2}");
    }
}
