using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Proje yönetimi servisi implementasyonu
/// </summary>
public class ProjectManager : IProjectManager
{
    private Project _currentProject;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProjectManager()
    {
        _currentProject = CreateDefaultProject();
        _jsonOptions = CreateJsonOptions();
    }

    /// <inheritdoc/>
    public Project CurrentProject => _currentProject;

    /// <inheritdoc/>
    public Task<Project> NewProjectAsync()
    {
        _currentProject = CreateDefaultProject();
        return Task.FromResult(_currentProject);
    }

    /// <inheritdoc/>
    public async Task<Project> LoadProjectAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Dosya yolu boş olamaz", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Proje dosyası bulunamadı", filePath);

        var json = await File.ReadAllTextAsync(filePath);
        var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions);

        if (project == null)
            throw new InvalidOperationException("Proje dosyası okunamadı");

        project.FilePath = filePath;
        _currentProject = project;
        return _currentProject;
    }

    /// <inheritdoc/>
    public async Task SaveProjectAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Dosya yolu boş olamaz", nameof(filePath));

        _currentProject.FilePath = filePath;
        _currentProject.MarkAsModified();

        var json = JsonSerializer.Serialize(_currentProject, _jsonOptions);
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(filePath, json);
    }

    /// <inheritdoc/>
    public async Task SaveProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentProject.FilePath))
            throw new InvalidOperationException("Proje dosya yolu belirlenmemiş. SaveProjectAsync(filePath) kullanın.");

        await SaveProjectAsync(_currentProject.FilePath);
    }


    /// <inheritdoc/>
    public void AddScreen(ScreenNode screen)
    {
        if (screen == null)
            throw new ArgumentNullException(nameof(screen));

        if (string.IsNullOrWhiteSpace(screen.Name))
            screen.Name = GenerateScreenName();

        _currentProject.Screens.Add(screen);
        _currentProject.MarkAsModified();
    }

    /// <inheritdoc/>
    public void RemoveScreen(ScreenNode screen)
    {
        if (screen == null)
            throw new ArgumentNullException(nameof(screen));

        _currentProject.Screens.Remove(screen);
        _currentProject.MarkAsModified();
    }

    /// <inheritdoc/>
    public void AddProgram(ScreenNode screen, ProgramNode program)
    {
        if (screen == null)
            throw new ArgumentNullException(nameof(screen));
        if (program == null)
            throw new ArgumentNullException(nameof(program));

        if (string.IsNullOrWhiteSpace(program.Name))
            program.Name = GenerateProgramName(screen);

        screen.Programs.Add(program);
        _currentProject.MarkAsModified();
    }

    /// <inheritdoc/>
    public void RemoveProgram(ProgramNode program)
    {
        if (program == null)
            throw new ArgumentNullException(nameof(program));

        var parentScreen = FindParentScreen(program);
        if (parentScreen != null)
        {
            parentScreen.Programs.Remove(program);
            _currentProject.MarkAsModified();
        }
    }

    /// <inheritdoc/>
    public void AddContent(ProgramNode program, ContentItem content)
    {
        if (program == null)
            throw new ArgumentNullException(nameof(program));
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        program.Contents.Add(content);
        _currentProject.MarkAsModified();
    }

    /// <inheritdoc/>
    public void RemoveContent(ContentItem content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        var parentProgram = FindParentProgram(content);
        if (parentProgram != null)
        {
            parentProgram.Contents.Remove(content);
            _currentProject.MarkAsModified();
        }
    }

    /// <inheritdoc/>
    public string GenerateScreenName()
    {
        int index = 1;
        string baseName = "Ekran";
        
        while (_currentProject.Screens.Any(s => s.Name == $"{baseName}{index}"))
        {
            index++;
        }
        
        return $"{baseName}{index}";
    }

    /// <inheritdoc/>
    public string GenerateProgramName(ScreenNode screen)
    {
        if (screen == null)
            throw new ArgumentNullException(nameof(screen));

        int index = 1;
        string baseName = "Program";
        
        while (screen.Programs.Any(p => p.Name == $"{baseName}{index}"))
        {
            index++;
        }
        
        return $"{baseName}{index}";
    }

    /// <inheritdoc/>
    public ScreenNode? FindScreen(string screenId)
    {
        if (string.IsNullOrWhiteSpace(screenId))
            return null;

        return _currentProject.Screens.FirstOrDefault(s => s.Id == screenId);
    }

    /// <inheritdoc/>
    public ProgramNode? FindProgram(string programId)
    {
        if (string.IsNullOrWhiteSpace(programId))
            return null;

        foreach (var screen in _currentProject.Screens)
        {
            var program = screen.Programs.FirstOrDefault(p => p.Id == programId);
            if (program != null)
                return program;
        }
        return null;
    }

    /// <inheritdoc/>
    public ContentItem? FindContent(string contentId)
    {
        if (string.IsNullOrWhiteSpace(contentId))
            return null;

        foreach (var screen in _currentProject.Screens)
        {
            foreach (var program in screen.Programs)
            {
                var content = program.Contents.FirstOrDefault(c => c.Id == contentId);
                if (content != null)
                    return content;
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public ScreenNode? FindParentScreen(ProgramNode program)
    {
        if (program == null)
            return null;

        return _currentProject.Screens.FirstOrDefault(s => s.Programs.Contains(program));
    }

    /// <inheritdoc/>
    public ProgramNode? FindParentProgram(ContentItem content)
    {
        if (content == null)
            return null;

        foreach (var screen in _currentProject.Screens)
        {
            var program = screen.Programs.FirstOrDefault(p => p.Contents.Contains(content));
            if (program != null)
                return program;
        }
        return null;
    }

    private static Project CreateDefaultProject()
    {
        var project = new Project
        {
            Name = "Yeni Proje",
            FilePath = string.Empty,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now,
            GlobalSettings = new DisplaySettings
            {
                PanelWidth = 160,
                PanelHeight = 24,
                ColorType = LedColorType.Amber,
                Brightness = 100,
                BackgroundDarkness = 100,
                PixelSize = 4,
                Pitch = PixelPitch.P10,
                Shape = PixelShape.Round,
                ZoomLevel = 100,
                LetterSpacing = 1
            }
        };

        // Varsayılan ekran oluştur
        var defaultScreen = new ScreenNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Ekran1",
            Width = 128,
            Height = 32,
            IsExpanded = true
        };

        // Varsayılan program oluştur
        var defaultProgram = new ProgramNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Program1",
            IsLoop = true,
            TransitionType = TransitionType.None,
            IsExpanded = true
        };

        // Varsayılan metin içeriği oluştur
        var defaultContent = new TextContent
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Metin Yazı",
            X = 0,
            Y = 0,
            Width = 128,
            Height = 16,
            DurationMs = 3000,
            ShowImmediately = true,
            Text = "HOŞGELDİNİZ",
            FontName = "PolarisRGB6x8",
            FontSize = 8,
            ForegroundColor = Color.FromRgb(255, 176, 0), // Amber
            BackgroundColor = Colors.Transparent,
            HorizontalAlignment = Models.HorizontalAlignment.Center,
            VerticalAlignment = Models.VerticalAlignment.Center,
            EntryEffect = new EffectConfig
            {
                EffectType = EffectType.Immediate,
                SpeedMs = 500,
                Direction = EffectDirection.Left
            },
            ExitEffect = new EffectConfig
            {
                EffectType = EffectType.Immediate,
                SpeedMs = 500,
                Direction = EffectDirection.Left
            }
        };

        defaultProgram.Contents.Add(defaultContent);
        defaultScreen.Programs.Add(defaultProgram);
        project.Screens.Add(defaultScreen);

        return project;
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
        options.Converters.Add(new ContentItemJsonConverter());
        return options;
    }
}



/// <summary>
/// ContentItem polimorfik serializasyon için JSON converter
/// </summary>
public class ContentItemJsonConverter : JsonConverter<ContentItem>
{
    public override ContentItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("contentType", out var contentTypeElement))
            return null;

        var contentType = (ContentType)contentTypeElement.GetInt32();
        var json = root.GetRawText();

        // Create new options without this converter to avoid recursion
        var newOptions = new JsonSerializerOptions(options);
        var converterToRemove = newOptions.Converters.FirstOrDefault(c => c is ContentItemJsonConverter);
        if (converterToRemove != null)
            newOptions.Converters.Remove(converterToRemove);

        return contentType switch
        {
            ContentType.Text => JsonSerializer.Deserialize<TextContent>(json, newOptions),
            ContentType.Clock => JsonSerializer.Deserialize<ClockContent>(json, newOptions),
            ContentType.Date => JsonSerializer.Deserialize<DateContent>(json, newOptions),
            ContentType.Countdown => JsonSerializer.Deserialize<CountdownContent>(json, newOptions),
            _ => JsonSerializer.Deserialize<ContentItem>(json, newOptions)
        };
    }

    public override void Write(Utf8JsonWriter writer, ContentItem value, JsonSerializerOptions options)
    {
        // Create new options without this converter to avoid recursion
        var newOptions = new JsonSerializerOptions(options);
        var converterToRemove = newOptions.Converters.FirstOrDefault(c => c is ContentItemJsonConverter);
        if (converterToRemove != null)
            newOptions.Converters.Remove(converterToRemove);

        // Serialize as the actual type
        JsonSerializer.Serialize(writer, value, value.GetType(), newOptions);
    }
}
