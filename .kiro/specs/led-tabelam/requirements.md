# Requirements Document

## Introduction

LEDTabelam, otobüs hat tabelaları (güzergah göstergeleri) için bitmap font önizleme uygulamasıdır. HD2018/HD2020 benzeri sistemler için tasarlanmış, sadece önizleme odaklı basitleştirilmiş bir masaüstü uygulamasıdır. Avalonia UI framework kullanılarak cross-platform (Windows, Linux, macOS) çalışabilir şekilde geliştirilecektir.

## Glossary

- **LED_Display**: LED piksellerinden oluşan sanal tabela görüntüleme alanı
- **Bitmap_Font**: Karakter görüntülerini piksel bazında içeren font dosyası
- **LED_Pixel**: Tek bir LED noktasını temsil eden görsel eleman
- **Glow_Effect**: LED'lerin etrafındaki ışık halesini simüle eden görsel efekt
- **Resolution**: LED matrisin genişlik x yükseklik piksel sayısı
- **Font_Loader**: Bitmap font dosyalarını yükleyen ve parse eden servis
- **LED_Renderer**: LED piksellerini ekrana çizen render servisi
- **Display_Settings**: Çözünürlük, renk, parlaklık gibi ayarları tutan model
- **Preset**: Önceden tanımlanmış tabela konfigürasyonu

## Requirements

### Requirement 1: Çözünürlük Yönetimi

**User Story:** As a kullanıcı, I want farklı LED tabela çözünürlüklerini seçmek, so that farklı tabela boyutlarını önizleyebilirim.

#### Acceptance Criteria

1. WHEN uygulama başlatıldığında, THE Resolution_Selector SHALL varsayılan olarak 128x16 piksel çözünürlüğü gösterecek
2. WHEN kullanıcı çözünürlük seçiciden bir değer seçtiğinde, THE LED_Display SHALL seçilen çözünürlüğe göre yeniden boyutlanacak
3. THE Resolution_Selector SHALL şu standart çözünürlükleri içerecek: 80x16, 96x16, 128x16, 160x16, 192x16
4. WHEN kullanıcı "Özel" seçeneğini seçtiğinde, THE System SHALL genişlik ve yükseklik için ayrı TextBox alanları gösterecek
5. WHEN özel çözünürlük girildiğinde, THE System SHALL 1-512 piksel aralığında değerleri kabul edecek
6. IF geçersiz çözünürlük değeri girilirse, THEN THE System SHALL hata mesajı gösterecek ve son geçerli değeri koruyacak

### Requirement 2: LED Renk Tipi Seçimi

**User Story:** As a kullanıcı, I want farklı LED renk tiplerini seçmek, so that gerçek tabela renklerini simüle edebilirim.

#### Acceptance Criteria

1. THE Color_Selector SHALL şu renk tiplerini RadioButton olarak sunacak: Amber (#FFB000), Kırmızı (#FF0000), Yeşil (#00FF00), 1R1G1B, Full RGB
2. WHEN kullanıcı tek renk tipi seçtiğinde (Amber, Kırmızı, Yeşil), THE LED_Renderer SHALL tüm aktif pikselleri seçilen renkte gösterecek
3. WHEN kullanıcı 1R1G1B seçtiğinde, THE LED_Renderer SHALL her pikseli basit RGB renk karışımı ile gösterecek
4. WHEN kullanıcı Full RGB seçtiğinde, THE LED_Renderer SHALL her pikseli tam renk desteği ile gösterecek
5. WHEN renk tipi değiştirildiğinde, THE LED_Display SHALL anlık olarak yeni renk ile yeniden render edilecek

### Requirement 3: Metin Girişi ve Türkçe Karakter Desteği

**User Story:** As a kullanıcı, I want Türkçe karakterler dahil metin girmek, so that Türkçe güzergah isimlerini önizleyebilirim.

#### Acceptance Criteria

1. THE Text_Input SHALL çok satırlı TextBox olarak sunulacak
2. THE Text_Input SHALL Türkçe özel karakterleri destekleyecek: ğ, ü, ş, ı, ö, ç, Ğ, Ü, Ş, İ, Ö, Ç
3. WHEN kullanıcı metin girdiğinde, THE LED_Display SHALL metni bitmap font kullanarak render edecek
4. WHEN metin değiştirildiğinde, THE LED_Display SHALL 250ms debounce ile güncellenecek
5. IF girilen karakter yüklü fontta yoksa, THEN THE System SHALL varsayılan placeholder karakter gösterecek

### Requirement 4: Bitmap Font Yönetimi

**User Story:** As a kullanıcı, I want bitmap font dosyalarını yüklemek ve seçmek, so that farklı font stilleri ile önizleme yapabilirim.

#### Acceptance Criteria

1. THE Font_Selector SHALL yüklenmiş fontları ComboBox olarak listeleyecek
2. WHEN "Font Yükle" butonuna tıklandığında, THE System SHALL .fnt, .json ve .png dosyaları için OpenFileDialog açacak
3. WHEN geçerli font dosyası seçildiğinde, THE Font_Loader SHALL dosyayı parse edecek ve font listesine ekleyecek
4. THE Font_Loader SHALL BMFont XML formatını (.fnt + .png) birincil format olarak destekleyecek
5. THE Font_Loader SHALL JSON formatını (.json + .png) alternatif format olarak destekleyecek
6. THE Font_Loader SHALL şu metadata'yı okuyacak: char id, x, y, width, height, xoffset, yoffset, xadvance
7. THE Font_Loader SHALL kerning bilgisini okuyacak (varsa)
8. THE Font_Loader SHALL Türkçe karakterleri (ĞÜŞİÖÇğüşıöç) destekleyecek
9. IF font dosyası geçersiz veya bozuksa, THEN THE System SHALL kullanıcıya hata mesajı gösterecek
10. IF font dosyası 10MB'dan büyükse, THEN THE System SHALL yüklemeyi reddedecek
11. IF font karakter seti boşsa, THEN THE System SHALL uyarı gösterecek
12. THE System SHALL bozuk font yüklemesi sonrası varsayılan fonta geri dönecek
13. THE System SHALL Assets/Fonts/ klasöründe örnek BMFont ile gelecek

### Requirement 5: LED Görsel Ayarları

**User Story:** As a kullanıcı, I want LED parlaklık, arka plan, piksel boyutu ve piksel aralığını ayarlamak, so that gerçekçi tabela görünümü elde edebilirim.

#### Acceptance Criteria

1. THE Brightness_Slider SHALL %0-100 aralığında parlaklık ayarı sunacak
2. WHEN parlaklık değiştirildiğinde, THE LED_Renderer SHALL LED renklerinin alpha/intensity değerini ayarlayacak
3. THE Background_Slider SHALL %0-100 aralığında arka plan karartma ayarı sunacak
4. WHEN arka plan karartma değiştirildiğinde, THE LED_Display SHALL arka plan rengini #000000 ile #0a0a0a arasında ayarlayacak
5. THE Pixel_Size_Slider SHALL 1-20 piksel aralığında LED boyutu ayarı sunacak
6. WHEN piksel boyutu değiştirildiğinde, THE LED_Display SHALL zoom seviyesini güncelleyecek
7. THE Pixel_Pitch_Selector SHALL LED pitch değerlerini sunacak: P2.5, P3, P4, P5, P6, P7.62, P10, Özel
8. WHEN pitch değeri seçildiğinde, THE LED_Renderer SHALL LED'ler arası mesafeyi pitch değerine göre ayarlayacak
9. THE Pixel_Pitch SHALL LED çapı ile LED merkez mesafesi oranını belirleyecek
10. WHEN "Özel" pitch seçildiğinde, THE System SHALL manuel piksel aralığı girişi için TextBox gösterecek
11. THE Default_Pixel_Pitch SHALL P10 olacak

### Requirement 6: LED Render ve Görselleştirme

**User Story:** As a kullanıcı, I want gerçekçi LED görünümü görmek, so that tabelanın gerçek görünümünü simüle edebilirim.

#### Acceptance Criteria

1. THE LED_Renderer SHALL her pikseli yuvarlak LED şeklinde çizecek
2. THE LED_Renderer SHALL aktif LED'ler etrafında gaussian blur glow efekti uygulayacak
3. THE Glow_Effect SHALL LED parlaklığına göre 2-10 piksel yarıçapında olacak
4. THE Glow_Effect SHALL LED renginin alpha değeri %30 olan halo oluşturacak
5. THE LED_Renderer SHALL LED'ler arası boşluk bırakacak (gerçekçi matrix görünümü)
6. THE LED_Display SHALL koyu arka plan (#0a0a0a) kullanacak
7. THE Zoom_Control SHALL +/- butonları ve slider içerecek
8. THE Zoom_Control SHALL %50-%400 aralığında zoom sunacak
9. WHEN zoom değiştirildiğinde, THE LED_Display SHALL merkez noktayı koruyarak önizlemeyi yakınlaştıracak/uzaklaştıracak
10. THE LED_Renderer SHALL SkiaSharp (Avalonia.Skia) kullanarak performanslı render yapacak
11. THE LED_Renderer SHALL SKImageFilter.CreateBlur ile donanım hızlandırmalı glow efekti uygulayacak
12. THE LED_Renderer SHALL bitmap ölçeklemede Nearest Neighbor algoritması kullanacak (keskin piksel kenarları için)
13. THE LED_Renderer SHALL pikseller arasına siyah plastik ızgara (grid overlay) çizecek
14. THE Grid_Overlay SHALL gerçekçi tabela görünümü için kontrast artıracak
15. WHEN "Ters Renk" modu aktifken, THE LED_Renderer SHALL arka planı aydınlatıp yazıları söndürecek

### Requirement 7: Dışa Aktarma

**User Story:** As a kullanıcı, I want önizlemeyi farklı formatlarda kaydetmek, so that tasarımı paylaşabilir veya dokümante edebilirim.

#### Acceptance Criteria

1. WHEN "PNG Kaydet" butonuna tıklandığında, THE System SHALL SaveFileDialog açacak
2. WHEN dosya konumu seçildiğinde, THE System SHALL mevcut LED_Display görüntüsünü PNG formatında kaydedecek
3. THE System SHALL PNG'yi ekranda görünen zoom seviyesinde kaydedecek (kullanıcı seçeneği ile gerçek çözünürlük de seçilebilir)
4. IF kaydetme işlemi başarısız olursa, THEN THE System SHALL kullanıcıya hata mesajı gösterecek
5. THE System SHALL animasyonlu önizlemeyi GIF formatında export edebilecek
6. THE System SHALL animasyonlu önizlemeyi WebP formatında export edebilecek
7. WHEN GIF/WebP export seçildiğinde, THE System SHALL animasyon süresini ve FPS değerini soracak

### Requirement 8: Animasyon Önizleme

**User Story:** As a kullanıcı, I want kayan yazı animasyonu önizlemek, so that dinamik tabela görünümünü test edebilirim.

#### Acceptance Criteria

1. THE Animation_Control SHALL Play/Pause butonu içerecek
2. WHEN Play butonuna tıklandığında, THE LED_Display SHALL metni soldan sağa kaydıracak
3. WHEN Pause butonuna tıklandığında, THE LED_Display SHALL animasyonu durduracak
4. THE Animation_Speed_Slider SHALL 1-100 piksel/saniye aralığında hız ayarı sunacak
5. THE Animation SHALL varsayılan olarak 20 piksel/saniye hızda başlayacak
6. WHEN metin display genişliğinden uzunsa, THE System SHALL otomatik kayan yazı modunu önerecek

### Requirement 9: Profil ve Preset Yönetimi

**User Story:** As a kullanıcı, I want farklı tabela profillerini kaydetmek ve yüklemek, so that Metrobüs, Belediye Otobüsü, Tramvay gibi farklı sistemlerin tabelalarını ayrı ayrı yönetebilirim.

#### Acceptance Criteria

1. THE Profile_Manager SHALL birden fazla profil oluşturmaya izin verecek
2. THE Profile SHALL profil adı içerecek (örn: "Metrobüs Tabelaları", "Belediye Otobüsü", "Tramvay")
3. THE Profile SHALL tüm 999 slot verisini içerecek
4. THE Profile SHALL görsel ayarları içerecek: çözünürlük, LED renk tipi, piksel pitch, parlaklık, arka plan, piksel boyutu, piksel şekli
5. THE Profile SHALL font ayarlarını içerecek
6. THE Profile SHALL zone/layout ayarlarını içerecek
7. THE Profile_Manager SHALL profilleri JSON formatında kaydetme/yükleme yapacak
8. WHEN profil seçildiğinde, THE System SHALL tüm ayarları ve 999 slotu yükleyecek
9. THE Profile_Manager SHALL profil kopyalama özelliği sunacak
10. THE Profile_Manager SHALL profil silme öncesi onay isteyecek
11. THE Profile_Manager SHALL profil import/export özelliği sunacak (tek dosya olarak)
12. THE Default_Profile SHALL "Varsayılan" adıyla uygulama ile birlikte gelecek
13. THE Profile_Selector SHALL ComboBox ile aktif profili seçmeye izin verecek

### Requirement 10: Kullanıcı Arayüzü

**User Story:** As a kullanıcı, I want modern ve kullanışlı bir arayüz, so that uygulamayı rahatça kullanabilirim.

#### Acceptance Criteria

1. THE Main_Window SHALL Windows 11 Fluent Design benzeri görünüm sunacak
2. THE Main_Window SHALL sol tarafta kontrol paneli, sağ tarafta önizleme alanı düzeni kullanacak
3. THE UI SHALL yuvarlatılmış köşeler ve modern renkler kullanacak
4. THE UI SHALL responsive olacak ve pencere boyutu değiştiğinde uyum sağlayacak
5. THE UI SHALL Türkçe arayüz metinleri kullanacak

### Requirement 11: Performance Gereksinimleri

**User Story:** As a kullanıcı, I want uygulamanın akıcı çalışmasını, so that kesintisiz önizleme yapabilirim.

#### Acceptance Criteria

1. THE LED_Renderer SHALL 60 FPS render hızını koruyacak
2. WHEN metin değiştirildiğinde, THE UI SHALL 250ms içinde güncelleme yapacak
3. THE System SHALL 192x64 piksel çözünürlüğe kadar sorunsuz çalışacak
4. THE Animation SHALL minimum 30 FPS akıcılıkta oynatılacak

### Requirement 12: Platform Uyumluluğu

**User Story:** As a kullanıcı, I want uygulamanın farklı platformlarda çalışmasını, so that istediğim işletim sisteminde kullanabilirim.

#### Acceptance Criteria

1. THE Application SHALL Windows 10/11, macOS 11+, ve Linux (Ubuntu 20.04+) üzerinde çalışacak
2. THE Application SHALL .NET 8.0 runtime gerektirecek
3. THE File_Dialogs SHALL her platformun native dialog'unu kullanacak
4. THE Application SHALL minimum 1280x720 ekran çözünürlüğünde çalışacak

### Requirement 13: Klavye Kısayolları

**User Story:** As a kullanıcı, I want klavye kısayolları kullanmak, so that daha hızlı çalışabilirim.

#### Acceptance Criteria

1. THE System SHALL Ctrl+S ile PNG kaydetme kısayolu sunacak
2. THE System SHALL Ctrl+O ile font yükleme kısayolu sunacak
3. THE System SHALL Space ile animasyon Play/Pause kısayolu sunacak
4. THE System SHALL Ctrl++ / Ctrl+- ile zoom kısayolu sunacak


### Requirement 14: Çoklu Satır Desteği

**User Story:** As a kullanıcı, I want birden fazla satır metni aynı anda önizlemek, so that çok satırlı tabelaları test edebilirim.

#### Acceptance Criteria

1. WHEN çözünürlük yüksekliği 16'dan fazla olduğunda, THE System SHALL çoklu satır girişine izin verecek
2. THE Text_Input SHALL her satırı ayrı ayrı render edecek
3. THE System SHALL satır yüksekliğini font yüksekliğine göre otomatik hesaplayacak
4. WHEN satır sayısı display yüksekliğini aştığında, THE System SHALL uyarı gösterecek
5. THE System SHALL satırlar arası boşluğu (line spacing) ayarlanabilir yapacak
6. THE Default_Line_Spacing SHALL 2 piksel olacak


### Requirement 15: Sıralı Mesajlar (Playlist)

**User Story:** As a kullanıcı, I want birden fazla mesajı sırayla göstermek, so that otobüs tabelası gibi döngüsel mesaj akışını simüle edebilirim.

#### Acceptance Criteria

1. THE Playlist_Manager SHALL birden fazla mesajı listeye eklemeye izin verecek
2. THE Playlist_Item SHALL mesaj metni ve gösterim süresi (saniye) içerecek
3. WHEN playlist modu aktifken, THE LED_Display SHALL mesajlar arasında otomatik geçiş yapacak
4. THE Playlist_Manager SHALL mesaj sırasını sürükle-bırak ile değiştirmeye izin verecek
5. THE Playlist_Manager SHALL döngü (loop) modunu destekleyecek
6. THE Default_Message_Duration SHALL 3 saniye olacak
7. WHEN mesajlar arası geçiş yapılırken, THE System SHALL fade veya slide geçiş efekti uygulayacak


### Requirement 16: SVG ve Grafik Desteği

**User Story:** As a kullanıcı, I want SVG ve bitmap grafikleri yüklemek, so that logo ve ikonları tabelada gösterebilirim.

#### Acceptance Criteria

1. THE System SHALL .svg dosyalarını yüklemeyi destekleyecek
2. THE SVG_Renderer SHALL vektör grafikleri matris yüksekliğine kayıpsız ölçekleyecek
3. THE SVG_Renderer SHALL IsAntialias = false ile hard-edge rendering yapacak (keskin LED pikselleri)
4. THE System SHALL siyah/beyaz SVG ikonları seçili LED rengine otomatik boyayacak
5. THE System SHALL PNG/JPG yüklemelerini destekleyecek
6. THE Image_Loader SHALL "Threshold" (eşik) ayarı ile hangi piksellerin yanacağını belirleyecek
7. THE Default_Threshold SHALL %50 olacak

### Requirement 17: Layout ve Bölge Yönetimi

**User Story:** As a kullanıcı, I want tabela ekranını bölgelere ayırmak, so that logo, metin ve hat numarasını ayrı alanlarda gösterebilirim.

#### Acceptance Criteria

1. THE Zone_Manager SHALL tabela ekranını dikey çizgilerle bölgelere ayırmaya izin verecek
2. THE Zone SHALL yüzde bazlı genişlik değeri alacak (örn: %15 sol, %70 orta, %15 sağ)
3. THE Zone SHALL içerik tipi belirleyecek: Resim, Metin, Kayan Yazı
4. THE Zone_Manager SHALL bölge sınırlarını sürükle-bırak ile ayarlamaya izin verecek
5. THE System SHALL katman (layer) mantığı destekleyecek: arka plan ve ön plan
6. THE Background_Layer SHALL sabit içerik (çerçeve, logo) için kullanılacak
7. THE Foreground_Layer SHALL dinamik içerik (kayan yazı) için kullanılacak

### Requirement 18: Dahili Varlık Kütüphanesi

**User Story:** As a kullanıcı, I want hazır ikonları kullanmak, so that hızlıca profesyonel görünümlü tabelalar oluşturabilirim.

#### Acceptance Criteria

1. THE Asset_Library SHALL uygulama içine gömülü pixel-perfect ikonlar içerecek
2. THE Asset_Library SHALL 16px ve 32px uyumlu ikonlar sunacak
3. THE Asset_Library SHALL şu kategorilerde ikonlar içerecek: Bayraklar (Türk Bayrağı), Erişilebilirlik (Engelli İkonu), Yön Okları, Ulaşım Sembolleri (Otobüs, Metro, Tramvay)
4. WHEN ikon seçildiğinde, THE System SHALL ikonu seçili LED rengine boyayacak
5. THE Asset_Library SHALL kullanıcının kendi ikonlarını eklemesine izin verecek

### Requirement 19: Piksel Şekli ve Eskime Efektleri

**User Story:** As a kullanıcı, I want farklı piksel şekilleri ve eskime efektleri uygulamak, so that farklı tabela tiplerini ve yaşanmışlık hissini simüle edebilirim.

#### Acceptance Criteria

1. THE Pixel_Shape_Selector SHALL Kare (eski tip) ve Yuvarlak (SMD/DIP) seçenekleri sunacak
2. WHEN piksel şekli değiştirildiğinde, THE LED_Renderer SHALL tüm pikselleri seçilen şekilde çizecek
3. THE Aging_Effect SHALL isteğe bağlı olarak rastgele ölü piksel simülasyonu yapacak
4. THE Aging_Effect SHALL %0-5 aralığında ölü piksel oranı ayarı sunacak
5. THE Aging_Effect SHALL bazı pikselleri sönük veya farklı renkte gösterecek
6. THE Default_Aging_Effect SHALL kapalı olacak


### Requirement 20: Tabela Slot Yönetimi

**User Story:** As a kullanıcı, I want 999 adet tabela slotunu yönetmek, so that gerçek otobüs tabela kontrol ünitesi gibi çalışabilirim.

#### Acceptance Criteria

1. THE Slot_Manager SHALL 001-999 arası numaralı tabela slotları sunacak
2. THE Slot SHALL hat numarası, güzergah metni ve opsiyonel ikon/logo içerecek
3. WHEN slot numarası girildiğinde, THE System SHALL ilgili slottaki tabela içeriğini gösterecek
4. THE Slot_Selector SHALL NumericUpDown veya TextBox ile slot numarası girişi alacak
5. THE Slot_Manager SHALL slotları JSON formatında kaydetme/yükleme yapacak
6. THE Slot_Manager SHALL boş slotları "Tanımsız" olarak gösterecek
7. THE Slot_Manager SHALL slot arama özelliği sunacak (hat numarası veya güzergah adına göre)
8. THE Slot_Manager SHALL toplu slot içe/dışa aktarma (import/export) destekleyecek
9. THE Slot_Editor SHALL seçili slotu düzenleme arayüzü sunacak
10. WHEN slot kaydedildiğinde, THE System SHALL değişiklikleri otomatik persist edecek


### Requirement 21: İçerik Hizalama

**User Story:** As a kullanıcı, I want metin ve grafikleri hizalamak, so that profesyonel görünümlü tabelalar oluşturabilirim.

#### Acceptance Criteria

1. THE Alignment_Control SHALL yatay hizalama seçenekleri sunacak: Sol, Orta, Sağ
2. THE Alignment_Control SHALL dikey hizalama seçenekleri sunacak: Üst, Orta, Alt
3. WHEN hizalama değiştirildiğinde, THE LED_Display SHALL içeriği seçilen hizalamaya göre konumlandıracak
4. THE Zone SHALL kendi içinde bağımsız hizalama ayarına sahip olacak
5. THE Default_Horizontal_Alignment SHALL Orta olacak
6. THE Default_Vertical_Alignment SHALL Orta olacak

### Requirement 22: Metin Stilleri (Arkaplan ve Stroke)

**User Story:** As a kullanıcı, I want metinlere arkaplan ve çerçeve (stroke) eklemek, so that okunabilirliği artırabilir ve farklı stiller oluşturabilirim.

#### Acceptance Criteria

1. THE Text_Style SHALL metin arkaplan rengi ayarı sunacak
2. THE Text_Background SHALL aktif/pasif durumu olacak
3. WHEN metin arkaplanı aktifken, THE LED_Renderer SHALL metin piksellerinin arkasına dolgu rengi çizecek
4. THE Text_Style SHALL stroke (çerçeve/kontur) ayarı sunacak
5. THE Stroke SHALL kalınlık ayarı sunacak: 1-3 piksel
6. THE Stroke SHALL renk ayarı sunacak
7. WHEN stroke aktifken, THE LED_Renderer SHALL metin piksellerinin etrafına kontur çizecek
8. THE Default_Stroke SHALL kapalı olacak
9. THE Default_Background SHALL kapalı olacak
