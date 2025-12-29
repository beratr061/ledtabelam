using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Animasyon servisi implementasyonu - DeltaTime tabanlı tick yayını
/// Requirements: 8.1, 8.2, 8.3, 8.4, 8.5
/// 
/// Mimari:
/// - Her frame'de DeltaTime içeren AnimationTick yayınlar
/// - Her Zone kendi ScrollSpeed'i ile offset hesaplar: Offset += DeltaTime * ZoneSpeed
/// - Render işlemi background thread'de yapılır, UI'a sadece bitmiş bitmap gönderilir
/// </summary>
public class AnimationService : IAnimationService, IDisposable
{
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _animationTask;
    
    private AnimationState _state = AnimationState.Stopped;
    private double _totalTime;
    private long _frameNumber;
    private bool _disposed;
    
    // Legacy uyumluluk için
    private int _legacyOffset;
    private int _legacySpeed = 20;
    
    // Render callback - background thread'de çağrılır
    private Func<AnimationTick, SKBitmap?>? _renderCallback;

    /// <summary>
    /// Target frame rate (60 FPS)
    /// </summary>
    private const int TargetFps = 60;
    private const double FrameIntervalMs = 1000.0 / TargetFps;

    public AnimationService()
    {
    }

    /// <inheritdoc/>
    public bool IsPlaying
    {
        get { lock (_lock) return _state == AnimationState.Playing; }
    }

    /// <inheritdoc/>
    public bool IsPaused
    {
        get { lock (_lock) return _state == AnimationState.Paused; }
    }
    
    /// <inheritdoc/>
    public double TotalTime
    {
        get { lock (_lock) return _totalTime; }
    }

    /// <inheritdoc/>
    public event Action<AnimationState>? StateChanged;

    /// <inheritdoc/>
    public event Action<AnimationTick>? OnTick;
    
    /// <inheritdoc/>
    public event Action<RenderedFrame>? OnFrameReady;

    // Legacy events
    public event Action<int>? OnFrameUpdate;

    /// <inheritdoc/>
    public void Start()
    {
        if (_disposed) return;

        lock (_lock)
        {
            StopInternal();
            _totalTime = 0;
            _frameNumber = 0;
            _legacyOffset = 0;
            _state = AnimationState.Playing;
        }

        _cts = new CancellationTokenSource();
        _animationTask = Task.Run(() => AnimationLoop(_cts.Token));

        NotifyStateChanged(AnimationState.Playing);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (_disposed) return;

        lock (_lock)
        {
            StopInternal();
            _totalTime = 0;
            _frameNumber = 0;
            _legacyOffset = 0;
            _state = AnimationState.Stopped;
        }

        NotifyStateChanged(AnimationState.Stopped);
    }

    /// <inheritdoc/>
    public void Pause()
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_state != AnimationState.Playing) return;
            _state = AnimationState.Paused;
        }

        NotifyStateChanged(AnimationState.Paused);
    }

    /// <inheritdoc/>
    public void Resume()
    {
        if (_disposed) return;

        bool shouldStart = false;
        lock (_lock)
        {
            if (_state != AnimationState.Paused) return;
            _state = AnimationState.Playing;
            shouldStart = _animationTask == null || _animationTask.IsCompleted;
        }

        if (shouldStart)
        {
            _cts = new CancellationTokenSource();
            _animationTask = Task.Run(() => AnimationLoop(_cts.Token));
        }

        NotifyStateChanged(AnimationState.Playing);
    }
    
    /// <inheritdoc/>
    public void SetRenderCallback(Func<AnimationTick, SKBitmap?>? renderCallback)
    {
        lock (_lock)
        {
            _renderCallback = renderCallback;
        }
    }

    /// <summary>
    /// Background thread üzerinde çalışan animasyon döngüsü
    /// DeltaTime tabanlı tick yayınlar, render işlemini burada yapar
    /// </summary>
    private async Task AnimationLoop(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastUpdate = stopwatch.Elapsed.TotalSeconds;
        double legacyAccumulated = 0;

        while (!ct.IsCancellationRequested)
        {
            AnimationState currentState;
            Func<AnimationTick, SKBitmap?>? renderCallback;
            int legacySpeed;

            lock (_lock)
            {
                currentState = _state;
                renderCallback = _renderCallback;
                legacySpeed = _legacySpeed;
            }

            if (currentState != AnimationState.Playing)
            {
                await Task.Delay(50, ct).ConfigureAwait(false);
                lastUpdate = stopwatch.Elapsed.TotalSeconds;
                continue;
            }

            var now = stopwatch.Elapsed.TotalSeconds;
            var deltaTime = now - lastUpdate;
            lastUpdate = now;

            // DeltaTime'ı makul sınırlar içinde tut (lag spike koruması)
            deltaTime = Math.Min(deltaTime, 0.1); // Max 100ms

            long frameNum;
            double totalTime;
            lock (_lock)
            {
                _totalTime += deltaTime;
                _frameNumber++;
                frameNum = _frameNumber;
                totalTime = _totalTime;
                
                // Legacy offset hesaplama
                legacyAccumulated += legacySpeed * deltaTime;
                if (legacyAccumulated >= 1.0)
                {
                    int pixels = (int)legacyAccumulated;
                    legacyAccumulated -= pixels;
                    _legacyOffset += pixels;
                }
            }

            var tick = new AnimationTick
            {
                DeltaTime = deltaTime,
                TotalTime = totalTime,
                FrameNumber = frameNum
            };

            // OnTick event'ini background thread'de çağır
            // Zone'lar bu tick ile kendi offset'lerini hesaplar
            OnTick?.Invoke(tick);

            // Render callback varsa, background thread'de render yap
            if (renderCallback != null)
            {
                var renderStart = stopwatch.Elapsed.TotalMilliseconds;
                SKBitmap? bitmap = null;
                
                try
                {
                    bitmap = renderCallback(tick);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Render error: {ex.Message}");
                }

                var renderTime = stopwatch.Elapsed.TotalMilliseconds - renderStart;

                if (bitmap != null && OnFrameReady != null)
                {
                    var frame = new RenderedFrame
                    {
                        Bitmap = bitmap,
                        FrameNumber = frameNum,
                        RenderTimeMs = renderTime
                    };

                    // Bitmiş frame'i UI thread'e gönder
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            OnFrameReady?.Invoke(frame);
                        }
                        finally
                        {
                            // UI kullandıktan sonra dispose et
                            // Not: UI tarafı bitmap'i kopyalamalı veya hemen kullanmalı
                        }
                    });
                }
            }

            // Legacy OnFrameUpdate
            if (OnFrameUpdate != null)
            {
                int offset;
                lock (_lock) offset = _legacyOffset;
                Dispatcher.UIThread.Post(() => OnFrameUpdate?.Invoke(offset));
            }

            // Frame rate kontrolü
            try
            {
                await Task.Delay((int)FrameIntervalMs, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void StopInternal()
    {
        _cts?.Cancel();
        try
        {
            _animationTask?.Wait(100);
        }
        catch { /* Ignore */ }
        _cts?.Dispose();
        _cts = null;
        _animationTask = null;
    }

    private void NotifyStateChanged(AnimationState state)
    {
        if (StateChanged == null) return;
        Dispatcher.UIThread.Post(() => StateChanged?.Invoke(state));
    }

    #region Legacy Compatibility (Deprecated)
    
    public int CurrentOffset
    {
        get { lock (_lock) return _legacyOffset; }
    }

    public int Speed
    {
        get { lock (_lock) return _legacySpeed; }
    }

    public void StartScrollAnimation(int speed)
    {
        SetSpeed(speed);
        Start();
    }

    public void StopAnimation() => Stop();
    public void PauseAnimation() => Pause();
    public void ResumeAnimation() => Resume();

    public void SetSpeed(int speed)
    {
        lock (_lock)
        {
            _legacySpeed = Math.Clamp(speed, 1, 100);
        }
    }

    public void SetOffset(int offset)
    {
        lock (_lock)
        {
            _legacyOffset = Math.Max(0, offset);
        }
        if (OnFrameUpdate != null)
        {
            Dispatcher.UIThread.Post(() => OnFrameUpdate?.Invoke(_legacyOffset));
        }
    }
    
    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        lock (_lock)
        {
            StopInternal();
            _renderCallback = null;
        }
        
        GC.SuppressFinalize(this);
    }
}
