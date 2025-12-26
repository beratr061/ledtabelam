namespace LEDTabelam.Services;

/// <summary>
/// Çözünürlük doğrulama servisi interface'i
/// Requirements: 1.5, 1.6
/// </summary>
public interface IResolutionValidator
{
    /// <summary>
    /// Minimum izin verilen çözünürlük değeri
    /// </summary>
    int MinResolution { get; }

    /// <summary>
    /// Maksimum izin verilen çözünürlük değeri
    /// </summary>
    int MaxResolution { get; }

    /// <summary>
    /// Çözünürlük değerinin geçerli olup olmadığını kontrol eder
    /// </summary>
    /// <param name="value">Kontrol edilecek değer</param>
    /// <returns>Geçerli ise true</returns>
    bool IsValidResolution(int value);

    /// <summary>
    /// Çözünürlük değerini geçerli aralığa sınırlar
    /// </summary>
    /// <param name="value">Sınırlanacak değer</param>
    /// <returns>Geçerli aralıkta değer</returns>
    int ClampResolution(int value);

    /// <summary>
    /// Çözünürlük değerini doğrular ve sonucu döndürür
    /// </summary>
    /// <param name="value">Doğrulanacak değer</param>
    /// <param name="lastValidValue">Son geçerli değer (geçersiz durumda kullanılır)</param>
    /// <returns>Doğrulama sonucu</returns>
    ResolutionValidationResult ValidateResolution(int value, int lastValidValue);

    /// <summary>
    /// Genişlik ve yükseklik çiftini doğrular
    /// </summary>
    /// <param name="width">Genişlik</param>
    /// <param name="height">Yükseklik</param>
    /// <param name="lastValidWidth">Son geçerli genişlik</param>
    /// <param name="lastValidHeight">Son geçerli yükseklik</param>
    /// <returns>Doğrulama sonucu</returns>
    ResolutionPairValidationResult ValidateResolutionPair(int width, int height, int lastValidWidth, int lastValidHeight);
}

/// <summary>
/// Çözünürlük doğrulama sonucu
/// </summary>
public class ResolutionValidationResult
{
    /// <summary>
    /// Doğrulama başarılı mı
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Sonuç değeri (geçerli veya düzeltilmiş)
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Değer düzeltildi mi
    /// </summary>
    public bool WasCorrected { get; set; }
}

/// <summary>
/// Çözünürlük çifti doğrulama sonucu
/// </summary>
public class ResolutionPairValidationResult
{
    /// <summary>
    /// Doğrulama başarılı mı
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Sonuç genişlik değeri
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Sonuç yükseklik değeri
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Genişlik düzeltildi mi
    /// </summary>
    public bool WidthWasCorrected { get; set; }

    /// <summary>
    /// Yükseklik düzeltildi mi
    /// </summary>
    public bool HeightWasCorrected { get; set; }
}
