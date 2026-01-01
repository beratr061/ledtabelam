using System;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Animasyon tick verisi
/// </summary>
public readonly struct AnimationTick
{
    public double DeltaTime { get; init; }
    public double TotalTime { get; init; }
    public long FrameNumber { get; init; }
}

/// <summary>
/// Render edilmiş frame verisi
/// </summary>
public sealed class RenderedFrame : IDisposable
{
    public SKBitmap? Bitmap { get; init; }
    public long FrameNumber { get; init; }
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
/// Animasyon durumu
/// </summary>
public enum AnimationState
{
    Stopped,
    Playing,
    Paused
}

/// <summary>
/// Animasyon servisi interface'i
/// </summary>
public interface IAnimationService
{
    void Start();
    void Stop();
    void Pause();
    void Resume();

    bool IsPlaying { get; }
    bool IsPaused { get; }
    double TotalTime { get; }

    event Action<AnimationState>? StateChanged;
    event Action<AnimationTick>? OnTick;
    event Action<RenderedFrame>? OnFrameReady;

    void SetRenderCallback(Func<AnimationTick, SKBitmap?>? renderCallback);
}
