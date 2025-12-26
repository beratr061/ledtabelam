using System;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Geçiş efekti servisi interface'i
/// Requirements: 15.7
/// </summary>
public interface ITransitionService
{
    /// <summary>
    /// Geçiş efekti uygular
    /// </summary>
    /// <param name="fromBitmap">Kaynak bitmap (null olabilir)</param>
    /// <param name="toBitmap">Hedef bitmap</param>
    /// <param name="transitionType">Geçiş tipi</param>
    /// <param name="progress">İlerleme (0.0 - 1.0)</param>
    /// <returns>Geçiş efekti uygulanmış bitmap</returns>
    SKBitmap ApplyTransition(SKBitmap? fromBitmap, SKBitmap toBitmap, TransitionType transitionType, double progress);

    /// <summary>
    /// Geçiş animasyonunu başlatır
    /// </summary>
    /// <param name="fromBitmap">Kaynak bitmap</param>
    /// <param name="toBitmap">Hedef bitmap</param>
    /// <param name="transitionType">Geçiş tipi</param>
    /// <param name="durationMs">Süre (milisaniye)</param>
    /// <param name="onFrame">Her frame'de çağrılacak callback</param>
    /// <param name="onComplete">Tamamlandığında çağrılacak callback</param>
    void StartTransition(SKBitmap? fromBitmap, SKBitmap toBitmap, TransitionType transitionType, 
        int durationMs, Action<SKBitmap> onFrame, Action? onComplete = null);

    /// <summary>
    /// Geçiş animasyonunu durdurur
    /// </summary>
    void StopTransition();

    /// <summary>
    /// Geçiş animasyonu devam ediyor mu
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// Varsayılan geçiş süresi (milisaniye)
    /// </summary>
    int DefaultDurationMs { get; set; }
}
