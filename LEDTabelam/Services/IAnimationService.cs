using System;

namespace LEDTabelam.Services;

/// <summary>
/// Animasyon servisi interface'i - Kayan yazı ve geçiş animasyonları
/// Requirements: 8.1, 8.2, 8.3
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Kayan yazı animasyonunu başlatır
    /// </summary>
    /// <param name="speed">Hız (piksel/saniye, 1-100)</param>
    void StartScrollAnimation(int speed);

    /// <summary>
    /// Animasyonu durdurur ve başa döner
    /// </summary>
    void StopAnimation();

    /// <summary>
    /// Animasyonu duraklatır (mevcut pozisyonda kalır)
    /// </summary>
    void PauseAnimation();

    /// <summary>
    /// Duraklatılmış animasyonu devam ettirir
    /// </summary>
    void ResumeAnimation();

    /// <summary>
    /// Animasyon hızını ayarlar
    /// </summary>
    /// <param name="speed">Hız (piksel/saniye, 1-100)</param>
    void SetSpeed(int speed);

    /// <summary>
    /// Animasyonu belirtilen offset'e ayarlar
    /// </summary>
    /// <param name="offset">Piksel offset değeri</param>
    void SetOffset(int offset);

    /// <summary>
    /// Mevcut scroll offset değeri (piksel)
    /// </summary>
    int CurrentOffset { get; }

    /// <summary>
    /// Animasyon oynatılıyor mu
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Animasyon duraklatılmış mı
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Mevcut animasyon hızı (piksel/saniye)
    /// </summary>
    int Speed { get; }

    /// <summary>
    /// Animasyon durumu değiştiğinde tetiklenir
    /// </summary>
    event Action<AnimationState>? StateChanged;

    /// <summary>
    /// Her frame güncellemesinde tetiklenir
    /// </summary>
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
