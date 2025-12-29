using System.Threading.Tasks;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Bitmap font yükleme ve metin render servisi interface'i
/// Requirements: 4.1, 4.2, 4.3
/// </summary>
public interface IFontLoader
{
    /// <summary>
    /// BMFont XML formatındaki font dosyasını yükler (.fnt + .png)
    /// </summary>
    /// <param name="fntPath">.fnt dosyasının yolu</param>
    /// <returns>Yüklenen BitmapFont nesnesi</returns>
    Task<BitmapFont> LoadBMFontAsync(string fntPath);

    /// <summary>
    /// JSON formatındaki font dosyasını yükler (.json + .png)
    /// </summary>
    /// <param name="jsonPath">.json dosyasının yolu</param>
    /// <returns>Yüklenen BitmapFont nesnesi</returns>
    Task<BitmapFont> LoadJsonFontAsync(string jsonPath);

    /// <summary>
    /// Font dosyasının geçerliliğini kontrol eder
    /// - 10MB limit kontrolü
    /// - Boş karakter seti kontrolü
    /// - PNG dosyası varlık kontrolü
    /// </summary>
    /// <param name="font">Kontrol edilecek font</param>
    /// <returns>Font geçerli ise true</returns>
    bool ValidateFont(BitmapFont font);

    /// <summary>
    /// Metni bitmap font kullanarak SKBitmap'e render eder
    /// - Türkçe karakter desteği (ğ, ü, ş, ı, ö, ç, Ğ, Ü, Ş, İ, Ö, Ç)
    /// - Eksik karakterler için placeholder (□) desteği
    /// </summary>
    /// <param name="font">Kullanılacak font</param>
    /// <param name="text">Render edilecek metin</param>
    /// <param name="color">Metin rengi</param>
    /// <param name="letterSpacing">Harf arası boşluk (piksel, varsayılan 1)</param>
    /// <returns>Render edilmiş bitmap</returns>
    SKBitmap RenderText(BitmapFont font, string text, SKColor color, int letterSpacing = 1);
}
