# Design Document: MAUI UI Redesign

## Overview

LEDTabelam uygulaması, Avalonia UI'dan .NET MAUI'ye geçiş yapacak ve HD2020 benzeri profesyonel bir arayüze kavuşacaktır. Bu tasarım, mevcut tüm işlevselliği koruyarak modern bir kullanıcı deneyimi sunmayı hedeflemektedir.

### Temel Değişiklikler
- Avalonia UI → .NET MAUI geçişi
- HD2020 benzeri 4 bölgeli layout
- TreeView tabanlı içerik hiyerarşisi
- Gelişmiş özellikler paneli
- Yeni içerik tipleri (Saat, Tarih, Geri Sayım)

### Korunan Özellikler
- Mevcut model sınıfları (DisplaySettings, BitmapFont, Profile, vb.)
- Mevcut servis sınıfları (FontLoader, LedRenderer, ProfileManager, vb.)
- 999 slot yönetimi
- PNG/GIF/WebP export
- Zone ve playlist yönetimi

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         .NET MAUI Application                           │
├─────────────────────────────────────────────────────────────────────────┤
│                          Presentation Layer                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │
│  │ MainPage    │  │ TreeView    │  │ Preview     │  │ Properties  │   │
│  │ (XAML)      │  │ Panel       │  │ Panel       │  │ Panel       │   │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘   │
│         │                │                │                │          │
│  ┌──────┴────────────────┴────────────────┴────────────────┴──────┐   │
│  │                    ViewModels (CommunityToolkit.Mvvm)          │   │
│  │  MainViewModel, TreeViewModel, PreviewViewModel, etc.          │   │
│  └────────────────────────────────┬───────────────────────────────┘   │
└───────────────────────────────────┼───────────────────────────────────┘
                                    │
┌───────────────────────────────────┼───────────────────────────────────┐
│                           Service Layer                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │ FontLoader   │  │ LedRenderer  │  │ProfileManager│  (Mevcut)      │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │ SlotManager  │  │ ZoneManager  │  │AssetLibrary  │  (Mevcut)      │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │AnimationSvc  │  │ ExportService│  │ SvgRenderer  │  (Mevcut)      │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │ProjectManager│  │ContentManager│  │EffectService │  (Yeni)        │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
└───────────────────────────────────┬───────────────────────────────────┘
                                    │
┌───────────────────────────────────┼───────────────────────────────────┐
│                            Model Layer                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │ BitmapFont   │  │DisplaySettings│ │   Profile   │  (Mevcut)       │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │  TabelaSlot  │  │    Zone      │  │ PlaylistItem │  (Mevcut)      │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │ ScreenNode   │  │ ProgramNode  │  │ ContentItem  │  (Yeni)        │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                │
│  │  Project     │  │ EffectConfig │  │ ClockContent │  (Yeni)        │
│  └──────────────┘  └──────────────┘  └──────────────┘                │
└───────────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. Yeni Model Sınıfları

#### Project
```csharp
public partial class Project : ObservableObject
{
    [ObservableProperty]
    private string name = "Yeni Proje";
    
    [ObservableProperty]
    private string filePath = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<ScreenNode> screens = new();
    
    [ObservableProperty]
    private DisplaySettings globalSettings = new();
    
    [ObservableProperty]
    private DateTime createdAt = DateTime.Now;
    
    [ObservableProperty]
    private DateTime modifiedAt = DateTime.Now;
}
```

#### ScreenNode
```csharp
public partial class ScreenNode : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString();
    
    [ObservableProperty]
    private string name = "Ekran1";
    
    [ObservableProperty]
    private int width = 128;
    
    [ObservableProperty]
    private int height = 32;
    
    [ObservableProperty]
    private ObservableCollection<ProgramNode> programs = new();
    
    [ObservableProperty]
    private bool isExpanded = true;
}
```

#### ProgramNode
```csharp
public partial class ProgramNode : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString();
    
    [ObservableProperty]
    private string name = "Program1";
    
    [ObservableProperty]
    private ObservableCollection<ContentItem> contents = new();
    
    [ObservableProperty]
    private bool isLoop = true;
    
    [ObservableProperty]
    private TransitionType transitionType = TransitionType.None;
    
    [ObservableProperty]
    private bool isExpanded = true;
}
```

#### ContentItem (Base)
```csharp
public partial class ContentItem : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString();
    
    [ObservableProperty]
    private string name = "İçerik";
    
    [ObservableProperty]
    private ContentType contentType = ContentType.Text;
    
    [ObservableProperty]
    private int x = 0;
    
    [ObservableProperty]
    private int y = 0;
    
    [ObservableProperty]
    private int width = 128;
    
    [ObservableProperty]
    private int height = 16;
    
    [ObservableProperty]
    private EffectConfig entryEffect = new();
    
    [ObservableProperty]
    private EffectConfig exitEffect = new();
    
    [ObservableProperty]
    private int durationMs = 3000;
    
    [ObservableProperty]
    private bool showImmediately = true;
}

public enum ContentType
{
    Text,
    Image,
    Clock,
    Date,
    Countdown
}
```

#### TextContent
```csharp
public partial class TextContent : ContentItem
{
    [ObservableProperty]
    private string text = "";
    
    [ObservableProperty]
    private string fontName = "Default";
    
    [ObservableProperty]
    private int fontSize = 16;
    
    [ObservableProperty]
    private Color foregroundColor = Colors.Amber;
    
    [ObservableProperty]
    private Color backgroundColor = Colors.Transparent;
    
    [ObservableProperty]
    private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
    
    [ObservableProperty]
    private VerticalAlignment verticalAlignment = VerticalAlignment.Center;
    
    [ObservableProperty]
    private bool isBold = false;
    
    [ObservableProperty]
    private bool isItalic = false;
    
    [ObservableProperty]
    private bool isUnderline = false;
    
    [ObservableProperty]
    private bool isRightToLeft = false;
    
    [ObservableProperty]
    private bool isScrolling = false;
    
    [ObservableProperty]
    private int scrollSpeed = 20;
    
    public TextContent()
    {
        ContentType = ContentType.Text;
        Name = "Metin Yazı";
    }
}
```

#### ClockContent
```csharp
public partial class ClockContent : ContentItem
{
    [ObservableProperty]
    private string format = "HH:mm:ss";
    
    [ObservableProperty]
    private string fontName = "Default";
    
    [ObservableProperty]
    private Color foregroundColor = Colors.Amber;
    
    [ObservableProperty]
    private bool showSeconds = true;
    
    [ObservableProperty]
    private bool is24Hour = true;
    
    public ClockContent()
    {
        ContentType = ContentType.Clock;
        Name = "Saat";
    }
}
```

#### DateContent
```csharp
public partial class DateContent : ContentItem
{
    [ObservableProperty]
    private string format = "dd.MM.yyyy";
    
    [ObservableProperty]
    private string fontName = "Default";
    
    [ObservableProperty]
    private Color foregroundColor = Colors.Amber;
    
    public DateContent()
    {
        ContentType = ContentType.Date;
        Name = "Tarih";
    }
}
```

#### CountdownContent
```csharp
public partial class CountdownContent : ContentItem
{
    [ObservableProperty]
    private DateTime targetDateTime = DateTime.Now.AddHours(1);
    
    [ObservableProperty]
    private string format = "HH:mm:ss";
    
    [ObservableProperty]
    private string fontName = "Default";
    
    [ObservableProperty]
    private Color foregroundColor = Colors.Amber;
    
    [ObservableProperty]
    private string completedText = "SÜRE DOLDU";
    
    public CountdownContent()
    {
        ContentType = ContentType.Countdown;
        Name = "Geri Sayım";
    }
}
```

#### EffectConfig
```csharp
public partial class EffectConfig : ObservableObject
{
    [ObservableProperty]
    private EffectType effectType = EffectType.Immediate;
    
    [ObservableProperty]
    private int speedMs = 500;
    
    [ObservableProperty]
    private EffectDirection direction = EffectDirection.Left;
}

public enum EffectType
{
    Immediate,      // Hemen Göster
    SlideIn,        // Kayarak Gir
    FadeIn,         // Solarak Gir
    None            // Efekt Yok
}

public enum EffectDirection
{
    Left,
    Right,
    Up,
    Down
}
```

### 2. Yeni Servis Sınıfları

#### IProjectManager
```csharp
public interface IProjectManager
{
    Project CurrentProject { get; }
    Task<Project> NewProjectAsync();
    Task<Project> LoadProjectAsync(string filePath);
    Task SaveProjectAsync(string filePath);
    Task SaveProjectAsync(); // Mevcut dosyaya kaydet
    void AddScreen(ScreenNode screen);
    void RemoveScreen(ScreenNode screen);
    void AddProgram(ScreenNode screen, ProgramNode program);
    void RemoveProgram(ProgramNode program);
    void AddContent(ProgramNode program, ContentItem content);
    void RemoveContent(ContentItem content);
}
```

#### IContentManager
```csharp
public interface IContentManager
{
    ContentItem CreateTextContent();
    ContentItem CreateImageContent();
    ContentItem CreateClockContent();
    ContentItem CreateDateContent();
    ContentItem CreateCountdownContent();
    void UpdateContent(ContentItem content);
    SKBitmap RenderContent(ContentItem content, DisplaySettings settings);
}
```

#### IEffectService
```csharp
public interface IEffectService
{
    void ApplyEntryEffect(ContentItem content, SKCanvas canvas, double progress);
    void ApplyExitEffect(ContentItem content, SKCanvas canvas, double progress);
    Task PlayEffectAsync(ContentItem content, EffectConfig effect);
    void StopEffect();
}
```

### 3. ViewModels

#### MainViewModel
```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private TreeViewModel treeView;
    
    [ObservableProperty]
    private PreviewViewModel preview;
    
    [ObservableProperty]
    private PropertiesViewModel properties;
    
    [ObservableProperty]
    private EditorViewModel editor;
    
    [ObservableProperty]
    private string statusMessage = "Hazır";
    
    [ObservableProperty]
    private string connectionStatus = "Çevrimdışı";
    
    // Commands
    [RelayCommand]
    private async Task NewProject();
    
    [RelayCommand]
    private async Task OpenProject();
    
    [RelayCommand]
    private async Task SaveProject();
    
    [RelayCommand]
    private void AddScreen();
    
    [RelayCommand]
    private void AddProgram();
    
    [RelayCommand]
    private void AddTextContent();
    
    [RelayCommand]
    private void AddClockContent();
    
    [RelayCommand]
    private void StartPreview();
}
```

#### TreeViewModel
```csharp
public partial class TreeViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ScreenNode> screens = new();
    
    [ObservableProperty]
    private object? selectedItem;
    
    [ObservableProperty]
    private bool isExpanded = true;
    
    // Commands
    [RelayCommand]
    private void SelectItem(object item);
    
    [RelayCommand]
    private void ExpandAll();
    
    [RelayCommand]
    private void CollapseAll();
    
    [RelayCommand]
    private void DeleteSelected();
    
    [RelayCommand]
    private void DuplicateSelected();
}
```

#### PreviewViewModel
```csharp
public partial class PreviewViewModel : ObservableObject
{
    [ObservableProperty]
    private SKBitmap? previewBitmap;
    
    [ObservableProperty]
    private int zoomLevel = 100;
    
    [ObservableProperty]
    private int currentPage = 1;
    
    [ObservableProperty]
    private int totalPages = 1;
    
    [ObservableProperty]
    private bool isPlaying = false;
    
    // Commands
    [RelayCommand]
    private void ZoomIn();
    
    [RelayCommand]
    private void ZoomOut();
    
    [RelayCommand]
    private void NextPage();
    
    [RelayCommand]
    private void PreviousPage();
    
    [RelayCommand]
    private void ToggleFullscreen();
    
    [RelayCommand]
    private void TogglePlay();
}
```

#### PropertiesViewModel
```csharp
public partial class PropertiesViewModel : ObservableObject
{
    [ObservableProperty]
    private ContentItem? selectedContent;
    
    [ObservableProperty]
    private EffectType selectedEntryEffect = EffectType.Immediate;
    
    [ObservableProperty]
    private EffectType selectedExitEffect = EffectType.Immediate;
    
    [ObservableProperty]
    private int effectSpeed = 500;
    
    [ObservableProperty]
    private int displayDuration = 3000;
    
    [ObservableProperty]
    private bool showImmediately = true;
    
    [ObservableProperty]
    private bool isTimed = false;
    
    [ObservableProperty]
    private BorderStyle borderStyle = BorderStyle.None;
    
    [ObservableProperty]
    private Color backgroundColor = Colors.Transparent;
}

public enum BorderStyle
{
    None,
    Solid,
    Dashed,
    Custom
}
```

#### EditorViewModel
```csharp
public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private ContentItem? editingContent;
    
    [ObservableProperty]
    private string text = "";
    
    [ObservableProperty]
    private string selectedFont = "Default";
    
    [ObservableProperty]
    private int fontSize = 16;
    
    [ObservableProperty]
    private Color foregroundColor = Colors.Amber;
    
    [ObservableProperty]
    private Color backgroundColor = Colors.Transparent;
    
    [ObservableProperty]
    private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
    
    [ObservableProperty]
    private bool isBold = false;
    
    [ObservableProperty]
    private bool isItalic = false;
    
    [ObservableProperty]
    private bool isUnderline = false;
    
    [ObservableProperty]
    private bool isRightToLeft = false;
    
    [ObservableProperty]
    private int positionX = 0;
    
    [ObservableProperty]
    private int positionY = 0;
    
    [ObservableProperty]
    private int contentWidth = 128;
    
    [ObservableProperty]
    private int contentHeight = 16;
    
    [ObservableProperty]
    private SKBitmap? miniPreview;
    
    [ObservableProperty]
    private ObservableCollection<string> availableFonts = new();
}
```

## Data Models

### Project JSON Schema
```json
{
  "name": "Metrobüs Tabelası",
  "filePath": "C:/Projects/metrobus.ledproj",
  "globalSettings": {
    "width": 128,
    "height": 32,
    "colorType": "Amber",
    "brightness": 100,
    "pitch": "P10"
  },
  "screens": [
    {
      "id": "screen-1",
      "name": "Ekran1",
      "width": 128,
      "height": 32,
      "programs": [
        {
          "id": "prog-1",
          "name": "Program1",
          "isLoop": true,
          "transitionType": "Fade",
          "contents": [
            {
              "id": "content-1",
              "contentType": "Text",
              "name": "Metin Yazı1",
              "x": 0,
              "y": 0,
              "width": 128,
              "height": 16,
              "text": "YENİ CAMİ",
              "fontName": "PolarisRGB6x8",
              "foregroundColor": "#00FF00",
              "horizontalAlignment": "Center",
              "entryEffect": {
                "effectType": "SlideIn",
                "speedMs": 500,
                "direction": "Left"
              },
              "exitEffect": {
                "effectType": "Immediate"
              },
              "durationMs": 3000
            }
          ]
        }
      ]
    }
  ],
  "createdAt": "2026-01-01T00:00:00Z",
  "modifiedAt": "2026-01-01T00:00:00Z"
}
```

## UI Layout

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ HD2020 - LEDTabelam                                                    [_][□][X]│
├─────────────────────────────────────────────────────────────────────────────────┤
│ Dosya(F)  Ayarlar(A)  Ekle(E)  Yardım(H)                                        │
├─────────────────────────────────────────────────────────────────────────────────┤
│ [Program] [Metin Yaz] [Zaman-Alan] [Saat] [Kronmetre] [Tarih] [Saat Ayarı]     │
│ [USB'ye Aktar] [Gönder] [Ara] [Ön İzleme]                          Bağlı Online│
├────────────────┬────────────────────────────────────────────┬───────────────────┤
│ Alan Sözlüğü   │  Ekran1: 128 * 32 Full Renk Gri Seviyesi 8 │ Efekt        [?] │
│ ┌────────────┐ │  ┌──────────────────────────────────────┐  │ ┌─────────────┐  │
│ │▼ Ekran1    │ │  │                                      │  │ │ Efekt:      │  │
│ │  ▼ Program1│ │  │                                      │  │ │ [Hemen Göst▼│  │
│ │    Metin1  │ │  │        ┌──────────────────┐          │  │ │             │  │
│ │    Metin2  │ │  │        │   YENİ CAMİ      │          │  │ │ Hız:        │  │
│ │▼ Ekran2    │ │  │        │   PEKSENLER      │          │  │ │ [██████░░░] │  │
│ │  ▼ Program1│ │  │        └──────────────────┘          │  │ │             │  │
│ │    Resim1  │ │  │                                      │  │ │ Durma Zamanı│  │
│ │    Metin1  │ │  │                                      │  │ │ [3    ] sn  │  │
│ │▼ Ekran3    │ │  │                                      │  │ │             │  │
│ │  ▼ Program1│ │  └──────────────────────────────────────┘  │ │ □ Sürer     │  │
│ │    Metin1  │ │                                            │ └─────────────┘  │
│ └────────────┘ │  [◀] [▶] [⏸] [⏹] [↔] [K] 1/4 [M] 300% [🔍]│ Kayış Süresi [?]│
├────────────────┴────────────────────────────────────────────┴───────────────────┤
│ [▣][▤][▥][▦] [AB][▼] [Sağ > Sol]                                               │
│ ┌─────────────────────────────────────────────────────────────────────────────┐│
│ │ Konum        │ PolarisRGBx10 ▼│ 16 ▼│ ████ │ Tablo ▼│                       ││
│ │ X: [0    ]   │ [A][▼][▼][■][B][I][U][▼][00]                                 ││
│ │ Y: [0    ]   ├─────────────────────────────────────────────────────────────┐││
│ │              │                                                             │││
│ │ Genişlik     │                      YENİ CAMİ                              │││
│ │ [128   ]     │                                                             │││
│ │              │                                                             │││
│ │ Yükseklik    └─────────────────────────────────────────────────────────────┘││
│ │ [16    ]     │                                                              ││
│ │ [🔒]         │                                                              ││
│ └─────────────────────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────────────────────┤
│ Çözünürlük: 128 x 32 │ Zoom: 300% │ Bağlantı: Çevrimdışı │ Hazır               │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## MAUI Project Structure

```
LEDTabelam.Maui/
├── App.xaml
├── App.xaml.cs
├── MauiProgram.cs
├── AppShell.xaml
├── AppShell.xaml.cs
├── Platforms/
│   └── Windows/
│       ├── App.xaml
│       ├── App.xaml.cs
│       └── Package.appxmanifest
├── Resources/
│   ├── Fonts/
│   ├── Images/
│   ├── Styles/
│   │   ├── Colors.xaml
│   │   └── Styles.xaml
│   └── Raw/
├── Models/
│   ├── Project.cs
│   ├── ScreenNode.cs
│   ├── ProgramNode.cs
│   ├── ContentItem.cs
│   ├── TextContent.cs
│   ├── ClockContent.cs
│   ├── DateContent.cs
│   ├── CountdownContent.cs
│   ├── EffectConfig.cs
│   └── (Mevcut modeller: DisplaySettings, BitmapFont, vb.)
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── TreeViewModel.cs
│   ├── PreviewViewModel.cs
│   ├── PropertiesViewModel.cs
│   └── EditorViewModel.cs
├── Views/
│   ├── MainPage.xaml
│   ├── MainPage.xaml.cs
│   ├── Controls/
│   │   ├── TreeViewPanel.xaml
│   │   ├── PreviewPanel.xaml
│   │   ├── PropertiesPanel.xaml
│   │   ├── EditorPanel.xaml
│   │   ├── ToolbarPanel.xaml
│   │   └── StatusBarPanel.xaml
│   └── Dialogs/
│       ├── NewProjectDialog.xaml
│       ├── SettingsDialog.xaml
│       └── AboutDialog.xaml
├── Services/
│   ├── ProjectManager.cs
│   ├── ContentManager.cs
│   ├── EffectService.cs
│   └── (Mevcut servisler: FontLoader, LedRenderer, vb.)
├── Converters/
│   ├── ColorToSKColorConverter.cs
│   ├── ContentTypeToIconConverter.cs
│   └── BoolToVisibilityConverter.cs
└── Helpers/
    ├── SkiaSharpExtensions.cs
    └── MauiHelpers.cs
```

## Error Handling

### MAUI Geçiş Hataları
| Error | Handling |
|-------|----------|
| Platform uyumsuzluğu | Windows-specific kod için conditional compilation |
| SkiaSharp render hatası | Fallback software rendering |
| Dosya erişim hatası | Platform-specific file picker kullanımı |

### Proje Yönetimi Hataları
| Error | Handling |
|-------|----------|
| Proje dosyası bulunamadı | Hata mesajı, yeni proje öner |
| JSON parse hatası | Detaylı hata mesajı, yedek dosya kontrolü |
| Kaydetme hatası | Otomatik yedekleme, alternatif konum öner |

### İçerik Hataları
| Error | Handling |
|-------|----------|
| Geçersiz içerik tipi | Varsayılan metin içeriği oluştur |
| Font bulunamadı | Varsayılan font kullan |
| Resim yüklenemedi | Placeholder göster |



## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Project Round-Trip Consistency
*For any* valid Project object containing screens, programs, and content items, saving to JSON and loading back should produce an equivalent Project with all data preserved including nested hierarchies.

**Validates: Requirements 8.5, 8.6, 8.7**

### Property 2: Model Backward Compatibility
*For any* existing model class (DisplaySettings, BitmapFont, Profile, TabelaSlot, Zone, PlaylistItem, TextStyle), the class should remain functional and serializable in the new MAUI application without data loss.

**Validates: Requirements 1.6, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8**

### Property 3: Service Backward Compatibility
*For any* existing service (FontLoader, LedRenderer, ProfileManager, SlotManager, ZoneManager, AnimationService, ExportService, SvgRenderer), the service should produce identical outputs for identical inputs in the new MAUI application.

**Validates: Requirements 1.7, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8**

### Property 4: TreeView Hierarchy Consistency
*For any* Project with screens, programs, and contents, the TreeView should display exactly three levels of hierarchy where each screen contains programs and each program contains content items. The total count of displayed nodes should equal screens + programs + contents.

**Validates: Requirements 3.1, 10.1, 10.3**

### Property 5: Auto-Naming Uniqueness
*For any* sequence of screen or program additions, the auto-generated names should be unique within their parent container. Adding N screens should produce names "Ekran1" through "EkranN" with no duplicates.

**Validates: Requirements 3.8, 3.9**

### Property 6: Zoom Bounds Validation
*For any* zoom operation, the resulting zoom level should be clamped between 50% and 400%. Attempting to zoom beyond these bounds should result in the boundary value being applied.

**Validates: Requirements 4.6, 4.7**

### Property 7: Page Navigation Consistency
*For any* program with N content items, the page navigation should cycle through pages 1 to N. After reaching page N, "next" should either stop or loop to page 1 based on loop setting. Current page should always be within [1, N] range.

**Validates: Requirements 4.4, 10.5, 10.6**

### Property 8: Content Type Creation
*For any* content type (Text, Image, Clock, Date, Countdown), creating a new content item should produce an object with all required properties initialized to valid default values. The content type should be correctly set and the item should be renderable.

**Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7**

### Property 9: Program Execution Order
*For any* program with ordered content items, executing the program should display contents in their defined order. The sequence should be deterministic and repeatable for the same program configuration.

**Validates: Requirements 10.4, 10.5, 10.7**

### Property 10: Effect Application
*For any* content item with entry/exit effects configured, applying the effect should produce a visual transformation that progresses from 0% to 100% over the specified duration. The effect type should determine the transformation behavior.

**Validates: Requirements 12.1, 12.2, 12.3, 12.4, 12.5, 12.6**

## Testing Strategy

### Unit Tests
- Model sınıfları için serialization/deserialization testleri
- ViewModel command testleri
- Service method testleri
- Converter testleri

### Integration Tests
- Proje kaydetme/yükleme döngüsü
- TreeView seçim ve özellik bağlama
- İçerik oluşturma ve render akışı
- Efekt uygulama ve animasyon

### Property-Based Tests
- FsCheck veya benzeri kütüphane ile
- Minimum 100 iterasyon per property
- Her test design document property'sine referans verecek
- Tag format: **Feature: maui-ui-redesign, Property {number}: {property_text}**

### UI Tests
- MAUI UI test framework ile
- Temel navigasyon testleri
- Keyboard shortcut testleri
