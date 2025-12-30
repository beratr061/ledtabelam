<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8.0"/>
  <img src="https://img.shields.io/badge/Avalonia-11.0-8B44AC?style=for-the-badge&logo=avalonia&logoColor=white" alt="Avalonia"/>
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-blue?style=for-the-badge" alt="Platform"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License"/>
</p>

<h1 align="center">🚌 LEDTabelam</h1>

<p align="center">
  <strong>Otobüs Hat Tabelaları için Profesyonel LED Simülasyon Aracı</strong>
</p>

<p align="center">
  HD2018/HD2020 benzeri sistemler için tasarlanmış, gerçek zamanlı LED önizleme ve animasyon uygulaması
</p>

---

## ✨ Öne Çıkan Özellikler

<table>
<tr>
<td width="50%">

### 🎯 Slot Yönetimi
999 slot kapasiteli, gerçek tabela kontrol ünitesi gibi içerik yönetimi

### 🎨 LED Simülasyonu
Gerçekçi LED görünümü, glow efekti ve ayarlanabilir piksel pitch değerleri

### 🔤 Font Desteği
BMFont XML/JSON formatları ve tam Türkçe karakter desteği

</td>
<td width="50%">

### 📁 Profil Sistemi
Metrobüs, Belediye Otobüsü, Tramvay için ayrı profiller

### 🎬 Animasyon
Kayan yazı, geçiş efektleri ve playlist desteği

### 📤 Export
PNG, GIF, WebP formatlarında yüksek kaliteli dışa aktarma

</td>
</tr>
</table>

---

## 🚀 Hızlı Başlangıç

### Gereksinimler

| Bileşen | Minimum |
|---------|---------|
| .NET SDK | 8.0+ |
| İşletim Sistemi | Windows 10+, macOS 11+, Ubuntu 20.04+ |
| Ekran | 1280x720 |

### Kurulum

```bash
# Projeyi klonlayın
git clone https://github.com/beratr061/ledtabelam.git
cd LEDTabelam

# Derleyin ve çalıştırın
dotnet build --configuration Release
dotnet run --project LEDTabelam
```

---

## 🎮 Kullanım

### Temel İş Akışı

```
1️⃣ Profil Seç    →    2️⃣ Slot Gir (001-999)    →    3️⃣ İçerik Düzenle
                                ↓
4️⃣ Export        ←    5️⃣ Önizle               ←    6️⃣ Ayarları Yap
```

### ⌨️ Klavye Kısayolları

| Kısayol | İşlev |
|:-------:|-------|
| `Ctrl+S` | PNG olarak kaydet |
| `Ctrl+O` | Font yükle |
| `Space` | Animasyon Oynat/Duraklat |
| `Ctrl++` | Yakınlaştır |
| `Ctrl+-` | Uzaklaştır |

---

## 🎨 LED Renk Seçenekleri

| Renk | Hex | Kullanım |
|------|-----|----------|
| 🟡 Amber | `#FFB000` | Klasik tabela görünümü |
| 🔴 Kırmızı | `#FF0000` | Uyarı/acil durum |
| 🟢 Yeşil | `#00FF00` | Alternatif görünüm |
| 🌈 Full RGB | - | Tam renk desteği |

---

## 📐 Piksel Pitch Değerleri

```
P2.5 ━━━ P3 ━━━ P4 ━━━ P5 ━━━ P6 ━━━ P7.62 ━━━ P10
 ↑                                              ↑
Yüksek                                        Düşük
Çözünürlük                                  Çözünürlük
```

---

## 🏗️ Proje Yapısı

```
LEDTabelam/
├── 📁 Assets/
│   ├── 🔤 Fonts/          # BMFont dosyaları
│   ├── 🖼️ Icons/          # Piskel C formatında ikonlar
│   └── 🌐 Strings/        # Yerelleştirme
├── 📁 Models/             # Veri modelleri
├── 📁 Services/           # İş mantığı
│   ├── FontLoader.cs      # Font yükleme
│   ├── LedRenderer.cs     # LED render
│   ├── ProfileManager.cs  # Profil yönetimi
│   ├── PiskelCParser.cs   # Piskel C parser
│   └── ...
├── 📁 ViewModels/         # MVVM ViewModels
├── 📁 Views/              # Avalonia AXAML
└── Program.cs

LEDTabelam.Tests/          # Test projesi
```

---

## 🧪 Testler

```bash
# Tüm testleri çalıştır
dotnet test

# Belirli test
dotnet test --filter "FontLoaderPropertyTests"

# Detaylı çıktı
dotnet test --verbosity normal
```

---

## 🛠️ Teknoloji Stack

<p align="center">
  <img src="https://img.shields.io/badge/Avalonia_UI-8B44AC?style=flat-square&logo=avalonia&logoColor=white" alt="Avalonia"/>
  <img src="https://img.shields.io/badge/ReactiveUI-B7178C?style=flat-square&logo=reactivex&logoColor=white" alt="ReactiveUI"/>
  <img src="https://img.shields.io/badge/SkiaSharp-0078D4?style=flat-square&logo=skia&logoColor=white" alt="SkiaSharp"/>
  <img src="https://img.shields.io/badge/xUnit-512BD4?style=flat-square&logo=xunit&logoColor=white" alt="xUnit"/>
</p>

| Teknoloji | Kullanım |
|-----------|----------|
| **Avalonia UI** | Cross-platform UI framework |
| **ReactiveUI** | Reaktif MVVM desteği |
| **SkiaSharp** | 2D grafik render |
| **System.Text.Json** | JSON serialization |

---

## 🤝 Katkıda Bulunma

```bash
# 1. Fork yapın
# 2. Feature branch oluşturun
git checkout -b feature/yeni-ozellik

# 3. Commit edin
git commit -am 'Yeni özellik eklendi'

# 4. Push edin
git push origin feature/yeni-ozellik

# 5. Pull Request açın
```

---

## 📄 Lisans

Bu proje **MIT** lisansı altında lisanslanmıştır.

---

<p align="center">
  <sub>Made with ❤️ for public transportation</sub>
</p>
