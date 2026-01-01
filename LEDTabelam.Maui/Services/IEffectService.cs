using System.Threading;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Efekt servisi interface'i
/// İçerik öğelerine giriş/çıkış efektleri uygular
/// </summary>
public interface IEffectService
{
    /// <summary>
    /// İçerik öğesine giriş efekti uygular
    /// </summary>
    /// <param name="content">İçerik öğesi</param>
    /// <param name="canvas">Çizim yapılacak canvas</param>
    /// <param name="sourceBitmap">Kaynak bitmap</param>
    /// <param name="progress">Efekt ilerleme durumu (0.0 - 1.0)</param>
    void ApplyEntryEffect(ContentItem content, SKCanvas canvas, SKBitmap sourceBitmap, double progress);

    /// <summary>
    /// İçerik öğesine çıkış efekti uygular
    /// </summary>
    /// <param name="content">İçerik öğesi</param>
    /// <param name="canvas">Çizim yapılacak canvas</param>
    /// <param name="sourceBitmap">Kaynak bitmap</param>
    /// <param name="progress">Efekt ilerleme durumu (0.0 - 1.0)</param>
    void ApplyExitEffect(ContentItem content, SKCanvas canvas, SKBitmap sourceBitmap, double progress);

    /// <summary>
    /// Efekti asenkron olarak oynatır
    /// </summary>
    /// <param name="content">İçerik öğesi</param>
    /// <param name="effect">Efekt yapılandırması</param>
    /// <param name="renderCallback">Her frame için çağrılacak render callback</param>
    /// <param name="cancellationToken">İptal token'ı</param>
    Task PlayEffectAsync(ContentItem content, EffectConfig effect, Action<double> renderCallback, CancellationToken cancellationToken = default);

    /// <summary>
    /// Çalışan efekti durdurur
    /// </summary>
    void StopEffect();

    /// <summary>
    /// Efekt çalışıyor mu?
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Belirtilen efekt tipine göre transform hesaplar
    /// </summary>
    /// <param name="effectType">Efekt tipi</param>
    /// <param name="direction">Efekt yönü</param>
    /// <param name="progress">İlerleme (0.0 - 1.0)</param>
    /// <param name="bounds">İçerik sınırları</param>
    /// <returns>Transform matrisi</returns>
    SKMatrix CalculateTransform(EffectType effectType, EffectDirection direction, double progress, SKRect bounds);

    /// <summary>
    /// Belirtilen efekt tipine göre opacity hesaplar
    /// </summary>
    /// <param name="effectType">Efekt tipi</param>
    /// <param name="progress">İlerleme (0.0 - 1.0)</param>
    /// <returns>Opacity değeri (0-255)</returns>
    byte CalculateOpacity(EffectType effectType, double progress);
}
