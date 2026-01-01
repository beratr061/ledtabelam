# Requirements Document

## Introduction

Bu özellik, LED tabela uygulamasına gerçek otobüs tabelalarındaki gibi program bazlı içerik yönetimi ve ara durak sistemi ekler. Gerçek otobüs tabelalarında sabit yazı nadiren görülür - bunun yerine hat bilgisi, bayram mesajları, ara duraklar gibi içerikler belirli sürelerle dönüşümlü olarak gösterilir. Bu özellik ile kullanıcılar birden fazla program oluşturabilir, her programın ekranda kalma süresini ayarlayabilir ve metin öğelerine ara durak listesi ekleyebilir.

## Glossary

- **Program**: Tabelada gösterilecek tek bir içerik konfigürasyonu. Her program kendi öğelerini (metin, sembol vb.) içerir ve belirli bir süre ekranda kalır.
- **Program_Manager**: Programları yöneten, sıralayan ve geçişleri kontrol eden servis.
- **Program_Duration**: Bir programın ekranda kalma süresi (saniye cinsinden).
- **Ara_Durak**: Bir metin öğesine bağlı, sırayla gösterilecek durak isimleri listesi.
- **Ara_Durak_Duration**: Tüm ara durakların ekranda kalma süresi (varsayılan 2 saniye).
- **Ara_Durak_Animation**: Ara duraklar arası geçiş animasyonu (direkt, kaymalı vb.).
- **Program_Transition**: Programlar arası geçiş efekti.
- **Active_Program**: Şu anda ekranda gösterilen program.

## Requirements

### Requirement 1: Program Yönetimi

**User Story:** As a kullanıcı, I want birden fazla program oluşturmak ve yönetmek, so that tabelada farklı içerikleri dönüşümlü olarak gösterebilirim.

#### Acceptance Criteria

1. THE Program_Manager SHALL birden fazla program oluşturmaya izin verecek
2. WHEN "Program Ekle" butonuna tıklandığında, THE System SHALL yeni bir boş program oluşturacak ve listeye ekleyecek
3. THE Program SHALL benzersiz bir ID ve kullanıcı tanımlı isim içerecek
4. THE Program SHALL kendi TabelaItem koleksiyonunu içerecek (mevcut Items yapısı gibi)
5. WHEN bir program seçildiğinde, THE Editor SHALL o programın öğelerini gösterecek ve düzenlemeye izin verecek
6. THE Program_Manager SHALL programları sürükle-bırak ile yeniden sıralamaya izin verecek
7. WHEN program silindiğinde, THE System SHALL onay isteyecek ve silme işlemini gerçekleştirecek
8. THE System SHALL en az bir program bulunmasını zorunlu kılacak (son program silinemez)

### Requirement 2: Program Süresi Ayarı

**User Story:** As a kullanıcı, I want her programın ekranda kalma süresini ayarlamak, so that içeriklerin görünme süresini kontrol edebilirim.

#### Acceptance Criteria

1. THE Program SHALL duration (süre) özelliği içerecek (saniye cinsinden)
2. THE Default_Program_Duration SHALL 5 saniye olacak
3. THE Program_Duration SHALL 1-60 saniye aralığında ayarlanabilir olacak
4. WHEN program süresi dolduğunda, THE System SHALL otomatik olarak sonraki programa geçecek
5. THE Program_Editor SHALL süre ayarı için NumericUpDown veya Slider sunacak
6. WHEN son programa ulaşıldığında, THE System SHALL ilk programa dönerek döngüyü sürdürecek

### Requirement 3: Program Geçiş Animasyonları

**User Story:** As a kullanıcı, I want programlar arası geçiş efektlerini seçmek, so that profesyonel görünümlü geçişler elde edebilirim.

#### Acceptance Criteria

1. THE Program_Transition SHALL şu geçiş tiplerini destekleyecek: Direkt (kesme), Fade (solma), Slide Sol, Slide Sağ, Slide Yukarı, Slide Aşağı
2. THE Default_Program_Transition SHALL Direkt olacak
3. WHEN geçiş animasyonu seçildiğinde, THE System SHALL programlar arası geçişte seçilen efekti uygulayacak
4. THE Transition_Duration SHALL 200-1000ms aralığında ayarlanabilir olacak
5. THE Default_Transition_Duration SHALL 300ms olacak

### Requirement 4: Ara Durak Sistemi

**User Story:** As a kullanıcı, I want metin öğelerine ara durak listesi eklemek, so that güzergah üzerindeki durakları sırayla gösterebilirim.

#### Acceptance Criteria

1. THE TabelaItem (metin tipi) SHALL ara durak listesi özelliği içerecek
2. WHEN bir metin öğesi seçildiğinde, THE Editor SHALL sağ panelde "Ara Duraklar" bölümü gösterecek
3. THE Ara_Durak_Section SHALL "Ara Durak Ekle" butonu içerecek
4. WHEN "Ara Durak Ekle" butonuna tıklandığında, THE System SHALL yeni bir ara durak girişi için TextBox gösterecek
5. THE Ara_Durak_List SHALL birden fazla durak eklemeye izin verecek
6. THE Ara_Durak_Item SHALL durak adı (string) içerecek
7. THE System SHALL ara durakları sürükle-bırak ile yeniden sıralamaya izin verecek
8. THE System SHALL ara durak silme özelliği sunacak

### Requirement 5: Ara Durak Gösterim Süresi

**User Story:** As a kullanıcı, I want ara durakların ekranda kalma süresini ayarlamak, so that her durağın ne kadar süre görüneceğini kontrol edebilirim.

#### Acceptance Criteria

1. THE Ara_Durak_Duration SHALL tüm ara duraklar için ortak bir süre değeri olacak (öğe bazında)
2. THE Default_Ara_Durak_Duration SHALL 2 saniye olacak
3. THE Ara_Durak_Duration SHALL 0.5-10 saniye aralığında ayarlanabilir olacak
4. WHEN ara durak süresi dolduğunda, THE System SHALL otomatik olarak sonraki durağa geçecek
5. WHEN son durağa ulaşıldığında, THE System SHALL ilk durağa dönerek döngüyü sürdürecek
6. THE Editor SHALL ara durak süresi için Slider veya NumericUpDown sunacak

### Requirement 6: Ara Durak Animasyonları

**User Story:** As a kullanıcı, I want ara duraklar arası geçiş animasyonlarını seçmek, so that durak değişimlerinin nasıl görüneceğini kontrol edebilirim.

#### Acceptance Criteria

1. THE Ara_Durak_Animation SHALL şu animasyon tiplerini destekleyecek: Direkt (kesme), Fade (solma), Slide Yukarı, Slide Aşağı
2. THE Default_Ara_Durak_Animation SHALL Direkt olacak
3. WHEN animasyon tipi seçildiğinde, THE System SHALL ara duraklar arası geçişte seçilen efekti uygulayacak
4. THE Animation_Duration SHALL 100-500ms aralığında olacak
5. THE Default_Animation_Duration SHALL 200ms olacak
6. THE Editor SHALL animasyon tipi için ComboBox sunacak

### Requirement 7: Program Oynatma Kontrolü

**User Story:** As a kullanıcı, I want program döngüsünü başlatmak, durdurmak ve kontrol etmek, so that önizleme sırasında programları test edebilirim.

#### Acceptance Criteria

1. THE Playback_Control SHALL Play/Pause butonu içerecek
2. WHEN Play butonuna tıklandığında, THE System SHALL program döngüsünü başlatacak
3. WHEN Pause butonuna tıklandığında, THE System SHALL program döngüsünü durduracak ve mevcut programda kalacak
4. THE Playback_Control SHALL "Sonraki Program" ve "Önceki Program" butonları içerecek
5. THE Playback_Control SHALL mevcut program numarasını ve toplam program sayısını gösterecek
6. WHEN döngü modunda değilken, THE System SHALL son programda duracak

### Requirement 8: Ara Durak Önizleme

**User Story:** As a kullanıcı, I want ara durakların nasıl görüneceğini önizlemek, so that ayarları test edebilirim.

#### Acceptance Criteria

1. WHEN ara durak içeren bir öğe seçiliyken, THE Preview SHALL ara durak döngüsünü gösterecek
2. THE Preview SHALL mevcut durak numarasını ve toplam durak sayısını gösterecek
3. WHEN program oynatılırken, THE System SHALL hem program döngüsünü hem ara durak döngüsünü eşzamanlı çalıştıracak
4. THE System SHALL ara durak döngüsünü program süresi içinde tamamlamaya çalışacak

### Requirement 9: Veri Kalıcılığı

**User Story:** As a kullanıcı, I want program ve ara durak ayarlarının kaydedilmesini, so that çalışmamı kaybetmeyeyim.

#### Acceptance Criteria

1. THE Profile SHALL tüm programları ve ayarlarını içerecek
2. THE TabelaItem SHALL ara durak listesini ve ayarlarını içerecek
3. WHEN profil kaydedildiğinde, THE System SHALL program ve ara durak verilerini JSON formatında persist edecek
4. WHEN profil yüklendiğinde, THE System SHALL program ve ara durak verilerini restore edecek

### Requirement 10: Kullanıcı Arayüzü Güncellemeleri

**User Story:** As a kullanıcı, I want program ve ara durak özelliklerini kolayca kullanmak, so that verimli çalışabilirim.

#### Acceptance Criteria

1. THE Left_Panel SHALL program listesini gösterecek (mevcut öğe listesinin üstünde)
2. THE Program_List SHALL her program için isim, süre ve öğe sayısını gösterecek
3. THE Right_Panel SHALL seçili metin öğesi için "Ara Duraklar" bölümü içerecek
4. THE Ara_Durak_Section SHALL açılır/kapanır (collapsible) olacak
5. THE UI SHALL program ve ara durak durumunu status bar'da gösterecek
6. THE Editor SHALL aktif programı görsel olarak vurgulayacak

