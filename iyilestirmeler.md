1. Kritik Hata: Threading (UI Thread vs Background Thread)
AvaloniaUI (ve hemen hemen tüm UI frameworkleri), arayüz güncellemelerinin Main UI Thread üzerinde yapılmasını zorunlu kılar.

Sorun: AnimationService.cs ve PlaylistManager.cs dosyalarında System.Timers.Timer kullanmışsın. Bu timer, süresi dolduğunda Elapsed olayını farklı bir iş parçacığında (ThreadPool thread) tetikler.

AnimationService offset'i güncelleyip OnFrameUpdate eventini tetiklediğinde, bu event arka planda çalışır.

Bu eventi dinleyen ViewModel, arayüzdeki bir nesneyi (örneğin Bitmap'i) güncellemeye çalıştığında, uygulama ya çöker (Cross-thread operation exception) ya da arayüz donar/güncellenmez.

Çözüm: Bu servislerde System.Timers.Timer yerine Avalonia'nın kendi zamanlayıcısı olan Avalonia.Threading.DispatcherTimer kullanılmalı veya eventler UI Thread'e taşınmalıdır.

Düzeltme Örneği (AnimationService.cs için):

C#

using Avalonia.Threading; // Bunu ekle
// using System.Timers; // Bunu kaldır veya kullanma

public class AnimationService : IAnimationService, IDisposable
{
    private readonly DispatcherTimer _timer; // Tipi değişti

    public AnimationService()
    {
        // DispatcherTimer varsayılan olarak UI Thread üzerinde çalışır.
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16.67) // 60 FPS
        };
        _timer.Tick += OnTimerTick; // Elapsed yerine Tick
    }
    
    // ... Diğer metodlar ...

    private void OnTimerTick(object? sender, EventArgs e) // İmza değişti
    {
        if (_state != AnimationState.Playing) return;
        
        // ... (Mevcut mantık aynen kalabilir) ...
        // Buradan tetiklenen OnFrameUpdate artık UI Thread'de çalışacağı için güvenlidir.
    }
    
    // Start/Stop metodlarında _timer.Start() / _timer.Stop() aynı kalır.
}
Aynı işlemi PlaylistManager.cs için de yapmalısın.

2. Performans Sorunu: LedRenderer Kaynak Tüketimi
LedRenderer.cs içerisindeki RenderDisplay metodları her çağrıldığında (animasyon oynarken saniyede 60 kez), new SKPaint oluşturup using bloğu ile yok ediyorsun.

Sorun: Her piksel çizimi veya her kare (frame) için sürekli nesne oluşturup yok etmek Garbage Collector (GC) üzerinde baskı oluşturur ve animasyonda takılmalara (stuttering) neden olur.

Çözüm: SKPaint nesnelerini sınıf seviyesinde readonly olarak bir kez tanımlayıp tekrar tekrar kullanmak çok daha performanslıdır.

Düzeltme Örneği (LedRenderer.cs):

C#

public class LedRenderer : ILedRenderer
{
    // Paint nesnesini önbelleğe al
    private readonly SKPaint _ledPaint = new SKPaint
    {
        IsAntialias = false,
        Style = SKPaintStyle.Fill
    };

    public SKBitmap RenderDisplay(bool[,] pixelMatrix, DisplaySettings settings)
    {
        // ... (Bitmap oluşturma kodları aynı) ...

        // Rengi her frame'de güncelle
        SKColor ledColor = GetLedColor(settings);
        _ledPaint.Color = ApplyBrightness(ledColor, settings.Brightness);

        for (int x = 0; x < matrixWidth; x++)
        {
            for (int y = 0; y < matrixHeight; y++)
            {
                // ...
                if (isLit)
                {
                    // New paint oluşturmak yerine mevcut olanı kullan
                    DrawLedPixel(canvas, x, y, pixelSize, ledDiameter, settings.Shape, _ledPaint);
                }
            }
        }
        return bitmap;
    }
}
3. Mantıksal Sorun: MainWindowViewModel İçinde Render İşlemleri
MainWindowViewModel.cs dosyasında RenderProgramToPreview ve RenderSimpleTabelaToPreview gibi metodlar var.

Sorun: ViewModel, UI mantığını yönetmelidir; piksel hesaplama ve çizim (rendering) işlerini yapmamalıdır. Bu kodlar karmaşıklaştıkça ViewModel şişer ve yönetilemez hale gelir. Ayrıca bu hesaplamalar UI thread'ini bloklayarak arayüzü dondurabilir.

Çözüm: Bu metodları LedRenderer servisine veya yeni bir IPreviewRenderer servisine taşıyıp, parametre olarak gerekli dataları (Zone listesi, Program itemları) oraya göndermelisin. ViewModel sadece sonucu alıp göstermeli.

4. 1R1G1B Renk Mantığı
LedRenderer.cs dosyasında:

C#

public SKColor GetOneROneGOneBColor(int x, int y)
{
    int channel = (x + y) % 3; // Çapraz çizgi oluşturur
    // ...
}
Analiz: Bu kod LED panelde çapraz (diagonal) RGB çizgileri oluşturur. Gerçek LED panellerde genellikle dikey (Vertical) strip yapısı kullanılır (x % 3). Eğer simülasyonun gerçek panellere benzemesini istiyorsan bu formülü kontrol etmelisin.

5. Bellek Yönetimi (Bitmap Dispose)
MainWindowViewModel.cs içinde RenderTextToPreview metodunda:

C#

var textBitmap = _fontLoader.RenderText(font, text, skColor);
if (textBitmap != null)
{
    Preview.UpdateFromTextBitmap(textBitmap);
    textBitmap.Dispose(); // Burada dispose ediliyor, GÜZEL.
}
Ancak RenderProgramToPreview içinde oluşturulan colorMatrix veya geçici bitmaplerin bellekte yer kaplamaması için SkiaSharp nesnelerinin (SKBitmap, SKCanvas) işi biter bitmez Dispose veya using bloğu ile temizlendiğinden emin olmalısın. Kodlarda bazı yerlerde using var ama karmaşık döngülerde kaçırılmış olabilir.