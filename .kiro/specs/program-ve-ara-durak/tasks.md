# Implementation Plan: Program ve Ara Durak Sistemi

## Overview

Bu plan, LED tabela uygulamasına program bazlı içerik yönetimi ve ara durak sistemi eklemek için gerekli adımları içerir. Uygulama C# ve Avalonia UI kullanılarak geliştirilecektir. Mevcut mimari üzerine inşa edilecek ve geriye dönük uyumluluk korunacaktır.

## Tasks

- [x] 1. Yeni Model Sınıflarını Oluştur
  - [x] 1.1 Enum tanımlarını ekle (ProgramTransitionType, StopAnimationType)
    - LEDTabelam/Models/Enums.cs dosyasına yeni enum'ları ekle
    - ProgramTransitionType: Direct, Fade, SlideLeft, SlideRight, SlideUp, SlideDown
    - StopAnimationType: Direct, Fade, SlideUp, SlideDown
    - _Requirements: 3.1, 6.1_
  - [x] 1.2 IntermediateStop model sınıfını oluştur
    - LEDTabelam/Models/IntermediateStop.cs dosyası oluştur
    - Order (int) ve StopName (string) property'leri
    - ReactiveObject'ten türet
    - _Requirements: 4.6_
  - [x] 1.3 IntermediateStopSettings model sınıfını oluştur
    - LEDTabelam/Models/IntermediateStopSettings.cs dosyası oluştur
    - IsEnabled, Stops (ObservableCollection), DurationSeconds, Animation, AnimationDurationMs, AutoCalculateDuration
    - Varsayılan değerler: IsEnabled=false, Duration=2.0, Animation=Direct, AnimationDurationMs=200
    - _Requirements: 5.1, 5.2, 6.2, 6.5_
  - [x] 1.4 TabelaProgram model sınıfını oluştur
    - LEDTabelam/Models/TabelaProgram.cs dosyası oluştur
    - Id, Name, DurationSeconds, Transition, TransitionDurationMs, Items (ObservableCollection<TabelaItem>), IsActive
    - Varsayılan değerler: Duration=5, Transition=Direct, TransitionDurationMs=300
    - _Requirements: 1.3, 1.4, 2.1, 2.2, 3.2, 3.5_
  - [x] 1.5 Model varsayılan değer testleri yaz
    - TabelaProgram varsayılan değerlerini doğrula
    - IntermediateStopSettings varsayılan değerlerini doğrula
    - _Requirements: 2.2, 5.2, 6.2, 6.5_

- [x] 2. TabelaItem Modelini Güncelle
  - [x] 2.1 IntermediateStopSettings property'sini TabelaItem'a ekle
    - LEDTabelam/Models/TabelaItem.cs dosyasını güncelle
    - IntermediateStops property'si ekle
    - HasIntermediateStops computed property ekle
    - _Requirements: 4.1, 9.2_
  - [x] 2.2 Property test: Ara durak koleksiyonu yönetimi
    - **Property 6: Ara Durak Koleksiyonu Yönetimi**
    - **Validates: Requirements 4.5, 4.7, 4.8**

- [x] 3. Profile Modelini Güncelle
  - [x] 3.1 Programs koleksiyonunu Profile'a ekle
    - LEDTabelam/Models/Profile.cs dosyasını güncelle
    - Programs (ObservableCollection<TabelaProgram>) property'si ekle
    - Geriye dönük uyumluluk için migration mantığı ekle
    - _Requirements: 9.1_
  - [x] 3.2 Property test: Program koleksiyonu minimum boyut invariantı
    - **Property 1: Program Koleksiyonu Minimum Boyut Invariantı**
    - **Validates: Requirements 1.8**
  - [x] 3.3 Property test: Program ID benzersizliği
    - **Property 2: Program ID Benzersizliği**
    - **Validates: Requirements 1.3**

- [x] 4. Checkpoint - Model Testleri
  - Tüm model testlerinin geçtiğinden emin ol
  - Kullanıcıya soru sor

- [x] 5. ProgramSequencer Servisini Oluştur
  - [x] 5.1 IProgramSequencer interface'ini oluştur
    - LEDTabelam/Services/IProgramSequencer.cs dosyası oluştur
    - CurrentProgramIndex, CurrentProgram, IsPlaying, IsLooping property'leri
    - Play(), Pause(), Stop(), NextProgram(), PreviousProgram(), GoToProgram() metodları
    - OnTick(deltaTime) metodu
    - ProgramChanged, StopChanged event'leri
    - _Requirements: 7.1, 7.4_
  - [x] 5.2 ProgramSequencer implementasyonunu oluştur
    - LEDTabelam/Services/ProgramSequencer.cs dosyası oluştur
    - Program timer mantığı (süre takibi, otomatik geçiş)
    - Ara durak timer mantığı (her item için ayrı)
    - Döngü modu desteği
    - _Requirements: 2.4, 2.6, 5.4, 5.5, 7.2, 7.3, 7.6_
  - [x] 5.3 Otomatik süre hesaplama mantığını ekle
    - AutoCalculateDuration aktifken: durak_süresi = program_süresi / durak_sayısı
    - _Requirements: 8.4_
  - [x] 5.4 Property test: Program döngü davranışı
    - **Property 5: Program Döngü Davranışı**
    - **Validates: Requirements 2.4, 2.6**
  - [x] 5.5 Property test: Ara durak döngü davranışı
    - **Property 8: Ara Durak Döngü Davranışı**
    - **Validates: Requirements 5.4, 5.5**
  - [x] 5.6 Property test: Play/Pause state değişimi
    - **Property 13: Play/Pause State Değişimi**
    - **Validates: Requirements 7.2, 7.3**
  - [x] 5.7 Property test: Non-loop mode davranışı
    - **Property 14: Non-Loop Mode Davranışı**
    - **Validates: Requirements 7.6**
  - [x] 5.8 Property test: Otomatik süre hesaplama
    - **Property 10: Otomatik Süre Hesaplama**
    - **Validates: Requirements 8.4**

- [x] 6. Sınır Kontrol Validasyonlarını Ekle
  - [x] 6.1 Program süresi sınır kontrolü
    - TabelaProgram.DurationSeconds setter'ında [1, 60] aralığına clamp
    - _Requirements: 2.3_
  - [x] 6.2 Ara durak süresi sınır kontrolü
    - IntermediateStopSettings.DurationSeconds setter'ında [0.5, 10] aralığına clamp
    - _Requirements: 5.3_
  - [x] 6.3 Geçiş süresi sınır kontrolleri
    - Program transition: [200, 1000] ms
    - Stop animation: [100, 500] ms
    - _Requirements: 3.4, 6.4_
  - [x] 6.4 Property test: Sınır kontrolleri
    - **Property 4: Program Süresi Sınır Kontrolü**
    - **Property 7: Ara Durak Süresi Sınır Kontrolü**
    - **Property 12: Geçiş Süresi Sınır Kontrolü**
    - **Validates: Requirements 2.3, 5.3, 3.4, 6.4**

- [x] 7. Checkpoint - Sequencer Testleri
  - Tüm sequencer testlerinin geçtiğinden emin ol
  - Kullanıcıya soru sor

- [x] 8. Serializasyon Desteğini Güncelle
  - [x] 8.1 ProfileManager'ı güncelle
    - Programs koleksiyonunu JSON'a serialize et
    - IntermediateStopSettings'i serialize et
    - Geriye dönük uyumluluk: eski profilleri yüklerken varsayılan program oluştur
    - _Requirements: 9.3, 9.4_
  - [x] 8.2 Property test: Serializasyon round-trip
    - **Property 11: Serializasyon Round-Trip**
    - **Validates: Requirements 9.3, 9.4**

- [x] 9. ViewModel Güncellemeleri
  - [x] 9.1 UnifiedEditorViewModel'e program yönetimi ekle
    - Programs koleksiyonu
    - SelectedProgram property
    - AddProgram(), RemoveProgram(), SelectProgram() komutları
    - Program sıralama desteği
    - _Requirements: 1.1, 1.2, 1.5, 1.6, 1.7_
  - [x] 9.2 Ara durak yönetimi metodlarını ekle
    - AddIntermediateStop(), RemoveIntermediateStop() komutları
    - Ara durak sıralama desteği
    - _Requirements: 4.3, 4.4, 4.5, 4.7, 4.8_
  - [x] 9.3 Playback kontrol komutlarını ekle
    - PlayCommand, PauseCommand, NextProgramCommand, PreviousProgramCommand
    - CurrentProgramDisplay (örn: "1/3")
    - _Requirements: 7.1, 7.4, 7.5_
  - [x] 9.4 Property test: Program ekleme koleksiyonu büyütür
    - **Property 3: Program Ekleme Koleksiyonu Büyütür**
    - **Validates: Requirements 1.1, 1.2, 1.4**

- [x] 10. UI Güncellemeleri - Sol Panel
  - [x] 10.1 Program listesi bölümünü ekle
    - UnifiedEditor.axaml sol paneline program listesi ekle
    - Her program için: isim, süre, öğe sayısı gösterimi
    - Program seçimi, ekleme, silme butonları
    - _Requirements: 10.1, 10.2_
  - [x] 10.2 Program düzenleme alanını ekle
    - Seçili program için isim, süre, geçiş tipi ayarları
    - _Requirements: 2.5, 3.1_

- [x] 11. UI Güncellemeleri - Sağ Panel
  - [x] 11.1 Ara Duraklar bölümünü ekle
    - Sağ panele collapsible "Ara Duraklar" bölümü ekle
    - Sadece metin öğeleri için görünür
    - _Requirements: 10.3, 10.4_
  - [x] 11.2 Ara durak listesi ve kontrolleri ekle
    - Durak listesi (ListBox)
    - Durak ekleme TextBox ve butonu
    - Durak silme butonu
    - Süre ayarı (Slider/NumericUpDown)
    - Animasyon tipi seçimi (ComboBox)
    - _Requirements: 4.2, 4.3, 4.4, 5.6, 6.1_

- [x] 12. UI Güncellemeleri - Playback Kontrolleri
  - [x] 12.1 Program playback kontrollerini ekle
    - Play/Pause butonu
    - Sonraki/Önceki program butonları
    - Program durumu gösterimi (örn: "Program 1/3")
    - _Requirements: 7.1, 7.4, 7.5_
  - [x] 12.2 Status bar güncellemesi
    - Aktif program ve ara durak durumunu göster
    - _Requirements: 10.5, 10.6_

- [x] 13. AnimationService Entegrasyonu
  - [x] 13.1 ProgramSequencer'ı AnimationService'e entegre et
    - OnTick çağrısını AnimationService'den yönlendir
    - Program geçiş animasyonlarını uygula
    - Ara durak animasyonlarını uygula
    - _Requirements: 3.3, 6.3, 8.3_
  - [x] 13.2 Property test: Eşzamanlı döngü bağımsızlığı
    - **Property 9: Eşzamanlı Döngü Bağımsızlığı**
    - **Validates: Requirements 8.3**

- [x] 14. Önizleme Güncellemeleri
  - [x] 14.1 Program önizlemesini güncelle
    - Aktif programın öğelerini render et
    - Program geçişlerinde animasyon efektleri
    - _Requirements: 8.1_
  - [x] 14.2 Ara durak önizlemesini ekle
    - Ara durak döngüsünü göster
    - Mevcut durak numarası gösterimi
    - _Requirements: 8.1, 8.2_

- [x] 15. Final Checkpoint
  - Tüm testlerin geçtiğinden emin ol
  - UI'ın düzgün çalıştığını doğrula
  - Kullanıcıya soru sor

## Notes

- Tüm tasklar zorunludur (opsiyonel task yok)
- Her task, ilgili requirements'a referans verir
- Property testleri FsCheck kütüphanesi ile yazılacak
- Mevcut kod yapısı korunacak, sadece ekleme yapılacak
- Geriye dönük uyumluluk için eski profiller otomatik migrate edilecek

