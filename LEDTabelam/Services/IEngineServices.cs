namespace LEDTabelam.Services;

/// <summary>
/// Render ve çizim ile ilgili servisleri gruplayan Facade arayüzü
/// ViewModel constructor şişmesini önlemek için kullanılır
/// </summary>
public interface IEngineServices
{
    /// <summary>
    /// Font yükleme servisi
    /// </summary>
    IFontLoader FontLoader { get; }

    /// <summary>
    /// LED render servisi
    /// </summary>
    ILedRenderer LedRenderer { get; }

    /// <summary>
    /// Animasyon servisi
    /// </summary>
    IAnimationService AnimationService { get; }

    /// <summary>
    /// Export servisi
    /// </summary>
    IExportService ExportService { get; }

    /// <summary>
    /// Çok satırlı metin render servisi
    /// </summary>
    IMultiLineTextRenderer MultiLineTextRenderer { get; }

    /// <summary>
    /// Önizleme render servisi
    /// </summary>
    IPreviewRenderer PreviewRenderer { get; }
}
