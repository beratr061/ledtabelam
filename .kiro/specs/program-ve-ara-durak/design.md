# Design Document: Program ve Ara Durak Sistemi

## Overview

Bu tasarım, LED tabela uygulamasına gerçek otobüs tabelalarındaki gibi program bazlı içerik yönetimi ve ara durak sistemi ekler. Mevcut mimari üzerine inşa edilecek ve Profile → Programs → Items hiyerarşisi oluşturulacaktır.

Temel konsept: Her "Program" bir PowerPoint slaytı gibidir. Her programın kendi öğeleri (TabelaItem), süresi ve geçiş ayarları vardır. Programlar sırayla oynatılır ve döngü halinde devam eder.

## Architecture

### Mevcut Yapı
```
Profile
├── Settings (DisplaySettings)
├── DefaultZones (List<Zone>)
└── Slots (Dictionary<int, TabelaSlot>)
```

### Yeni Yapı
```
Profile
├── Settings (DisplaySettings)
├── Programs (ObservableCollection<TabelaProgram>)  // YENİ
│   ├── Program 1
│   │   ├── Name, Duration, Transition
│   │   └── Items (ObservableCollection<TabelaItem>)
│   │       └── TabelaItem
│   │           └── IntermediateStops (List<IntermediateStop>)  // YENİ
│   └── Program 2
│       └── ...
└── Slots (Dictionary<int, TabelaSlot>)
```

### Sequencer Mimarisi

İki seviyeli zamanlayıcı sistemi:

```
┌─────────────────────────────────────────────────────────┐
│                    ProgramSequencer                      │
│  ┌─────────────────────────────────────────────────┐    │
│  │ Global Timer (Program Geçişi)                    │    │
│  │ - CurrentProgramIndex                            │    │
│  │ - ProgramElapsedTime                             │    │
│  │ - OnProgramComplete → NextProgram()              │    │
│  └─────────────────────────────────────────────────┘    │
│                                                          │
│  ┌─────────────────────────────────────────────────┐    │
│  │ Item Timers (Ara Durak Geçişi)                   │    │
│  │ - Her TabelaItem için ayrı timer                 │    │
│  │ - CurrentStopIndex                               │    │
│  │ - StopElapsedTime                                │    │
│  │ - OnStopComplete → NextStop()                    │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### 1. TabelaProgram (Yeni Model)

```csharp
public class TabelaProgram : ReactiveObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "Program 1";
    public int DurationSeconds { get; set; } = 5;
    public ProgramTransitionType Transition { get; set; } = ProgramTransitionType.Direct;
    public int TransitionDurationMs { get; set; } = 300;
    public ObservableCollection<TabelaItem> Items { get; set; } = new();
    public bool IsActive { get; set; }
}
```

### 2. IntermediateStop (Yeni Model)

```csharp
public class IntermediateStop : ReactiveObject
{
    public int Order { get; set; }
    public string StopName { get; set; } = string.Empty;
}
```

### 3. IntermediateStopSettings (Yeni Model)

```csharp
public class IntermediateStopSettings : ReactiveObject
{
    public bool IsEnabled { get; set; } = false;
    public ObservableCollection<IntermediateStop> Stops { get; set; } = new();
    public double DurationSeconds { get; set; } = 2.0;
    public StopAnimationType Animation { get; set; } = StopAnimationType.Direct;
    public int AnimationDurationMs { get; set; } = 200;
    public bool AutoCalculateDuration { get; set; } = false;
}
```

### 4. Enum Tanımları

```csharp
public enum ProgramTransitionType
{
    Direct,      // Kesme (anında geçiş)
    Fade,        // Solma efekti
    SlideLeft,   // Sola kayma
    SlideRight,  // Sağa kayma
    SlideUp,     // Yukarı kayma
    SlideDown    // Aşağı kayma
}

public enum StopAnimationType
{
    Direct,      // Kesme (anında geçiş)
    Fade,        // Solma efekti
    SlideUp,     // Yukarı kayma
    SlideDown    // Aşağı kayma
}
```

### 5. IProgramSequencer (Yeni Interface)

```csharp
public interface IProgramSequencer
{
    // State
    int CurrentProgramIndex { get; }
    TabelaProgram? CurrentProgram { get; }
    bool IsPlaying { get; }
    bool IsLooping { get; set; }
    double ProgramElapsedTime { get; }
    
    // Program koleksiyonu
    ObservableCollection<TabelaProgram> Programs { get; }
    
    // Kontrol
    void Play();
    void Pause();
    void Stop();
    void NextProgram();
    void PreviousProgram();
    void GoToProgram(int index);
    
    // Tick (AnimationService'den çağrılır)
    void OnTick(double deltaTime);
    
    // Events
    event Action<TabelaProgram>? ProgramChanged;
    event Action<TabelaItem, IntermediateStop>? StopChanged;
}
```

### 6. TabelaItem Güncellemesi

Mevcut TabelaItem modeline eklenecek:

```csharp
// TabelaItem.cs'e eklenecek
private IntermediateStopSettings _intermediateStops = new();

public IntermediateStopSettings IntermediateStops
{
    get => _intermediateStops;
    set => this.RaiseAndSetIfChanged(ref _intermediateStops, value ?? new());
}

// Ara durak aktif mi ve durak var mı
public bool HasIntermediateStops => 
    IntermediateStops.IsEnabled && IntermediateStops.Stops.Count > 0;

// Mevcut gösterilecek içerik (ara durak varsa durak adı, yoksa Content)
public string DisplayContent => 
    HasIntermediateStops ? GetCurrentStopName() : Content;
```

### 7. Profile Güncellemesi

```csharp
// Profile.cs'e eklenecek
private ObservableCollection<TabelaProgram> _programs = new();

public ObservableCollection<TabelaProgram> Programs
{
    get => _programs;
    set => this.RaiseAndSetIfChanged(ref _programs, value ?? new());
}
```

## Data Models

### Program Veri Akışı

```
User Action          →  ViewModel           →  Sequencer           →  Renderer
─────────────────────────────────────────────────────────────────────────────────
"Program Ekle"       →  AddProgram()        →  Programs.Add()      →  -
"Program Seç"        →  SelectProgram()     →  CurrentProgram=     →  Redraw
"Play"               →  Play()              →  IsPlaying=true      →  StartAnimation
"Tick" (60fps)       →  -                   →  OnTick(dt)          →  UpdateDisplay
"Program Süresi Dol" →  -                   →  NextProgram()       →  TransitionEffect
```

### Ara Durak Veri Akışı

```
User Action          →  ViewModel           →  Item State          →  Renderer
─────────────────────────────────────────────────────────────────────────────────
"Ara Durak Ekle"     →  AddStop()           →  Stops.Add()         →  -
"Süre Ayarla"        →  SetDuration()       →  Duration=           →  -
"Play"               →  -                   →  StartStopCycle()    →  -
"Tick" (60fps)       →  -                   →  UpdateStopIndex()   →  UpdateText
"Durak Süresi Dol"   →  -                   →  NextStop()          →  AnimateTransition
```

### Otomatik Süre Hesaplama

Ara durak döngüsünün program süresi içinde tamamlanması için:

```
Formül: durak_süresi = program_süresi / durak_sayısı

Örnek:
- Program süresi: 10 saniye
- Durak sayısı: 5
- Her durak süresi: 10 / 5 = 2 saniye
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Program Koleksiyonu Minimum Boyut Invariantı
*For any* Profile, the Programs collection SHALL always contain at least one program. Attempting to remove the last program SHALL be rejected.
**Validates: Requirements 1.8**

### Property 2: Program ID Benzersizliği
*For any* set of programs in a Profile, all program IDs SHALL be unique. No two programs can have the same ID.
**Validates: Requirements 1.3**

### Property 3: Program Ekleme Koleksiyonu Büyütür
*For any* program collection with N programs, adding a new program SHALL result in a collection with N+1 programs, and the new program SHALL be present in the collection.
**Validates: Requirements 1.1, 1.2, 1.4**

### Property 4: Program Süresi Sınır Kontrolü
*For any* program duration value, it SHALL be clamped to the range [1, 60] seconds. Values outside this range SHALL be adjusted to the nearest valid value.
**Validates: Requirements 2.3**

### Property 5: Program Döngü Davranışı
*For any* program sequence with N programs, when the current program index reaches N-1 and the program duration expires, the next program index SHALL be 0 (loop back to first).
**Validates: Requirements 2.4, 2.6**

### Property 6: Ara Durak Koleksiyonu Yönetimi
*For any* TabelaItem with intermediate stops, adding a stop SHALL increase the collection size by 1, removing a stop SHALL decrease it by 1, and reordering SHALL preserve all stops.
**Validates: Requirements 4.5, 4.7, 4.8**

### Property 7: Ara Durak Süresi Sınır Kontrolü
*For any* intermediate stop duration value, it SHALL be clamped to the range [0.5, 10] seconds.
**Validates: Requirements 5.3**

### Property 8: Ara Durak Döngü Davranışı
*For any* TabelaItem with N intermediate stops, when the current stop index reaches N-1 and the stop duration expires, the next stop index SHALL be 0 (loop back to first).
**Validates: Requirements 5.4, 5.5**

### Property 9: Eşzamanlı Döngü Bağımsızlığı
*For any* playing state, the program timer and intermediate stop timers SHALL operate independently. A program transition SHALL NOT reset intermediate stop timers of items in the new program.
**Validates: Requirements 8.3**

### Property 10: Otomatik Süre Hesaplama
*For any* TabelaItem with AutoCalculateDuration enabled and N stops within a program of D seconds duration, each stop duration SHALL be D/N seconds (±0.01 tolerance for floating point).
**Validates: Requirements 8.4**

### Property 11: Serializasyon Round-Trip
*For any* valid Profile with programs and intermediate stops, serializing to JSON and deserializing back SHALL produce an equivalent Profile with all programs, items, and stops preserved.
**Validates: Requirements 9.3, 9.4**

### Property 12: Geçiş Süresi Sınır Kontrolü
*For any* program transition duration, it SHALL be clamped to [200, 1000] ms. For stop animation duration, it SHALL be clamped to [100, 500] ms.
**Validates: Requirements 3.4, 6.4**

### Property 13: Play/Pause State Değişimi
*For any* sequencer, calling Play() SHALL set IsPlaying to true, and calling Pause() SHALL set IsPlaying to false while preserving the current program index.
**Validates: Requirements 7.2, 7.3**

### Property 14: Non-Loop Mode Davranışı
*For any* sequencer with IsLooping=false and N programs, when reaching program N-1 and duration expires, the sequencer SHALL stop (IsPlaying=false) and remain at program N-1.
**Validates: Requirements 7.6**

## Error Handling

### Program Yönetimi Hataları
- Son program silinmeye çalışıldığında: İşlem reddedilir, kullanıcıya bilgi mesajı gösterilir
- Geçersiz program index'i: Sınırlara clamp edilir (0 veya Programs.Count-1)
- Null program ekleme: ArgumentNullException fırlatılır

### Ara Durak Hataları
- Boş durak adı: Kabul edilir ama uyarı gösterilir
- Geçersiz süre değeri: Sınırlara clamp edilir
- Null stop ekleme: ArgumentNullException fırlatılır

### Serializasyon Hataları
- Bozuk JSON: Varsayılan değerlerle yeni profil oluşturulur
- Eksik alanlar: Varsayılan değerler kullanılır
- Versiyon uyumsuzluğu: Migration stratejisi uygulanır

## Testing Strategy

### Unit Tests
- TabelaProgram model testleri (varsayılan değerler, property değişiklikleri)
- IntermediateStop model testleri
- IntermediateStopSettings model testleri
- Enum değer testleri

### Property-Based Tests (FsCheck veya Hedgehog)
- Minimum 100 iterasyon per property
- Her test, design document property'sine referans verecek
- Tag format: **Feature: program-ve-ara-durak, Property N: [property text]**

### Integration Tests
- ProgramSequencer ile AnimationService entegrasyonu
- Profile serializasyon/deserializasyon
- UI ViewModel binding testleri

### Test Framework
- xUnit test framework
- FsCheck for property-based testing (C# ile uyumlu)
- Moq for mocking (gerekirse)

