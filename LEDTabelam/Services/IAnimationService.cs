using System;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Animasyon tick verisi - Her zone kendi hızıyla offset hesaplar
/// </summary>
public readonly struct AnimationTick
{
    /// <summary>
    /// Son frame'den bu yana geçen süre (saniye)
    /// </summary>
    public double DeltaTime { get; init; }
    
    /// <summary>
    /// Toplam geçen süre (saniye)
    /// </summary>
    public double TotalTime { get; init; }
    
    /// <summary>
    /// Frame numarası
    /// </summary>
    public long FrameNumber { get; init; }
}

/// <summary>
/// Render edilmiş frame verisi - UI thread'e gönderilir
/// </summary>
public sealed class RenderedFrame : IDisposable
{
    /// <summary>
    /// Render edilmiş bitmap (thread-safe, UI'da kullanılabilir)
    /// </summary>
    public SKBitmap? Bitmap { get; init; }
    
    /// <summary>
    /// Frame numarası
    /// </summary>
    public long FrameNumber { get; init; }
    
    /// <summary>
    /// Render süresi (ms)
    /// </summary>
    public double RenderTimeMs { get; init; }
    
    private bool _disposed;
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Bitmap?.Dispose();
    }
}

/// <summary>
/// Animasyon servisi interface'i - Kayan yazı ve geçiş animasyonları
/// Requirements: 8.1, 8.2, 8.3
/// DeltaTime tabanlı: Her zone kendi hızıyla offset hesaplar
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Animasyonu başlatır (global tick yayını)
    /// </summary>
    void Start();

    /// <summary>
    /// Animasyonu durdurur ve sıfırlar
    /// </summary>
    void Stop();

    /// <summary>
    /// Animasyonu duraklatır
    /// </summary>
    void Pause();

    /// <summary>
    /// Duraklatılmış animasyonu devam ettirir
    /// </summary>
    void Resume();

    /// <summary>
    /// Animasyon oynatılıyor mu
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Animasyon duraklatılmış mı
    /// </summary>
    bool IsPaused { get; }
    
    /// <summary>
    /// Toplam geçen süre (saniye)
    /// </summary>
    double TotalTime { get; }

    /// <summary>
    /// Animasyon durumu değiştiğinde tetiklenir
    /// </summary>
    event Action<AnimationState>? StateChanged;

    /// <summary>
    /// Her frame'de tetiklenir - Zone'lar bu tick ile kendi offset'lerini hesaplar
    /// Background thread'de çağrılır
    /// </summary>
    event Action<AnimationTick>? OnTick;
    
    /// <summary>
    /// Render edilmiş frame hazır olduğunda tetiklenir
    /// UI thread'de çağrılır - doğrudan görüntülenebilir
    /// </summary>
    event Action<RenderedFrame>? OnFrameReady;
    
    /// <summary>
    /// Render callback'i ayarlar - Background thread'de çağrılır
    /// </summary>
    /// <param name="renderCallback">Tick alıp bitmap döndüren fonksiyon</param>
    void SetRenderCallback(Func<AnimationTick, SKBitmap?>? renderCallback);
    
    // Legacy uyumluluk için (deprecated)
    [Obsolete("Use OnTick event instead. Each zone should calculate its own offset.")]
    int CurrentOffset { get; }
    
    [Obsolete("Use Start() instead")]
    void StartScrollAnimation(int speed);
    
    [Obsolete("Use Stop() instead")]
    void StopAnimation();
    
    [Obsolete("Use Pause() instead")]
    void PauseAnimation();
    
    [Obsolete("Use Resume() instead")]
    void ResumeAnimation();
    
    [Obsolete("Zone-specific speed should be used instead")]
    void SetSpeed(int speed);
    
    [Obsolete("Zone-specific offset should be used instead")]
    void SetOffset(int offset);
    
    [Obsolete("Zone-specific speed should be used instead")]
    int Speed { get; }
    
    [Obsolete("Use OnTick event instead")]
    event Action<int>? OnFrameUpdate;
}

/// <summary>
/// Animasyon durumu
/// </summary>
public enum AnimationState
{
    /// <summary>
    /// Animasyon durdurulmuş (başlangıç pozisyonunda)
    /// </summary>
    Stopped,

    /// <summary>
    /// Animasyon oynatılıyor
    /// </summary>
    Playing,

    /// <summary>
    /// Animasyon duraklatılmış
    /// </summary>
    Paused
}
