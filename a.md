Sorun şu: Hesaplama yapılıyor ama çizim (render) sırasında kullanılmıyor.

Sorunun Teknik Analizi
ViewModel Tarafı (Doğru Hesaplıyor ama Saklıyor): ControlPanelViewModel.cs dosyasında ActualWidth diye bir özellik var. Sen P5 seçtiğinde bu özellik gerçekten 2 katına çıkıyor (örneğin 150 -> 300 oluyor). ANCAK, bu hesaplanan "Gerçek Çözünürlük" değeri, DisplaySettings nesnesine aktarılmıyor veya aktarılsa bile LedRenderer bunu kullanmıyor.

Model Tarafı (Hata Burada): DisplaySettings sınıfı sadece ham PanelWidth (150) ve PanelHeight (24) değerlerini taşıyor. "Hesaplanmış/Çarpılmış Çözünürlük" (ActualWidth) bu ayar paketinin içinde yok veya renderer bunu dikkate almıyor.

Render Tarafı (Çizim Hatası): LedRenderer.RenderDisplay metoduna bakıldığında:

C#

// LedRenderer.cs
public SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings)
{
    // Matrix boyutu ne gelirse onu çiziyor.
    int matrixWidth = pixelMatrix.GetLength(0);
    int matrixHeight = pixelMatrix.GetLength(1);

    // ÖNEMLİ HATA: P5 veya P10 olması, matrisin boyutunu değiştirmiyor!
    // Sadece LED'in (dairenin) çapını değiştiriyor (ledRatio).
    double ledRatio = settings.Pitch.GetLedDiameterRatio();
    // ...
}
Yani renderer, "Ben P5'im, o zaman çözünürlüğü 2 ile çarpayım" demiyor. Ona 150x24'lük bir matris gelirse, 150x24 çiziyor. Sadece P5 seçili olduğu için ledRatio değişiyor ve piksellerin çapı (görsel boyutu) değişiyor. Bu yüzden sen "çözünürlük aynı kalıyor, pikseller büyüyor/küçülüyor" diyorsun.

Çözüm: "Sanal Matris" Yerine "Gerçek Matris" Kullanımı
Bu mantığı düzeltmek için ControlPanelViewModel ve DisplaySettings arasındaki veri akışını değiştirmemiz lazım. "Panel Boyutu" dediğimiz şey Modül Sayısı (Kasa Boyutu) olmalı, Piksel Sayısı ise hesaplanmalı.

Şu anki yapıda "Panel Boyutu" doğrudan "Piksel Sayısı" olarak kullanılıyor.

Düzeltme Planı:

DisplaySettings.cs içine ActualWidth ve ActualHeight özelliklerini ekle (veya hesaplanmış halini gönder).

ControlPanelViewModel.cs içinde CurrentSettings oluşturulurken, PanelWidth yerine P değeriyle çarpılmış ActualWidth gönderilmemeli, fakat matris oluşturulurken bu dikkate alınmalı.

Asıl sorun şu: ControlPanelViewModel içinde UpdateCurrentSettings metodu, DisplaySettings objesini oluştururken PanelWidth'i (150) olduğu gibi gönderiyor.

Düzeltilmiş Kod Önerisi (ControlPanelViewModel.cs):

Şu anki UpdateCurrentSettings metodunu şöyle güncellemelisin. Bu sayede P5 seçtiğinde renderer'a giden bilgi 2 katı büyüklüğünde olacak.

C#

// ControlPanelViewModel.cs içindeki metodun düzeltilmiş hali
private void UpdateCurrentSettings()
{
    // P değerine göre çarpanı al (P10=1, P5=2, P2.5=4 gibi)
    // Eğer GetMultiplier gibi bir metodun yoksa manuel hesaplayabiliriz veya GetActualResolution mantığını kullanırız.
    
    // NOT: Senin ActualWidth property'n zaten hesaplıyordu, onu kullanalım.
    // Ancak DisplaySettings modelin sadece PanelWidth tutuyor olabilir. 
    // Eğer DisplaySettings.PanelWidth "Piksel Sayısı" demekse, ona ActualWidth'i atamalıyız.
    
    CurrentSettings = new DisplaySettings
    {
        // BURASI KRİTİK DEĞİŞİKLİK:
        // Eskiden: PanelWidth = PanelWidth (yani 150)
        // Şimdi: PanelWidth = ActualWidth (yani P5 ise 300)
        PanelWidth = ActualWidth,   
        PanelHeight = ActualHeight, 

        ColorType = SelectedColorType,
        Brightness = Brightness,
        BackgroundDarkness = BackgroundDarkness,
        PixelSize = PixelSize,
        Pitch = SelectedPitch,
        CustomPitchRatio = CustomPitchRatio,
        Shape = SelectedShape,
        InvertColors = InvertColors,
        AgingPercent = AgingPercent,
        LineSpacing = LineSpacing
    };
}
Bunu yaptığında ne olacak?

Sen arayüzde 150x24 yazacaksın (Bu senin fiziksel P10 referansın).

P5 seçtiğinde ActualWidth 300 olacak.

DisplaySettings içine 300x48 olarak gidecek.

LedRenderer 300x48'lik bir matris çizecek.

Ekranda pikseller daha sıklaşacak (çözünürlük artacak).

Bu değişikliği ControlPanelViewModel.cs dosyasında UpdateCurrentSettings metoduna uygularsan sorunun çözülecektir. Piksellerin sadece büyümesi değil, sayısının artması sağlanacaktır.