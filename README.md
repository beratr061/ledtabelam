# LEDTabelam

Otobüs hat tabelaları (güzergah göstergeleri) için bitmap font önizleme uygulaması. HD2018/HD2020 benzeri sistemler için tasarlanmış, cross-platform (Windows, Linux, macOS) masaüstü uygulamasıdır.

## Özellikler

- **999 Slot Yönetimi**: Gerçek otobüs tabela kontrol ünitesi gibi slot bazlı içerik yönetimi
- **Profil Sistemi**: Metrobüs, Belediye Otobüsü, Tramvay gibi farklı sistemler için ayrı profiller
- **BMFont Desteği**: BMFont XML (.fnt) ve JSON format desteği
- **Türkçe Karakter**: Tam Türkçe karakter desteği (ğ, ü, ş, ı, ö, ç, Ğ, Ü, Ş, İ, Ö, Ç)
- **LED Simülasyonu**: Gerçekçi LED görünümü, glow efekti, piksel pitch ayarları
- **Zone/Layout**: Tabela ekranını bölgelere ayırma (logo, metin, hat numarası)
- **Animasyon**: Kayan yazı animasyonu ve playlist desteği
- **Export**: PNG, GIF, WebP formatlarında dışa aktarma

## Gereksinimler

- .NET 8.0 SDK veya Runtime
- Windows 10/11, macOS 11+, veya Linux (Ubuntu 20.04+)
- Minimum 1280x720 ekran çözünürlüğü

## Kurulum

### Windows

```bash
# .NET 8.0 SDK yükleyin (https://dotnet.microsoft.com/download)
# Projeyi klonlayın
git clone <repository-url>
cd LEDTabelam

# Uygulamayı derleyin
dotnet build --configuration Release

# Çalıştırın
dotnet run --project LEDTabelam
```

### Linux (Ubuntu/Debian)

```bash
# .NET 8.0 SDK yükleyin
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Projeyi klonlayın ve derleyin
git clone <repository-url>
cd LEDTabelam
dotnet build --configuration Release
dotnet run --project LEDTabelam
```

### macOS

```bash
# Homebrew ile .NET 8.0 SDK yükleyin
brew install dotnet-sdk

# Projeyi klonlayın ve derleyin
git clone <repository-url>
cd LEDTabelam
dotnet build --configuration Release
dotnet run --project LEDTabelam
```

## Kullanım

### Temel İş Akışı

1. **Profil Seçimi**: Üst menüden profil seçin veya yeni profil oluşturun
2. **Slot Seçimi**: Sol panelden slot numarası girin (001-999)
3. **İçerik Düzenleme**: Hat numarası ve güzergah metnini girin
4. **Ayarlar**: Çözünürlük, LED rengi, font ve görsel ayarları yapın
5. **Önizleme**: Sağ panelde gerçek zamanlı LED önizlemesi görün
6. **Export**: PNG/GIF/WebP olarak kaydedin

### Klavye Kısayolları

| Kısayol | İşlev |
|---------|-------|
| `Ctrl+S` | PNG olarak kaydet |
| `Ctrl+O` | Font yükle |
| `Space` | Animasyon Oynat/Duraklat |
| `Ctrl++` | Yakınlaştır |
| `Ctrl+-` | Uzaklaştır |

### LED Renk Tipleri

- **Amber**: Klasik sarı-turuncu LED (#FFB000)
- **Kırmızı**: Kırmızı LED (#FF0000)
- **Yeşil**: Yeşil LED (#00FF00)
- **1R1G1B**: Basit RGB karışımı
- **Full RGB**: Tam renk desteği

### Piksel Pitch Değerleri

| Pitch | Açıklama |
|-------|----------|
| P2.5 | 2.5mm LED aralığı |
| P3 | 3mm LED aralığı |
| P4 | 4mm LED aralığı |
| P5 | 5mm LED aralığı |
| P6 | 6mm LED aralığı |
| P7.62 | 7.62mm LED aralığı |
| P10 | 10mm LED aralığı (varsayılan) |

### Zone Yönetimi

Tabela ekranını dikey bölgelere ayırabilirsiniz:
- Sol bölge: Logo/ikon (%15)
- Orta bölge: Güzergah metni (%70)
- Sağ bölge: Hat numarası (%15)

## Geliştirme

### Proje Yapısı

```
LEDTabelam/
├── Assets/
│   ├── Fonts/          # BMFont dosyaları
│   ├── Icons/          # SVG ikonlar (16px, 32px)
│   └── Strings/        # Yerelleştirme dosyaları
├── Models/             # Veri modelleri
├── Services/           # İş mantığı servisleri
├── ViewModels/         # MVVM ViewModels
├── Views/              # Avalonia AXAML dosyaları
└── Program.cs          # Uygulama giriş noktası

LEDTabelam.Tests/       # Birim ve property testleri
```

### Testleri Çalıştırma

```bash
# Tüm testleri çalıştır
dotnet test

# Belirli bir test dosyasını çalıştır
dotnet test --filter "FullyQualifiedName~FontLoaderPropertyTests"

# Detaylı çıktı ile
dotnet test --verbosity normal
```

### Yeni Font Ekleme

1. BMFont formatında font oluşturun (BMFont, Hiero, vb. araçlarla)
2. `.fnt` ve `.png` dosyalarını `Assets/Fonts/` klasörüne kopyalayın
3. Türkçe karakterleri (ğüşıöçĞÜŞİÖÇ) font'a eklemeyi unutmayın

### BMFont Format Örneği

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
  </chars>
</font>
```

## Mimari

Uygulama MVVM (Model-View-ViewModel) mimarisi kullanır:

- **Avalonia UI**: Cross-platform UI framework
- **ReactiveUI**: Reaktif MVVM desteği
- **SkiaSharp**: Performanslı 2D grafik render
- **System.Text.Json**: JSON serialization

### Servisler

| Servis | Açıklama |
|--------|----------|
| `FontLoader` | BMFont/JSON font yükleme ve parse |
| `LedRenderer` | LED piksel render ve efektler |
| `ProfileManager` | Profil CRUD operasyonları |
| `SlotManager` | 999 slot yönetimi |
| `ZoneManager` | Bölge/layout yönetimi |
| `AnimationService` | Kayan yazı animasyonu |
| `ExportService` | PNG/GIF/WebP export |
| `SvgRenderer` | SVG ve bitmap render |

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Değişikliklerinizi commit edin (`git commit -am 'Yeni özellik eklendi'`)
4. Branch'i push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request açın

## Destek

Sorularınız veya sorunlarınız için GitHub Issues kullanın.
