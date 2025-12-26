# LEDTabelam - Kullanım Kılavuzu

## İçindekiler

1. [Giriş](#1-giriş)
2. [Kurulum](#2-kurulum)
3. [Arayüz Tanıtımı](#3-arayüz-tanıtımı)
4. [Profil Yönetimi](#4-profil-yönetimi)
5. [Slot Yönetimi](#5-slot-yönetimi)
6. [Çözünürlük Ayarları](#6-çözünürlük-ayarları)
7. [LED Renk ve Piksel Ayarları](#7-led-renk-ve-piksel-ayarları)
8. [Font Yönetimi](#8-font-yönetimi)
9. [Görsel Ayarlar ve Efektler](#9-görsel-ayarlar-ve-efektler)
10. [Zone (Bölge) Düzenleyici](#10-zone-bölge-düzenleyici)
11. [Animasyon Kontrolleri](#11-animasyon-kontrolleri)
12. [Playlist (Mesaj Listesi)](#12-playlist-mesaj-listesi)
13. [Dışa Aktarma](#13-dışa-aktarma)
14. [Klavye Kısayolları](#14-klavye-kısayolları)
15. [Dahili Varlık Kütüphanesi](#15-dahili-varlık-kütüphanesi)
16. [Sık Sorulan Sorular](#16-sık-sorulan-sorular)

---

## 1. Giriş

### LEDTabelam Nedir?

LEDTabelam, otobüs hat tabelaları (güzergah göstergeleri) için tasarlanmış profesyonel bir bitmap font önizleme uygulamasıdır. HD2018/HD2020 benzeri LED tabela sistemleri için gerçekçi önizleme sağlar.

### Temel Özellikler

- **999 Slot Desteği**: Gerçek tabela kontrol üniteleri gibi 001-999 arası slot yönetimi
- **Profil Sistemi**: Metrobüs, Belediye Otobüsü, Tramvay gibi farklı sistemler için ayrı profiller
- **Çoklu Font Desteği**: BMFont XML (.fnt) ve JSON formatlarında bitmap font desteği
- **Türkçe Karakter Desteği**: ğ, ü, ş, ı, ö, ç ve büyük harfleri tam destekler
- **Gerçekçi LED Simülasyonu**: Glow efekti, piksel aralığı, eskime efekti
- **Zone Sistemi**: Tabela ekranını bölgelere ayırma (logo, metin, hat numarası)
- **Animasyon**: Kayan yazı animasyonu ve playlist desteği
- **Çoklu Format Export**: PNG, GIF ve WebP formatlarında dışa aktarma
- **Cross-Platform**: Windows, macOS ve Linux desteği

---

## 2. Kurulum

### Sistem Gereksinimleri

| Gereksinim | Minimum |
|------------|---------|
| İşletim Sistemi | Windows 10/11, macOS 11+, Ubuntu 20.04+ |
| .NET Runtime | .NET 8.0 |
| Ekran Çözünürlüğü | 1280x720 |
| RAM | 4 GB |
| Disk Alanı | 100 MB |

### Kurulum Adımları

#### Windows
1. .NET 8.0 Runtime'ı [Microsoft'un resmi sitesinden](https://dotnet.microsoft.com/download/dotnet/8.0) indirin ve kurun
2. LEDTabelam uygulamasını indirin
3. Uygulamayı çalıştırın

#### macOS / Linux
```bash
# .NET 8.0 kurulumu (örnek: Ubuntu)
sudo apt-get update
sudo apt-get install -y dotnet-runtime-8.0

# Uygulamayı çalıştırma
dotnet LEDTabelam.dll
```

---

## 3. Arayüz Tanıtımı

### Ana Pencere Düzeni

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Profil Seçici] [Yeni] [Kaydet] [Sil]     LEDTabelam     [PNG][GIF][WebP]│
├──────────────────────┬──────────────────────────────────────────────────┤
│                      │                                                  │
│   KONTROL PANELİ     │              ÖNİZLEME ALANI                     │
│                      │                                                  │
│   • Slot Seçici      │   ┌─────────────────────────────────────────┐   │
│   • Çözünürlük       │   │                                         │   │
│   • LED Tipi         │   │         LED TABELA GÖRÜNTÜLEMESİ        │   │
│   • Piksel Ayarları  │   │                                         │   │
│   • Font             │   └─────────────────────────────────────────┘   │
│   • Görsel Ayarlar   │                                                  │
│   • Efektler         │   [Zoom Kontrolleri: - ────────── + %100]       │
│   • Metin Girişi     │                                                  │
│   • Animasyon Hızı   ├──────────────────────────────────────────────────┤
│                      │  [Slot Düzenleyici][Zone][Animasyon][Export][Playlist]│
│                      │                                                  │
└──────────────────────┴──────────────────────────────────────────────────┘
│  Durum Çubuğu: Hazır                    Çözünürlük: 128    Zoom: %100  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Bölümler

| Bölüm | Açıklama |
|-------|----------|
| **Üst Toolbar** | Profil yönetimi ve hızlı dışa aktarma butonları |
| **Sol Panel** | Tüm ayar kontrolleri (kaydırılabilir) |
| **Önizleme Alanı** | LED tabela gerçek zamanlı önizlemesi |
| **Alt Sekmeler** | Slot düzenleyici, Zone, Animasyon, Export, Playlist |
| **Durum Çubuğu** | Mevcut durum, çözünürlük ve zoom bilgisi |

---

## 4. Profil Yönetimi

### Profil Nedir?

Profil, belirli bir tabela sistemi için tüm ayarları (çözünürlük, renk, font, slotlar) içeren bir konfigürasyon paketidir.

### Örnek Profiller

- **Metrobüs Tabelaları**: 192x16, Amber LED, P10 pitch
- **Belediye Otobüsü**: 128x16, Amber LED, P7.62 pitch
- **Tramvay**: 160x32, Full RGB, P5 pitch

### Profil İşlemleri

#### Yeni Profil Oluşturma
1. Üst toolbar'daki **"Yeni"** butonuna tıklayın
2. Profil adını girin (örn: "Metrobüs Tabelaları")
3. Ayarları yapılandırın
4. **"Kaydet"** butonuna tıklayın

#### Profil Seçme
1. Üst toolbar'daki profil açılır listesinden istediğiniz profili seçin
2. Tüm ayarlar ve slotlar otomatik olarak yüklenir

#### Profil Kaydetme
- **"Kaydet"** butonuna tıklayın veya **Ctrl+S** kısayolunu kullanın
- Tüm değişiklikler (ayarlar, slotlar, zone'lar) kaydedilir

#### Profil Silme
1. Silmek istediğiniz profili seçin
2. **"Sil"** butonuna tıklayın
3. Onay iletişim kutusunda **"Evet"** seçin

### Profil Dosya Yapısı

Profiller JSON formatında kaydedilir:

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
  "slots": {
    "1": {
      "routeNumber": "34",
      "routeText": "Zincirlikuyu - Söğütlüçeşme"
    }
  }
}
```

---

## 5. Slot Yönetimi

### Slot Sistemi

LEDTabelam, gerçek tabela kontrol üniteleri gibi 001-999 arası numaralı slot sistemi kullanır. Her slot bir hat/güzergah tanımı içerir.

### Slot Seçme

1. **Kontrol Paneli > Slot** bölümünde NumericUpDown kontrolünü kullanın
2. Slot numarasını girin (001-999)
3. Veya arama kutusuna hat numarası/güzergah adı yazarak arayın

### Slot Düzenleme

**Slot Düzenleyici** sekmesinde:

| Alan | Açıklama | Örnek |
|------|----------|-------|
| **Hat No** | Hat numarası | 34, 19K, M1 |
| **Güzergah** | Güzergah metni | Zincirlikuyu - Söğütlüçeşme |
| **İkon** | Opsiyonel logo/ikon | türk_bayrağı.svg |

### Hizalama Ayarları

| Yatay Hizalama | Dikey Hizalama |
|----------------|----------------|
| Sol | Üst |
| Orta (varsayılan) | Orta (varsayılan) |
| Sağ | Alt |

### Metin Stilleri

- **Arkaplan**: Metin arkasına dolgu rengi ekler
- **Stroke**: Metin etrafına kontur çizer (1-3 piksel kalınlık)

### Slot İşlemleri

| Buton | İşlev |
|-------|-------|
| **Kaydet** | Slot değişikliklerini kaydeder |
| **İptal** | Değişiklikleri geri alır |
| **Sil** | Slot tanımını siler |

---

## 6. Çözünürlük Ayarları

### Standart Çözünürlükler

| Çözünürlük | Kullanım Alanı |
|------------|----------------|
| 80x16 | Küçük yan tabelalar |
| 96x16 | Kompakt ön tabelalar |
| **128x16** | Standart ön tabela (varsayılan) |
| 160x16 | Geniş ön tabela |
| 192x16 | Metrobüs tabelaları |
| 192x32 | Çift satır tabelalar |
| 256x64 | Büyük bilgi panoları |

### Özel Çözünürlük

1. Çözünürlük açılır listesinden **"Özel"** seçin
2. Genişlik ve yükseklik değerlerini girin
3. Geçerli aralık: **1-512 piksel**

### Çoklu Satır Desteği

- Yükseklik 16'dan fazla olduğunda çoklu satır girişi aktif olur
- Satır arası boşluk ayarlanabilir (varsayılan: 2 piksel)
- Metin yüksekliği display yüksekliğini aşarsa uyarı gösterilir

---

## 7. LED Renk ve Piksel Ayarları

### LED Renk Tipleri

| Tip | Renk Kodu | Açıklama |
|-----|-----------|----------|
| **Amber** | #FFB000 | En yaygın tabela rengi (varsayılan) |
| **Kırmızı** | #FF0000 | Uyarı/acil durum tabelaları |
| **Yeşil** | #00FF00 | Bilgi tabelaları |
| **1R1G1B** | - | Basit RGB karışımı |
| **Full RGB** | - | Tam renk desteği |

### Piksel Pitch (Aralık) Değerleri

Pitch, LED'ler arası merkez-merkez mesafesini belirtir:

| Pitch | Mesafe | Kullanım |
|-------|--------|----------|
| P2.5 | 2.5mm | Yüksek çözünürlük iç mekan |
| P3 | 3mm | İç mekan |
| P4 | 4mm | İç/dış mekan |
| P5 | 5mm | Dış mekan |
| P6 | 6mm | Dış mekan |
| P7.62 | 7.62mm | Otobüs tabelaları |
| **P10** | 10mm | Standart dış mekan (varsayılan) |
| Özel | - | Manuel oran girişi |

### Piksel Şekli

| Şekil | Açıklama |
|-------|----------|
| **Yuvarlak** | Modern SMD/DIP LED'ler (varsayılan) |
| Kare | Eski tip LED matrisler |

---

## 8. Font Yönetimi

### Desteklenen Font Formatları

#### BMFont XML (.fnt)
```xml
<?xml version="1.0"?>
<font>
  <info face="PixelFont" size="16" />
  <common lineHeight="16" base="13" />
  <pages>
    <page id="0" file="PixelFont.png" />
  </pages>
  <chars count="95">
    <char id="65" x="0" y="0" width="8" height="16" 
          xoffset="0" yoffset="0" xadvance="9" />
  </chars>
</font>
```

#### JSON Format (.json)
```json
{
  "name": "PixelFont",
  "size": 16,
  "lineHeight": 16,
  "imageFile": "PixelFont.png",
  "characters": {
    "65": { "x": 0, "y": 0, "width": 8, "height": 16, "xadvance": 9 }
  }
}
```

### Font Yükleme

1. **Kontrol Paneli > Font** bölümünde **"Font Yükle..."** butonuna tıklayın
2. .fnt veya .json dosyasını seçin
3. PNG görüntü dosyası aynı klasörde olmalıdır
4. Font otomatik olarak listeye eklenir

### Font Gereksinimleri

| Gereksinim | Değer |
|------------|-------|
| Maksimum dosya boyutu | 10 MB |
| Desteklenen karakterler | ASCII + Türkçe (ğüşıöçĞÜŞİÖÇ) |
| Görüntü formatı | PNG |

### Dahili Fontlar

Uygulama ile birlikte gelen fontlar:
- **PixelFont8**: 8 piksel yüksekliğinde kompakt font
- **PolarisRGB6x10M**: 6x10 piksel RGB font

---

## 9. Görsel Ayarlar ve Efektler

### Parlaklık

- **Aralık**: %0 - %100
- **Varsayılan**: %100
- LED'lerin yoğunluğunu kontrol eder
- %0 = tamamen sönük, %100 = tam parlaklık

### Arka Plan Karartma

- **Aralık**: %0 - %100
- **Varsayılan**: %100
- Arka plan rengini #000000 (siyah) ile #0a0a0a arasında ayarlar
- Gerçekçi tabela görünümü için %100 önerilir

### Piksel Boyutu

- **Aralık**: 1 - 20 piksel
- **Varsayılan**: 8 piksel
- Önizlemedeki LED boyutunu belirler
- Zoom ile birlikte çalışır

### Satır Arası Boşluk

- **Aralık**: 0 - 10 piksel
- **Varsayılan**: 2 piksel
- Çoklu satır metinlerde satırlar arası mesafe

### Ters Renk Modu

- Aktif olduğunda arka plan aydınlanır, yazılar söner
- Negatif görüntü efekti oluşturur

### Eskime Efekti

- **Aralık**: %0 - %5
- **Varsayılan**: %0 (kapalı)
- Rastgele ölü/sönük piksel simülasyonu
- Gerçekçi yaşlanmış tabela görünümü

---

## 10. Zone (Bölge) Düzenleyici

### Zone Sistemi

Zone sistemi, tabela ekranını dikey çizgilerle bölgelere ayırmanızı sağlar. Her zone farklı içerik tipine sahip olabilir.

### Tipik Zone Düzeni

```
┌──────────┬────────────────────────────────────┬──────────┐
│   %15    │              %70                   │   %15    │
│   Logo   │          Kayan Yazı                │  Hat No  │
└──────────┴────────────────────────────────────┴──────────┘
```

### Zone Ekleme

1. **Zone Düzenleyici** sekmesine gidin
2. **"+ Zone Ekle"** butonuna tıklayın
3. Yeni zone varsayılan ayarlarla eklenir

### Zone Ayarları

| Ayar | Açıklama |
|------|----------|
| **Genişlik (%)** | Zone'un toplam genişlik içindeki yüzdesi |
| **İçerik Tipi** | Text, Image veya ScrollingText |
| **Yatay Hizalama** | Sol, Orta, Sağ |
| **Dikey Hizalama** | Üst, Orta, Alt |
| **Kayan** | Kayan yazı modu (Text tipi için) |

### İçerik Tipleri

| Tip | Açıklama |
|-----|----------|
| **Text** | Sabit metin |
| **Image** | Resim/ikon |
| **ScrollingText** | Kayan yazı |

### Zone Silme

- Her zone kartının sağ üst köşesindeki **"X"** butonuna tıklayın
- Zone silindiğinde diğer zone'ların genişlikleri otomatik normalize edilir

### Genişlik Normalizasyonu

- Tüm zone genişliklerinin toplamı her zaman %100 olmalıdır
- Zone eklendiğinde veya silindiğinde otomatik normalize edilir

---

## 11. Animasyon Kontrolleri

### Kayan Yazı Animasyonu

Metin display genişliğinden uzun olduğunda kayan yazı animasyonu kullanılır.

### Kontrol Butonları

| Buton | İşlev | Kısayol |
|-------|-------|---------|
| **▶ (Oynat)** | Animasyonu başlatır | Space |
| **⏸ (Duraklat)** | Animasyonu duraklatır | Space |
| **⏹ (Durdur)** | Animasyonu durdurur ve sıfırlar | - |

### Hız Ayarı

- **Aralık**: 1 - 100 piksel/saniye
- **Varsayılan**: 20 piksel/saniye
- Düşük değerler = yavaş kayma
- Yüksek değerler = hızlı kayma

### Animasyon Durumu

Animasyon panelinde gösterilen bilgiler:
- Mevcut durum (Oynatılıyor/Duraklatıldı/Durduruldu)
- Scroll offset (piksel cinsinden konum)

---

## 12. Playlist (Mesaj Listesi)

### Playlist Nedir?

Playlist, birden fazla mesajı sırayla göstermenizi sağlar. Otobüs tabelalarındaki döngüsel mesaj akışını simüle eder.

### Mesaj Ekleme

1. **Playlist** sekmesine gidin
2. Alt kısımdaki metin kutusuna mesajı yazın
3. **"Ekle"** butonuna tıklayın

### Mesaj Ayarları

| Ayar | Açıklama | Varsayılan |
|------|----------|------------|
| **Süre** | Mesajın gösterim süresi (saniye) | 3 saniye |
| **Geçiş Efekti** | Mesajlar arası geçiş animasyonu | Fade |

### Geçiş Efektleri

| Efekt | Açıklama |
|-------|----------|
| **None** | Geçiş efekti yok, anında değişim |
| **Fade** | Solma efekti |
| **SlideLeft** | Sola kayma |
| **SlideRight** | Sağa kayma |

### Mesaj Sıralama

- **▲** butonu: Seçili mesajı yukarı taşır
- **▼** butonu: Seçili mesajı aşağı taşır
- **X** butonu: Seçili mesajı siler

### Döngü Modu

- **"Döngü Modu"** checkbox'ı aktif olduğunda playlist sürekli tekrarlar
- Pasif olduğunda son mesajdan sonra durur

### Playlist Oynatma

| Buton | İşlev |
|-------|-------|
| **▶** | Playlist'i başlatır |
| **⏸** | Playlist'i duraklatır |
| **⏹** | Playlist'i durdurur ve başa döner |

---

## 13. Dışa Aktarma

### PNG Kaydetme

1. **Dışa Aktar** sekmesine gidin veya üst toolbar'daki **"PNG"** butonuna tıklayın
2. **"Zoom uygula"** seçeneğini ayarlayın:
   - ✓ Aktif: Ekranda görünen zoom seviyesinde kaydeder
   - ✗ Pasif: Gerçek çözünürlükte kaydeder
3. **"PNG Kaydet"** butonuna tıklayın
4. Dosya konumunu seçin

**Kısayol**: Ctrl+S

### GIF Kaydetme

Animasyonlu önizlemeyi GIF formatında kaydetmek için:

1. **Dışa Aktar** sekmesine gidin
2. **FPS** değerini ayarlayın (1-30, varsayılan: 15)
3. **Süre** değerini ayarlayın (1-30 saniye, varsayılan: 3)
4. **"GIF Kaydet"** butonuna tıklayın

### WebP Kaydetme

GIF'e benzer şekilde, daha iyi sıkıştırma oranı ile:

1. **Dışa Aktar** sekmesine gidin
2. **FPS** ve **Süre** değerlerini ayarlayın
3. **"WebP Kaydet"** butonuna tıklayın

### Format Karşılaştırması

| Format | Avantaj | Dezavantaj |
|--------|---------|------------|
| **PNG** | Kayıpsız, şeffaflık | Animasyon yok |
| **GIF** | Geniş uyumluluk | 256 renk limiti |
| **WebP** | Küçük dosya boyutu | Sınırlı uyumluluk |

---

## 14. Klavye Kısayolları

| Kısayol | İşlev |
|---------|-------|
| **Ctrl+S** | PNG olarak kaydet |
| **Ctrl+O** | Font yükle |
| **Space** | Animasyon Oynat/Duraklat |
| **Ctrl++** | Yakınlaştır |
| **Ctrl+-** | Uzaklaştır |

---

## 15. Dahili Varlık Kütüphanesi

### Dahili İkonlar

Uygulama ile birlikte gelen pixel-perfect ikonlar:

#### Bayraklar
- Türk Bayrağı (16px, 32px)

#### Erişilebilirlik
- Engelli İkonu (16px, 32px)

#### Yön Okları
- Sol Ok
- Sağ Ok
- Yukarı Ok
- Aşağı Ok

#### Ulaşım Sembolleri
- Otobüs
- Metro
- Tramvay

### İkon Kullanımı

1. **Slot Düzenleyici**'de **"..."** butonuna tıklayın
2. Dahili ikonlardan veya dosya sisteminden seçin
3. İkon otomatik olarak seçili LED rengine boyanır

### Özel İkon Ekleme

Desteklenen formatlar:
- **SVG**: Vektör grafik (önerilen)
- **PNG**: Bitmap grafik
- **JPG**: Bitmap grafik

SVG dosyaları matris yüksekliğine kayıpsız ölçeklenir.

---

## 16. Sık Sorulan Sorular

### Genel Sorular

**S: Uygulama hangi platformlarda çalışır?**
C: Windows 10/11, macOS 11+ ve Linux (Ubuntu 20.04+) üzerinde çalışır.

**S: Türkçe karakterler neden görünmüyor?**
C: Yüklü fontun Türkçe karakterleri (ğüşıöçĞÜŞİÖÇ) içerdiğinden emin olun. Dahili fontlar Türkçe desteği içerir.

**S: Maksimum kaç slot tanımlayabilirim?**
C: Her profilde 999 slot tanımlayabilirsiniz (001-999).

### Font Sorunları

**S: Font yüklenmiyor, ne yapmalıyım?**
C: Kontrol edin:
- Dosya boyutu 10MB'dan küçük mü?
- PNG görüntü dosyası aynı klasörde mi?
- Font formatı BMFont XML (.fnt) veya JSON (.json) mu?

**S: Bazı karakterler kutu (□) olarak görünüyor?**
C: Bu karakterler yüklü fontta tanımlı değil. Farklı bir font deneyin veya font dosyasına eksik karakterleri ekleyin.

### Performans Sorunları

**S: Uygulama yavaş çalışıyor?**
C: Deneyin:
- Çözünürlüğü düşürün (maksimum 192x64 önerilir)
- Zoom seviyesini azaltın
- Eskime efektini kapatın

**S: Animasyon takılıyor?**
C: Animasyon hızını düşürün veya çözünürlüğü azaltın.

### Dışa Aktarma Sorunları

**S: GIF dosyası çok büyük?**
C: FPS değerini düşürün veya süreyi kısaltın. WebP formatı daha küçük dosya boyutu sağlar.

**S: PNG şeffaf arka planlı olmuyor?**
C: LED tabela simülasyonu için arka plan her zaman koyu renktedir. Şeffaf arka plan için görüntü düzenleme yazılımı kullanın.

---

## Teknik Destek

### Hata Bildirimi

Hata bildirmek için:
1. Hatanın detaylı açıklamasını yazın
2. Hangi adımları izlediğinizi belirtin
3. Ekran görüntüsü ekleyin (mümkünse)

### Özellik İsteği

Yeni özellik istemek için:
1. Özelliğin ne işe yarayacağını açıklayın
2. Kullanım senaryosunu belirtin

---

## Sürüm Geçmişi

### v1.0.0
- İlk sürüm
- 999 slot desteği
- Profil yönetimi
- BMFont XML ve JSON desteği
- Zone sistemi
- Animasyon ve playlist
- PNG/GIF/WebP export

---

*Bu kılavuz LEDTabelam v1.0.0 için hazırlanmıştır.*
*Son güncelleme: Aralık 2025*


---

## Ek A: Adım Adım Senaryolar

### Senaryo 1: İlk Tabela Oluşturma

**Amaç**: Basit bir otobüs tabelası oluşturmak

1. **Uygulamayı başlatın**
   - Varsayılan profil otomatik yüklenir

2. **Çözünürlüğü ayarlayın**
   - Kontrol Paneli > Çözünürlük > "128x16" seçin

3. **LED tipini seçin**
   - Kontrol Paneli > LED Tipi > "Amber" seçin

4. **Slot numarasını girin**
   - Kontrol Paneli > Slot > "001" girin

5. **Slot bilgilerini doldurun**
   - Slot Düzenleyici sekmesine gidin
   - Hat No: "34"
   - Güzergah: "Zincirlikuyu - Söğütlüçeşme"
   - Kaydet butonuna tıklayın

6. **Önizlemeyi kontrol edin**
   - Önizleme alanında tabelanızı görün
   - Zoom kontrollerini kullanarak yakınlaştırın

7. **PNG olarak kaydedin**
   - Ctrl+S veya PNG butonuna tıklayın
   - Dosya konumunu seçin

---

### Senaryo 2: Metrobüs Profili Oluşturma

**Amaç**: Metrobüs tabelaları için özel profil oluşturmak

1. **Yeni profil oluşturun**
   - Üst toolbar > "Yeni" butonuna tıklayın
   - Ad: "Metrobüs Tabelaları"

2. **Çözünürlüğü ayarlayın**
   - Çözünürlük > "192x16" seçin

3. **Piksel ayarlarını yapın**
   - Pitch > "P10" seçin
   - Piksel Şekli > "Yuvarlak" seçin

4. **Zone'ları yapılandırın**
   - Zone Düzenleyici sekmesine gidin
   - 3 zone ekleyin:
     - Zone 0: %15, Image (logo için)
     - Zone 1: %70, ScrollingText (güzergah için)
     - Zone 2: %15, Text (hat no için)

5. **Profili kaydedin**
   - "Kaydet" butonuna tıklayın

6. **Slotları tanımlayın**
   - Slot 001: 34 - Zincirlikuyu - Söğütlüçeşme
   - Slot 002: 34A - Cevizlibağ - Söğütlüçeşme
   - Slot 003: 34AS - Avcılar - Söğütlüçeşme
   - ...

---

### Senaryo 3: Kayan Yazı Animasyonu

**Amaç**: Uzun güzergah metni için kayan yazı oluşturmak

1. **Uzun metin girin**
   - Kontrol Paneli > Metin alanına yazın:
   - "Zincirlikuyu - Mecidiyeköy - Gayrettepe - Levent - 4.Levent - Söğütlüçeşme"

2. **Animasyon hızını ayarlayın**
   - Kontrol Paneli > Animasyon > Hız: 25 px/s

3. **Animasyonu başlatın**
   - Animasyon sekmesine gidin
   - ▶ (Oynat) butonuna tıklayın
   - Veya Space tuşuna basın

4. **GIF olarak kaydedin**
   - Dışa Aktar sekmesine gidin
   - FPS: 15, Süre: 5 saniye
   - "GIF Kaydet" butonuna tıklayın

---

### Senaryo 4: Çoklu Mesaj Playlist

**Amaç**: Döngüsel mesaj akışı oluşturmak

1. **Playlist sekmesine gidin**

2. **Mesajları ekleyin**
   - "34 Zincirlikuyu" (3 saniye)
   - "Söğütlüçeşme" (3 saniye)
   - "Tüm duraklara" (2 saniye)

3. **Geçiş efektlerini ayarlayın**
   - Her mesaj için "Fade" seçin

4. **Döngü modunu aktifleştirin**
   - "Döngü Modu" checkbox'ını işaretleyin

5. **Playlist'i oynatın**
   - ▶ butonuna tıklayın
   - Mesajlar sırayla gösterilir

---

### Senaryo 5: Logo ve İkon Ekleme

**Amaç**: Tabelaya Türk Bayrağı eklemek

1. **Zone yapısını oluşturun**
   - Zone 0: %15, Image
   - Zone 1: %85, Text

2. **Slot düzenleyiciye gidin**
   - İkon alanında "..." butonuna tıklayın
   - Dahili ikonlardan "Türk Bayrağı" seçin

3. **Önizlemeyi kontrol edin**
   - Bayrak sol tarafta, metin sağ tarafta görünür

---

## Ek B: Sorun Giderme Rehberi

### Sorun: Uygulama Başlamıyor

**Olası Nedenler ve Çözümler:**

| Neden | Çözüm |
|-------|-------|
| .NET 8.0 yüklü değil | .NET 8.0 Runtime'ı yükleyin |
| Eksik DLL dosyaları | Uygulamayı yeniden indirin |
| Yetersiz izinler | Yönetici olarak çalıştırın |

### Sorun: Font Yüklenmiyor

**Kontrol Listesi:**
- [ ] Dosya uzantısı .fnt veya .json mi?
- [ ] PNG dosyası aynı klasörde mi?
- [ ] Dosya boyutu 10MB'dan küçük mü?
- [ ] Font dosyası bozuk değil mi?

**Test Yöntemi:**
1. Dahili fontlardan birini seçin
2. Çalışıyorsa, kendi font dosyanızı kontrol edin

### Sorun: Türkçe Karakterler Görünmüyor

**Çözüm Adımları:**
1. Font dosyasının Türkçe karakterleri içerdiğini doğrulayın
2. BMFont Editor ile font dosyasını açın
3. Türkçe karakterleri (ğüşıöçĞÜŞİÖÇ) ekleyin
4. Fontu yeniden export edin

### Sorun: Animasyon Çalışmıyor

**Kontrol Listesi:**
- [ ] Metin display genişliğinden uzun mu?
- [ ] Animasyon hızı 0'dan büyük mü?
- [ ] Play butonu aktif mi?

### Sorun: Export Başarısız

**Olası Nedenler:**
- Disk alanı yetersiz
- Yazma izni yok
- Dosya başka uygulama tarafından kullanılıyor

**Çözüm:**
1. Farklı bir klasör deneyin
2. Disk alanını kontrol edin
3. Dosyayı kullanan uygulamaları kapatın

---

## Ek C: Font Oluşturma Rehberi

### BMFont ile Font Oluşturma

**Gerekli Yazılım:** [BMFont](https://www.angelcode.com/products/bmfont/)

**Adımlar:**

1. **BMFont'u açın**

2. **Font ayarlarını yapın**
   - Options > Font Settings
   - Font: İstediğiniz piksel fontu seçin
   - Size: 16 (veya istediğiniz boyut)
   - Render from TrueType outline: İşaretli

3. **Karakter setini seçin**
   - Edit > Select chars from file
   - Veya manuel olarak seçin:
     - Latin (32-126)
     - Türkçe: ğüşıöçĞÜŞİÖÇ (Unicode: 286, 252, 351, 305, 246, 231, 287, 220, 350, 304, 214, 199)

4. **Export ayarları**
   - Options > Export Options
   - Bit depth: 32
   - Preset: White text with alpha
   - Font descriptor: XML
   - Textures: PNG

5. **Export edin**
   - Options > Save bitmap font as...
   - .fnt ve .png dosyaları oluşturulur

### JSON Font Formatı

Manuel olarak JSON font oluşturmak için:

```json
{
  "name": "MyPixelFont",
  "size": 16,
  "lineHeight": 16,
  "base": 13,
  "imageFile": "MyPixelFont.png",
  "characters": {
    "32": { "x": 0, "y": 0, "width": 4, "height": 16, "xoffset": 0, "yoffset": 0, "xadvance": 4 },
    "65": { "x": 4, "y": 0, "width": 8, "height": 16, "xoffset": 0, "yoffset": 0, "xadvance": 9 },
    "286": { "x": 12, "y": 0, "width": 8, "height": 16, "xoffset": 0, "yoffset": 0, "xadvance": 9 }
  },
  "kernings": {}
}
```

**Karakter ID'leri (Unicode):**
- A-Z: 65-90
- a-z: 97-122
- 0-9: 48-57
- Ğ: 286, ğ: 287
- Ü: 220, ü: 252
- Ş: 350, ş: 351
- İ: 304, ı: 305
- Ö: 214, ö: 246
- Ç: 199, ç: 231

---

## Ek D: Teknik Referans

### Dosya Formatları

#### Profil Dosyası (.json)
```
Konum: [Uygulama Klasörü]/Profiles/
Format: JSON
Kodlama: UTF-8
```

#### Font Dosyaları
```
Konum: [Uygulama Klasörü]/Assets/Fonts/
Formatlar: .fnt (XML), .json
Görüntü: .png (aynı klasörde)
```

### Renk Kodları

| LED Tipi | RGB | Hex |
|----------|-----|-----|
| Amber | 255, 176, 0 | #FFB000 |
| Kırmızı | 255, 0, 0 | #FF0000 |
| Yeşil | 0, 255, 0 | #00FF00 |

### Pitch Oranları

| Pitch | LED Çapı / Merkez Mesafesi |
|-------|---------------------------|
| P2.5 | 0.8 |
| P3 | 0.75 |
| P4 | 0.7 |
| P5 | 0.7 |
| P6 | 0.7 |
| P7.62 | 0.7 |
| P10 | 0.7 |

### Performans Limitleri

| Parametre | Önerilen Maksimum |
|-----------|-------------------|
| Çözünürlük | 192x64 piksel |
| Font boyutu | 10 MB |
| Slot sayısı | 999 |
| Playlist mesaj | 100 |
| GIF süresi | 30 saniye |
| GIF FPS | 30 |

---

## Ek E: Kısayol Referans Kartı

```
┌─────────────────────────────────────────────────────────────┐
│                    LEDTabelam Kısayolları                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  DOSYA İŞLEMLERİ                                           │
│  ─────────────                                             │
│  Ctrl+S .............. PNG Kaydet                          │
│  Ctrl+O .............. Font Yükle                          │
│                                                             │
│  GÖRÜNTÜLEME                                               │
│  ───────────                                               │
│  Ctrl++ .............. Yakınlaştır                         │
│  Ctrl+- .............. Uzaklaştır                          │
│                                                             │
│  ANİMASYON                                                 │
│  ─────────                                                 │
│  Space ............... Oynat/Duraklat                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

*LEDTabelam - Profesyonel LED Tabela Önizleme Uygulaması*
*© 2025 - Tüm hakları saklıdır.*
