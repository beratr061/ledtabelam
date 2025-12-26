# Design Document: LEDTabelam

## Overview

LEDTabelam, otobüs hat tabelaları için bitmap font önizleme uygulamasıdır. Avalonia UI framework kullanılarak cross-platform (Windows, Linux, macOS) çalışacak şekilde tasarlanmıştır. Uygulama MVVM (Model-View-ViewModel) mimarisi ve ReactiveUI kullanarak geliştirilecektir.

### Temel Özellikler
- 999 adet tabela slot yönetimi
- Profil bazlı konfigürasyon (Metrobüs, Belediye Otobüsü, Tramvay vb.)
- BMFont XML ve JSON format desteği
- SkiaSharp ile performanslı LED render
- Zone/Layout bazlı içerik yönetimi
- Animasyon ve playlist desteği
- PNG/GIF/WebP export

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │   MainWindow    │  │  ControlPanel   │  │  PreviewPanel   │  │
│  │    (AXAML)      │  │    (AXAML)      │  │    (AXAML)      │  │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘  │
│           │                    │                    │            │
│  ┌────────┴────────────────────┴────────────────────┴────────┐  │
│  │                    ViewModels (ReactiveUI)                 │  │
│  │  MainWindowViewModel, ControlPanelViewModel, etc.          │  │
│  └────────────────────────────┬──────────────────────────────┘  │
└───────────────────────────────┼──────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────┐
│                        Service Layer                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │ FontLoader   │  │ LedRenderer  │  │ProfileManager│           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │ SlotManager  │  │ ZoneManager  │  │AssetLibrary  │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │AnimationSvc  │  │ ExportService│  │ SvgRenderer  │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└───────────────────────────────┬──────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────┐
│                         Model Layer                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │ BitmapFont   │  │ DisplaySettings│ │   Profile   │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  TabelaSlot  │  │    Zone      │  │ PlaylistItem │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  TextStyle   │  │  LedPixel    │  │   Asset      │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└──────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. Models

#### DisplaySettings
```csharp
public class DisplaySettings : ReactiveObject
{
    public int Width { get; set; } = 128;
    public int Height { get; set; } = 16;
    public LedColorType ColorType { get; set; } = LedColorType.Amber;
    public Color CustomColor { get; set; } = Color.FromArgb(255, 255, 176, 0);
    public int Brightness { get; set; } = 100;
    public int BackgroundDarkness { get; set; } = 100;
    public int PixelSize { get; set; } = 8;
    public PixelPitch Pitch { get; set; } = PixelPitch.P10;
    public double CustomPitchRatio { get; set; } = 0.7;
    public PixelShape Shape { get; set; } = PixelShape.Round;
    public int ZoomLevel { get; set; } = 100;
    public bool InvertColors { get; set; } = false;
    public int AgingPercent { get; set; } = 0;
    public int LineSpacing { get; set; } = 2;
}

public enum LedColorType { Amber, Red, Green, OneROneGOneB, FullRGB }
public enum PixelPitch { P2_5, P3, P4, P5, P6, P7_62, P10, Custom }
public enum PixelShape { Square, Round }
```

#### BitmapFont
```csharp
public class BitmapFont
{
    public string Name { get; set; }
    public string FilePath { get; set; }
    public int LineHeight { get; set; }
    public int Base { get; set; }
    public SKBitmap FontImage { get; set; }
    public Dictionary<int, FontChar> Characters { get; set; }
    public Dictionary<(int, int), int> Kernings { get; set; }
}

public class FontChar
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int XAdvance { get; set; }
}
```

#### TabelaSlot
```csharp
public class TabelaSlot
{
    public int SlotNumber { get; set; }
    public string RouteNumber { get; set; }
    public string RouteText { get; set; }
    public string IconPath { get; set; }
    public List<Zone> Zones { get; set; }
    public TextStyle TextStyle { get; set; }
    public HorizontalAlignment HAlign { get; set; }
    public VerticalAlignment VAlign { get; set; }
}
```

#### Profile
```csharp
public class Profile
{
    public string Name { get; set; }
    public DisplaySettings Settings { get; set; }
    public string FontName { get; set; }
    public List<Zone> DefaultZones { get; set; }
    public Dictionary<int, TabelaSlot> Slots { get; set; } // 1-999
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
```

#### Zone
```csharp
public class Zone
{
    public int Index { get; set; }
    public double WidthPercent { get; set; }
    public ZoneContentType ContentType { get; set; }
    public string Content { get; set; }
    public HorizontalAlignment HAlign { get; set; }
    public VerticalAlignment VAlign { get; set; }
    public bool IsScrolling { get; set; }
    public int ScrollSpeed { get; set; }
}

public enum ZoneContentType { Text, Image, ScrollingText }
```

#### TextStyle
```csharp
public class TextStyle
{
    public bool HasBackground { get; set; } = false;
    public Color BackgroundColor { get; set; }
    public bool HasStroke { get; set; } = false;
    public int StrokeWidth { get; set; } = 1;
    public Color StrokeColor { get; set; }
}
```

#### PlaylistItem
```csharp
public class PlaylistItem
{
    public int Order { get; set; }
    public string Text { get; set; }
    public int DurationSeconds { get; set; } = 3;
    public TransitionType Transition { get; set; } = TransitionType.Fade;
}

public enum TransitionType { None, Fade, SlideLeft, SlideRight }
```

### 2. Services

#### IFontLoader
```csharp
public interface IFontLoader
{
    Task<BitmapFont> LoadBMFontAsync(string fntPath);
    Task<BitmapFont> LoadJsonFontAsync(string jsonPath);
    bool ValidateFont(BitmapFont font);
    SKBitmap RenderText(BitmapFont font, string text, Color color);
}
```

#### ILedRenderer
```csharp
public interface ILedRenderer
{
    SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings);
    SKBitmap RenderWithGlow(SKBitmap source, DisplaySettings settings);
    void DrawGridOverlay(SKCanvas canvas, DisplaySettings settings);
    void ApplyAgingEffect(bool[,] pixelMatrix, int agingPercent);
}
```

#### IProfileManager
```csharp
public interface IProfileManager
{
    Task<List<Profile>> GetAllProfilesAsync();
    Task<Profile> LoadProfileAsync(string name);
    Task SaveProfileAsync(Profile profile);
    Task DeleteProfileAsync(string name);
    Task<Profile> DuplicateProfileAsync(string sourceName, string newName);
    Task ExportProfileAsync(Profile profile, string filePath);
    Task<Profile> ImportProfileAsync(string filePath);
}
```

#### ISlotManager
```csharp
public interface ISlotManager
{
    TabelaSlot GetSlot(int slotNumber);
    void SetSlot(int slotNumber, TabelaSlot slot);
    List<TabelaSlot> SearchSlots(string query);
    Task ExportSlotsAsync(string filePath);
    Task ImportSlotsAsync(string filePath);
}
```

#### IZoneManager
```csharp
public interface IZoneManager
{
    List<Zone> GetZones();
    void AddZone(Zone zone);
    void RemoveZone(int index);
    void UpdateZoneWidth(int index, double widthPercent);
    void NormalizeZoneWidths();
}
```

#### IAnimationService
```csharp
public interface IAnimationService
{
    void StartScrollAnimation(int speed);
    void StopAnimation();
    void PauseAnimation();
    void ResumeAnimation();
    int CurrentOffset { get; }
    bool IsPlaying { get; }
    event Action<int> OnFrameUpdate;
}
```

#### IExportService
```csharp
public interface IExportService
{
    Task ExportPngAsync(SKBitmap bitmap, string filePath, bool useZoom);
    Task ExportGifAsync(List<SKBitmap> frames, string filePath, int fps);
    Task ExportWebPAsync(List<SKBitmap> frames, string filePath, int fps);
}
```

#### ISvgRenderer
```csharp
public interface ISvgRenderer
{
    SKBitmap RenderSvg(string svgPath, int targetHeight, Color tintColor);
    SKBitmap RenderBitmap(string imagePath, int threshold);
}
```

### 3. ViewModels

#### MainWindowViewModel
```csharp
public class MainWindowViewModel : ViewModelBase
{
    public ControlPanelViewModel ControlPanel { get; }
    public PreviewViewModel Preview { get; }
    public SlotEditorViewModel SlotEditor { get; }
    
    public ReactiveCommand<Unit, Unit> SavePngCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadFontCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleAnimationCommand { get; }
    public ReactiveCommand<int, Unit> ZoomInCommand { get; }
    public ReactiveCommand<int, Unit> ZoomOutCommand { get; }
}
```

#### ControlPanelViewModel
```csharp
public class ControlPanelViewModel : ViewModelBase
{
    // Resolution
    public ObservableCollection<string> Resolutions { get; }
    public string SelectedResolution { get; set; }
    public int CustomWidth { get; set; }
    public int CustomHeight { get; set; }
    
    // Color
    public LedColorType SelectedColorType { get; set; }
    
    // Font
    public ObservableCollection<BitmapFont> Fonts { get; }
    public BitmapFont SelectedFont { get; set; }
    
    // Text
    public string InputText { get; set; }
    
    // Visual Settings
    public int Brightness { get; set; }
    public int BackgroundDarkness { get; set; }
    public int PixelSize { get; set; }
    public PixelPitch SelectedPitch { get; set; }
    public PixelShape SelectedShape { get; set; }
    
    // Profile
    public ObservableCollection<Profile> Profiles { get; }
    public Profile SelectedProfile { get; set; }
    
    // Slot
    public int CurrentSlotNumber { get; set; }
}
```

## Data Models

### Profile JSON Schema
```json
{
  "name": "Metrobüs Tabelaları",
  "settings": {
    "width": 192,
    "height": 16,
    "colorType": "Amber",
    "brightness": 100,
    "pitch": "P10",
    "shape": "Round"
  },
  "fontName": "PixelFont16",
  "defaultZones": [
    { "index": 0, "widthPercent": 15, "contentType": "Image" },
    { "index": 1, "widthPercent": 70, "contentType": "ScrollingText" },
    { "index": 2, "widthPercent": 15, "contentType": "Text" }
  ],
  "slots": {
    "1": {
      "routeNumber": "34",
      "routeText": "Zincirlikuyu - Söğütlüçeşme",
      "zones": []
    },
    "2": {
      "routeNumber": "34A",
      "routeText": "Cevizlibağ - Söğütlüçeşme"
    }
  },
  "createdAt": "2025-01-01T00:00:00Z",
  "modifiedAt": "2025-01-01T00:00:00Z"
}
```

### BMFont XML Format (Desteklenen)
```xml
<?xml version="1.0"?>
<font>
  <info face="PixelFont" size="16" />
  <common lineHeight="16" base="13" scaleW="256" scaleH="256" pages="1" />
  <pages>
    <page id="0" file="PixelFont.png" />
  </pages>
  <chars count="95">
    <char id="65" x="0" y="0" width="8" height="16" xoffset="0" yoffset="0" xadvance="9" />
    <!-- A karakteri -->
  </chars>
  <kernings count="0" />
</font>
```

### JSON Font Format (Alternatif)
```json
{
  "name": "PixelFont",
  "size": 16,
  "lineHeight": 16,
  "base": 13,
  "imageFile": "PixelFont.png",
  "characters": {
    "65": { "x": 0, "y": 0, "width": 8, "height": 16, "xoffset": 0, "yoffset": 0, "xadvance": 9 }
  },
  "kernings": {}
}
```

## Render Pipeline

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Text Input │───▶│ Font Render │───▶│ Pixel Matrix│───▶│ LED Render  │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
                                                                │
                   ┌─────────────┐    ┌─────────────┐           │
                   │ Grid Overlay│◀───│ Glow Effect │◀──────────┘
                   └─────────────┘    └─────────────┘
                          │
                   ┌─────────────┐
                   │   Display   │
                   └─────────────┘
```

### Render Steps:
1. **Text to Bitmap**: FontLoader ile metin bitmap'e dönüştürülür
2. **Bitmap to Matrix**: Bitmap, bool[,] piksel matrisine çevrilir
3. **Apply Effects**: Aging, stroke, background efektleri uygulanır
4. **LED Render**: SkiaSharp ile LED pikselleri çizilir
5. **Glow Effect**: SKImageFilter.CreateBlur ile glow eklenir
6. **Grid Overlay**: Plastik ızgara çizilir
7. **Display**: Final görüntü ekrana basılır



## UI Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│  LEDTabelam                                              [_][□][X]      │
├─────────────────────────────────────────────────────────────────────────┤
│  Profil: [Metrobüs Tabelaları ▼] [Yeni] [Kaydet] [Sil]                 │
├──────────────────────┬──────────────────────────────────────────────────┤
│                      │                                                  │
│  ┌─ Ayarlar ───────┐ │  ┌─ Önizleme ─────────────────────────────────┐ │
│  │                 │ │  │                                             │ │
│  │ Slot: [020 ▲▼]  │ │  │   ●●●● ░░░░ ●●●● ░░░░ ●●●● ░░░░ ●●●●     │ │
│  │ [Ara...]        │ │  │   ░●●● ░░░░ ░●●● ░░░░ ░●●● ░░░░ ░●●●     │ │
│  │                 │ │  │   ░░●● ●●●● ░░●● ●●●● ░░●● ●●●● ░░●●     │ │
│  │ Çözünürlük:     │ │  │                                             │ │
│  │ [128x16     ▼]  │ │  │         19K Sakarya Park - Kampüs           │ │
│  │ G:[128] Y:[16]  │ │  │                                             │ │
│  │                 │ │  └─────────────────────────────────────────────┘ │
│  │ LED Tipi:       │ │                                                  │
│  │ ● Amber         │ │  Zoom: [- ●────────────────● +] %100            │
│  │ ○ Kırmızı       │ │                                                  │
│  │ ○ Yeşil         │ │  ┌─ Slot Düzenleyici ──────────────────────────┐ │
│  │ ○ 1R1G1B        │ │  │                                             │ │
│  │ ○ Full RGB      │ │  │ Hat No: [19K        ]                       │ │
│  │                 │ │  │                                             │ │
│  │ Pitch:          │ │  │ Güzergah: [Sakarya Park - Kampüs          ] │ │
│  │ [P10        ▼]  │ │  │                                             │ │
│  │                 │ │  │ Hizalama: [Sol ▼] [Orta ▼]                  │ │
│  │ Piksel Şekli:   │ │  │                                             │ │
│  │ ● Yuvarlak      │ │  │ Stil: □ Arkaplan □ Stroke                   │ │
│  │ ○ Kare          │ │  │                                             │ │
│  │                 │ │  │ [Kaydet] [İptal]                            │ │
│  │ Font:           │ │  └─────────────────────────────────────────────┘ │
│  │ [PixelFont  ▼]  │ │                                                  │
│  │ [Yükle...]      │ │  ┌─ Zone Düzenleyici ──────────────────────────┐ │
│  │                 │ │  │ [15%|Logo] [70%|Kayan Yazı] [15%|Hat No]    │ │
│  │ Parlaklık:      │ │  │ [+ Zone Ekle]                               │ │
│  │ ├────────●──┤   │ │  └─────────────────────────────────────────────┘ │
│  │ %100            │ │                                                  │
│  │                 │ │  ┌─ Animasyon ─────────────────────────────────┐ │
│  │ Arka Plan:      │ │  │ [▶ Oynat] [⏸ Duraklat] [⏹ Durdur]          │ │
│  │ ├──────────●┤   │ │  │ Hız: ├────●──────────┤ 20 px/s             │ │
│  │ %100            │ │  └─────────────────────────────────────────────┘ │
│  │                 │ │                                                  │
│  │ Piksel Boyutu:  │ │  ┌─ Dışa Aktar ────────────────────────────────┐ │
│  │ ├────●──────┤   │ │  │ [PNG Kaydet] [GIF Kaydet] [WebP Kaydet]     │ │
│  │ 8 px             │ │  └─────────────────────────────────────────────┘ │
│  │                 │ │                                                  │
│  │ □ Ters Renk     │ │                                                  │
│  │ □ Eskime %[0]   │ │                                                  │
│  │                 │ │                                                  │
│  └─────────────────┘ │                                                  │
│                      │                                                  │
└──────────────────────┴──────────────────────────────────────────────────┘
```

## Error Handling

### Font Loading Errors
| Error | Handling |
|-------|----------|
| Dosya bulunamadı | Hata mesajı göster, varsayılan font kullan |
| Geçersiz format | "Desteklenmeyen font formatı" mesajı |
| 10MB üzeri dosya | "Dosya çok büyük (max 10MB)" mesajı |
| Boş karakter seti | "Font karakter içermiyor" uyarısı |
| PNG dosyası eksik | "Font görüntü dosyası bulunamadı" mesajı |

### Profile Errors
| Error | Handling |
|-------|----------|
| Profil bulunamadı | Varsayılan profil oluştur |
| JSON parse hatası | Hata mesajı, yedek profil kullan |
| Yazma izni yok | "Profil kaydedilemedi" mesajı |
| Disk dolu | "Yetersiz disk alanı" mesajı |

### Render Errors
| Error | Handling |
|-------|----------|
| Bellek yetersiz | Çözünürlüğü düşür, uyarı göster |
| GPU hatası | Software rendering'e geç |
| Karakter bulunamadı | Placeholder karakter (□) göster |

## Testing Strategy

### Unit Tests
- Model sınıfları için serialization/deserialization testleri
- FontLoader için BMFont XML ve JSON parse testleri
- SlotManager için CRUD operasyon testleri
- ZoneManager için width normalization testleri

### Integration Tests
- Profil kaydetme/yükleme döngüsü
- Font yükleme ve metin render akışı
- Export işlemleri (PNG, GIF, WebP)

### UI Tests
- Keyboard shortcut testleri
- Responsive layout testleri
- Theme/color testleri

### Property-Based Tests
Aşağıdaki bölümde detaylandırılmıştır.



## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Font Round-Trip Consistency
*For any* valid BitmapFont object, serializing to BMFont XML or JSON format and then parsing back should produce an equivalent BitmapFont object with identical character mappings, metadata, and kerning information.

**Validates: Requirements 4.4, 4.5, 4.6**

### Property 2: Profile Round-Trip Consistency
*For any* valid Profile object containing display settings, font configuration, zone layouts, and up to 999 slots, saving to JSON and loading back should produce an equivalent Profile with all data preserved.

**Validates: Requirements 9.1, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8**

### Property 3: Slot Round-Trip Consistency
*For any* valid TabelaSlot object with route number, route text, zones, and text style, saving and loading should preserve all slot data exactly.

**Validates: Requirements 20.1, 20.2, 20.5, 20.10**

### Property 4: Resolution Bounds Validation
*For any* resolution value within 1-512 range for both width and height, the system should accept the value and the LED_Display should resize to match exactly. *For any* value outside this range, the system should reject it and maintain the previous valid value.

**Validates: Requirements 1.2, 1.5, 1.6**

### Property 5: Zone Width Normalization
*For any* set of zones with arbitrary width percentages, after normalization the sum of all zone widths should equal exactly 100%. Additionally, removing or adding a zone should trigger re-normalization maintaining the 100% total.

**Validates: Requirements 17.1, 17.2, 17.4**

### Property 6: Turkish Character Rendering
*For any* string containing Turkish special characters (ğ, ü, ş, ı, ö, ç, Ğ, Ü, Ş, İ, Ö, Ç), if the loaded font contains these characters, the rendered output should contain exactly the same number of character glyphs as the input string length.

**Validates: Requirements 3.2, 3.3, 4.8**

### Property 7: Single Color Mode Consistency
*For any* single color mode (Amber, Red, Green) and any pixel matrix, all active (lit) pixels in the rendered output should have the exact same color value corresponding to the selected mode.

**Validates: Requirements 2.2, 2.5**

### Property 8: Brightness Affects All Pixels Uniformly
*For any* brightness value between 0-100 and any rendered LED display, all active pixels should have their intensity scaled by the same factor. A brightness of 0 should result in no visible pixels, and 100 should show full intensity.

**Validates: Requirements 5.1, 5.2**

### Property 9: Pixel Pitch Determines Spacing
*For any* pixel pitch value (P2.5 through P10 or custom), the ratio of LED diameter to center-to-center distance should match the pitch specification. Changing pitch should not affect the logical pixel matrix, only the visual spacing.

**Validates: Requirements 5.7, 5.8, 5.9**

### Property 10: Zoom Preserves Content
*For any* zoom level between 50-400%, the logical content (which pixels are on/off) should remain unchanged. Only the visual scale should change, and the center point should be preserved during zoom operations.

**Validates: Requirements 6.7, 6.8, 6.9**

### Property 11: Multi-line Text Height Calculation
*For any* multi-line text input and font with known line height, the total rendered height should equal: (number_of_lines * font_line_height) + ((number_of_lines - 1) * line_spacing). If this exceeds display height, a warning should be generated.

**Validates: Requirements 14.1, 14.2, 14.3, 14.4, 14.5**

### Property 12: Playlist Transition Completeness
*For any* playlist with N items and loop mode enabled, after N transitions the display should return to the first item. Each item should be displayed for exactly its specified duration before transitioning.

**Validates: Requirements 15.1, 15.2, 15.3, 15.5, 15.6**

### Property 13: SVG Threshold Binarization
*For any* grayscale or color image and threshold value T (0-100), pixels with brightness >= T should be rendered as "on" and pixels with brightness < T should be "off". Changing threshold should produce a monotonic change in the number of lit pixels.

**Validates: Requirements 16.5, 16.6, 16.7**

### Property 14: Alignment Positioning
*For any* content with known width/height and alignment settings (horizontal: left/center/right, vertical: top/center/bottom), the content position should be calculable and consistent. Center alignment should place content at (container_size - content_size) / 2.

**Validates: Requirements 21.1, 21.2, 21.3, 21.4**

### Property 15: Slot Search Completeness
*For any* search query, the search results should include all slots where the route number OR route text contains the query string (case-insensitive). No matching slot should be excluded from results.

**Validates: Requirements 20.7**

### Property 16: Animation State Machine
*For any* sequence of Play/Pause/Stop commands, the animation state should follow valid transitions: Stopped -> Playing, Playing -> Paused, Paused -> Playing, Playing -> Stopped, Paused -> Stopped. Invalid transitions should be ignored.

**Validates: Requirements 8.1, 8.2, 8.3**

### Property 17: Export Format Validity
*For any* exported PNG file, the file should be a valid PNG that can be opened by standard image libraries. *For any* exported GIF/WebP animation, the file should contain the correct number of frames at the specified FPS.

**Validates: Requirements 7.1, 7.2, 7.5, 7.6, 7.7**

### Property 18: Aging Effect Distribution
*For any* aging percentage P (0-5%), approximately P% of pixels should be affected (dead or dim). The affected pixels should be randomly distributed, and the same seed should produce the same distribution.

**Validates: Requirements 19.3, 19.4, 19.5**

### Property 19: Stroke Expands Glyph Bounds
*For any* text with stroke enabled and stroke width W, the rendered glyph bounds should be expanded by W pixels in all directions compared to the same text without stroke.

**Validates: Requirements 22.4, 22.5, 22.6, 22.7**

