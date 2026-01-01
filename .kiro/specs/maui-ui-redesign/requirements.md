# Requirements Document

## Introduction

LEDTabelam uygulaması, HD2020 yazılımına benzer profesyonel bir arayüze kavuşacak ve Avalonia UI'dan .NET MAUI'ye geçiş yapılacaktır. Bu geçiş, Windows masaüstü odaklı olacak ve mevcut tüm işlevsellik korunarak HD2020 tarzı bir kullanıcı deneyimi sunulacaktır.

## Glossary

- **MAUI_Application**: .NET MAUI framework kullanılarak geliştirilen cross-platform uygulama
- **TreeView_Panel**: Sol tarafta yer alan, Ekran/Program/İçerik hiyerarşisini gösteren ağaç yapısı paneli
- **Preview_Area**: Ortada yer alan, LED tabela önizlemesinin gösterildiği ana alan
- **Properties_Panel**: Sağ tarafta yer alan, seçili öğenin özelliklerini düzenleme paneli
- **Editor_Panel**: Alt tarafta yer alan, metin ve içerik düzenleme alanı
- **Toolbar**: Üst tarafta yer alan, hızlı erişim butonları içeren araç çubuğu
- **Status_Bar**: Alt tarafta yer alan, durum bilgilerini gösteren çubuk
- **Content_Item**: Program içindeki metin, resim veya animasyon öğesi
- **Program_Node**: Bir veya daha fazla içerik öğesi içeren program düğümü
- **Screen_Node**: Bir veya daha fazla program içeren ekran düğümü

## Requirements

### Requirement 1: .NET MAUI Geçişi

**User Story:** As a geliştirici, I want uygulamayı .NET MAUI'ye taşımak, so that modern Windows masaüstü desteği ve gelecekte mobil platform desteği sağlanabilsin.

#### Acceptance Criteria

1. THE MAUI_Application SHALL .NET 8.0 ve MAUI 8.x kullanacak
2. THE MAUI_Application SHALL Windows 10/11 masaüstü platformunu destekleyecek
3. WHEN uygulama başlatıldığında, THE MAUI_Application SHALL mevcut tüm işlevselliği koruyacak
4. THE MAUI_Application SHALL SkiaSharp.Views.Maui ile LED render işlemlerini gerçekleştirecek
5. THE MAUI_Application SHALL CommunityToolkit.Mvvm ile MVVM pattern kullanacak
6. THE MAUI_Application SHALL mevcut model sınıflarını (DisplaySettings, BitmapFont, Profile, vb.) koruyacak
7. THE MAUI_Application SHALL mevcut servis sınıflarını (FontLoader, LedRenderer, ProfileManager, vb.) koruyacak

### Requirement 2: HD2020 Benzeri Ana Pencere Düzeni

**User Story:** As a kullanıcı, I want HD2020 benzeri profesyonel bir arayüz, so that tanıdık ve verimli bir çalışma ortamı elde edebilirim.

#### Acceptance Criteria

1. THE Main_Window SHALL dört ana bölgeden oluşacak: Sol TreeView, Orta Önizleme, Sağ Özellikler, Alt Düzenleyici
2. THE Main_Window SHALL üstte menü çubuğu ve araç çubuğu içerecek
3. THE Main_Window SHALL altta durum çubuğu içerecek
4. THE Main_Window SHALL koyu tema (dark theme) kullanacak
5. WHEN pencere boyutu değiştirildiğinde, THE Main_Window SHALL responsive olarak uyum sağlayacak
6. THE Main_Window SHALL minimum 1280x720 çözünürlükte çalışacak
7. THE Main_Window SHALL panel boyutlarını splitter ile ayarlanabilir yapacak

### Requirement 3: Sol Panel - TreeView Hiyerarşisi

**User Story:** As a kullanıcı, I want ekran, program ve içerik öğelerini ağaç yapısında görmek, so that içerik organizasyonunu kolayca yönetebilirim.

#### Acceptance Criteria

1. THE TreeView_Panel SHALL üç seviyeli hiyerarşi gösterecek: Ekran → Program → İçerik
2. THE TreeView_Panel SHALL her düğüm için ikon gösterecek (klasör, program, metin, resim)
3. WHEN bir düğüm seçildiğinde, THE Properties_Panel SHALL seçili öğenin özelliklerini gösterecek
4. WHEN bir düğüm çift tıklandığında, THE Editor_Panel SHALL düzenleme moduna geçecek
5. THE TreeView_Panel SHALL sürükle-bırak ile öğe sıralama destekleyecek
6. THE TreeView_Panel SHALL sağ tık context menüsü sunacak (Ekle, Sil, Kopyala, Yapıştır)
7. THE TreeView_Panel SHALL düğüm genişletme/daraltma destekleyecek
8. WHEN yeni ekran eklendiğinde, THE System SHALL "Ekran1", "Ekran2" şeklinde otomatik isimlendirme yapacak
9. WHEN yeni program eklendiğinde, THE System SHALL "Program1", "Program2" şeklinde otomatik isimlendirme yapacak

### Requirement 4: Orta Panel - Önizleme Alanı

**User Story:** As a kullanıcı, I want LED tabela önizlemesini merkezi bir alanda görmek, so that tasarımı net bir şekilde değerlendirebilirim.

#### Acceptance Criteria

1. THE Preview_Area SHALL LED tabela önizlemesini merkeze hizalı gösterecek
2. THE Preview_Area SHALL koyu arka plan (#1a1a1a) kullanacak
3. THE Preview_Area SHALL zoom kontrolleri içerecek (slider ve +/- butonları)
4. THE Preview_Area SHALL mevcut program/sayfa numarasını gösterecek (örn: "1/4")
5. THE Preview_Area SHALL önceki/sonraki sayfa navigasyon butonları içerecek
6. WHEN zoom değiştirildiğinde, THE Preview_Area SHALL önizlemeyi merkez noktayı koruyarak ölçekleyecek
7. THE Preview_Area SHALL %50-%400 aralığında zoom destekleyecek
8. THE Preview_Area SHALL tam ekran modu destekleyecek

### Requirement 5: Sağ Panel - Özellikler Paneli

**User Story:** As a kullanıcı, I want seçili öğenin özelliklerini sağ panelde düzenlemek, so that hızlı ayar değişiklikleri yapabilirim.

#### Acceptance Criteria

1. THE Properties_Panel SHALL "Efekt" bölümü içerecek (giriş/çıkış animasyonları)
2. THE Properties_Panel SHALL "Kayış Süresi" bölümü içerecek (hız, süre ayarları)
3. THE Properties_Panel SHALL "Arka Plan" bölümü içerecek (renk, çerçeve ayarları)
4. WHEN metin öğesi seçildiğinde, THE Properties_Panel SHALL metin efektlerini gösterecek
5. WHEN resim öğesi seçildiğinde, THE Properties_Panel SHALL resim efektlerini gösterecek
6. THE Properties_Panel SHALL "Hemen Göster" checkbox'ı içerecek
7. THE Properties_Panel SHALL "Süreli" checkbox'ı ve süre girişi içerecek
8. THE Properties_Panel SHALL "Çerçeve Stili" seçici içerecek
9. THE Properties_Panel SHALL "Özel Çerçeve" seçeneği içerecek

### Requirement 6: Alt Panel - Düzenleyici Alanı

**User Story:** As a kullanıcı, I want metin ve içerik düzenlemeyi alt panelde yapmak, so that geniş bir çalışma alanında düzenleme yapabilirim.

#### Acceptance Criteria

1. THE Editor_Panel SHALL metin girişi için TextBox içerecek
2. THE Editor_Panel SHALL font seçici ComboBox içerecek
3. THE Editor_Panel SHALL font boyutu seçici içerecek
4. THE Editor_Panel SHALL renk seçici içerecek (ön plan ve arka plan)
5. THE Editor_Panel SHALL hizalama butonları içerecek (sol, orta, sağ)
6. THE Editor_Panel SHALL kalın, italik, altı çizili stil butonları içerecek
7. THE Editor_Panel SHALL "Sağ > Sol" (RTL) checkbox'ı içerecek
8. THE Editor_Panel SHALL konum ayarları içerecek (X, Y koordinatları)
9. THE Editor_Panel SHALL boyut ayarları içerecek (Genişlik, Yükseklik)
10. THE Editor_Panel SHALL canlı önizleme gösterecek (mini LED display)
11. WHEN metin değiştirildiğinde, THE Preview_Area SHALL 100ms debounce ile güncellenecek

### Requirement 7: Üst Araç Çubuğu

**User Story:** As a kullanıcı, I want sık kullanılan işlemlere araç çubuğundan erişmek, so that hızlı işlem yapabilirim.

#### Acceptance Criteria

1. THE Toolbar SHALL "Program" butonu içerecek (yeni program oluştur)
2. THE Toolbar SHALL "Metin Yaz" butonu içerecek (yeni metin öğesi)
3. THE Toolbar SHALL "Zaman-Alan" butonu içerecek (zamanlama ayarları)
4. THE Toolbar SHALL "Saat" butonu içerecek (saat gösterimi ekle)
5. THE Toolbar SHALL "Kronmetre" butonu içerecek (geri sayım ekle)
6. THE Toolbar SHALL "Tarih" butonu içerecek (tarih gösterimi ekle)
7. THE Toolbar SHALL "Saat Ayarı" butonu içerecek
8. THE Toolbar SHALL "Yapılan Programı USB'ye Aktar" butonu içerecek
9. THE Toolbar SHALL "Gönder" butonu içerecek
10. THE Toolbar SHALL "Ara" butonu içerecek (içerik arama)
11. THE Toolbar SHALL "Ön İzleme" butonu içerecek
12. THE Toolbar SHALL butonları gruplandırılmış şekilde gösterecek

### Requirement 8: Menü Çubuğu

**User Story:** As a kullanıcı, I want tüm işlemlere menü çubuğundan erişmek, so that kapsamlı kontrol sağlayabilirim.

#### Acceptance Criteria

1. THE Menu_Bar SHALL "Dosya" menüsü içerecek (Yeni, Aç, Kaydet, Farklı Kaydet, Çıkış)
2. THE Menu_Bar SHALL "Ayarlar" menüsü içerecek (Profil, Çözünürlük, LED Tipi)
3. THE Menu_Bar SHALL "Ekle" menüsü içerecek (Ekran, Program, Metin, Resim)
4. THE Menu_Bar SHALL "Yardım" menüsü içerecek (Hakkında, Kullanım Kılavuzu)
5. WHEN "Yeni" seçildiğinde, THE System SHALL yeni proje oluşturacak
6. WHEN "Aç" seçildiğinde, THE System SHALL dosya seçim dialogu açacak
7. WHEN "Kaydet" seçildiğinde, THE System SHALL mevcut projeyi kaydedecek

### Requirement 9: Durum Çubuğu

**User Story:** As a kullanıcı, I want uygulama durumunu alt çubukta görmek, so that mevcut durumu takip edebilirim.

#### Acceptance Criteria

1. THE Status_Bar SHALL mevcut çözünürlüğü gösterecek (örn: "128 x 32")
2. THE Status_Bar SHALL mevcut zoom seviyesini gösterecek (örn: "300%")
3. THE Status_Bar SHALL bağlantı durumunu gösterecek ("Bağlı" / "Çevrimdışı")
4. THE Status_Bar SHALL son işlem mesajını gösterecek
5. THE Status_Bar SHALL koyu tema ile uyumlu renklerde olacak

### Requirement 10: Ekran ve Program Yönetimi

**User Story:** As a kullanıcı, I want birden fazla ekran ve program yönetmek, so that karmaşık tabela senaryoları oluşturabilirim.

#### Acceptance Criteria

1. THE System SHALL birden fazla ekran (Screen) tanımlamaya izin verecek
2. THE Screen_Node SHALL benzersiz isim ve çözünürlük ayarları içerecek
3. THE Program_Node SHALL bir veya daha fazla içerik öğesi içerecek
4. THE Program_Node SHALL sıralı veya paralel içerik gösterimi destekleyecek
5. WHEN program çalıştırıldığında, THE System SHALL içerikleri sırayla gösterecek
6. THE System SHALL program döngüsü (loop) destekleyecek
7. THE System SHALL programlar arası geçiş efektleri destekleyecek

### Requirement 11: İçerik Tipleri

**User Story:** As a kullanıcı, I want farklı içerik tipleri eklemek, so that zengin tabela içerikleri oluşturabilirim.

#### Acceptance Criteria

1. THE System SHALL "Metin Yazı" içerik tipi destekleyecek
2. THE System SHALL "Resim/Video" içerik tipi destekleyecek
3. THE System SHALL "Saat" içerik tipi destekleyecek
4. THE System SHALL "Tarih" içerik tipi destekleyecek
5. THE System SHALL "Geri Sayım" içerik tipi destekleyecek
6. WHEN içerik tipi seçildiğinde, THE Editor_Panel SHALL ilgili düzenleme araçlarını gösterecek
7. THE Content_Item SHALL konum (X, Y) ve boyut (W, H) özellikleri içerecek

### Requirement 12: Animasyon ve Efektler

**User Story:** As a kullanıcı, I want içeriklere animasyon ve efekt eklemek, so that dinamik tabelalar oluşturabilirim.

#### Acceptance Criteria

1. THE System SHALL giriş efektleri destekleyecek (Hemen Göster, Soldan Kayma, Sağdan Kayma, Yukarıdan, Aşağıdan)
2. THE System SHALL çıkış efektleri destekleyecek (Hemen Kapat, Soldan Çık, Sağdan Çık)
3. THE System SHALL kayan yazı efekti destekleyecek
4. THE System SHALL efekt hızı ayarı sunacak
5. THE System SHALL efekt süresi ayarı sunacak
6. WHEN efekt seçildiğinde, THE Preview_Area SHALL efekti önizleyecek

### Requirement 13: Mevcut İşlevsellik Korunması

**User Story:** As a kullanıcı, I want mevcut tüm özelliklerin çalışmaya devam etmesini, so that geçiş sürecinde işlevsellik kaybı yaşamayayım.

#### Acceptance Criteria

1. THE System SHALL mevcut 999 slot yönetimini koruyacak
2. THE System SHALL mevcut profil yönetimini koruyacak
3. THE System SHALL mevcut font yükleme/render işlevini koruyacak
4. THE System SHALL mevcut LED render işlevini koruyacak
5. THE System SHALL mevcut PNG/GIF/WebP export işlevini koruyacak
6. THE System SHALL mevcut zone yönetimini koruyacak
7. THE System SHALL mevcut playlist işlevini koruyacak
8. THE System SHALL mevcut hizalama ve stil özelliklerini koruyacak

### Requirement 14: Tema ve Görsel Tasarım

**User Story:** As a kullanıcı, I want profesyonel ve tutarlı bir görsel tasarım, so that rahat bir kullanıcı deneyimi yaşayayım.

#### Acceptance Criteria

1. THE UI SHALL koyu tema (dark theme) kullanacak
2. THE UI SHALL tutarlı renk paleti kullanacak (koyu gri tonları, mavi vurgular)
3. THE UI SHALL modern ikonlar kullanacak
4. THE UI SHALL yuvarlatılmış köşeler kullanacak
5. THE UI SHALL hover ve focus durumları için görsel geri bildirim sağlayacak
6. THE UI SHALL Türkçe arayüz metinleri kullanacak
7. THE UI SHALL okunabilir font boyutları kullanacak (minimum 12px)

### Requirement 15: Klavye Kısayolları

**User Story:** As a kullanıcı, I want klavye kısayolları kullanmak, so that daha hızlı çalışabilirim.

#### Acceptance Criteria

1. THE System SHALL Ctrl+N ile yeni proje kısayolu sunacak
2. THE System SHALL Ctrl+O ile proje açma kısayolu sunacak
3. THE System SHALL Ctrl+S ile kaydetme kısayolu sunacak
4. THE System SHALL Ctrl+Z ile geri alma kısayolu sunacak
5. THE System SHALL Ctrl+Y ile yineleme kısayolu sunacak
6. THE System SHALL Delete ile seçili öğeyi silme kısayolu sunacak
7. THE System SHALL Ctrl+C/V/X ile kopyala/yapıştır/kes kısayolları sunacak
8. THE System SHALL F5 ile önizleme başlatma kısayolu sunacak

