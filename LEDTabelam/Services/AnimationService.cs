using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace LEDTabelam.Services;

/// <summary>
/// Animasyon servisi implementasyonu - Kayan yazı ve geçiş animasyonları
/// Requirements: 8.1, 8.2, 8.3, 8.4, 8.5
/// Background thread üzerinde çalışır, UI donmalarını önler
/// </summary>
public class AnimationService : IAnimationService, IDisposable
{
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _animationTask;
    
    private AnimationState _state = AnimationState.Stopped;
    private int _currentOffset;
    private int _speed = 20; // Varsayılan: 20 px/s (Requirement 8.5)
    private bool _disposed;

    /// <summary>
    /// Minimum hız (piksel/saniye)
    /// </summary>
    public const int MinSpeed = 1;

    /// <summary>
    /// Maksimum hız (piksel/saniye)
    /// </summary>
    public const int MaxSpeed = 100;

    /// <summary>
    /// Varsayılan hız (piksel/saniye)
    /// </summary>
    public const int DefaultSpeed = 20;

    /// <summary>
    /// Target frame rate (60 FPS)
    /// </summary>
    private const int TargetFps = 60;
    private const double FrameIntervalMs = 1000.0 / TargetFps;

    public AnimationService()
    {
    }

    /// <inheritdoc/>
    public int CurrentOffset
    {
        get { lock (_lock) return _currentOffset; }
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
    public int Speed
    {
        get { lock (_lock) return _speed; }
    }

    /// <inheritdoc/>
    public event Action<AnimationState>? StateChanged;

    /// <inheritdoc/>
    public event Action<int>? OnFrameUpdate;

    /// <inheritdoc/>
    public void StartScrollAnimation(int speed)
    {
        if (_disposed) return;

        lock (_lock)
        {
            // Mevcut animasyonu durdur
            StopAnimationInternal();

            SetSpeed(speed);
            _currentOffset = 0;
            _state = AnimationState.Playing;
        }

        // Yeni animasyon task'ı başlat
        _cts = new CancellationTokenSource();
        _animationTask = Task.Run(() => AnimationLoop(_cts.Token));

        NotifyStateChanged(AnimationState.Playing);
        NotifyFrameUpdate(0);
    }

    /// <inheritdoc/>
    public void StopAnimation()
    {
        if (_disposed) return;

        lock (_lock)
        {
            StopAnimationInternal();
            _currentOffset = 0;
            _state = AnimationState.Stopped;
        }

        NotifyStateChanged(AnimationState.Stopped);
        NotifyFrameUpdate(0);
    }

    /// <inheritdoc/>
    public void PauseAnimation()
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
    public void ResumeAnimation()
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
    public void SetSpeed(int speed)
    {
        lock (_lock)
        {
            _speed = Math.Clamp(speed, MinSpeed, MaxSpeed);
        }
    }

    /// <inheritdoc/>
    public void SetOffset(int offset)
    {
        lock (_lock)
        {
            _currentOffset = Math.Max(0, offset);
        }
        NotifyFrameUpdate(_currentOffset);
    }

    /// <summary>
    /// Background thread üzerinde çalışan animasyon döngüsü
    /// </summary>
    private async Task AnimationLoop(CancellationToken ct)
    {
        var lastUpdate = DateTime.UtcNow;
        double accumulatedPixels = 0;

        while (!ct.IsCancellationRequested)
        {
            AnimationState currentState;
            int currentSpeed;

            lock (_lock)
            {
                currentState = _state;
                currentSpeed = _speed;
            }

            if (currentState != AnimationState.Playing)
            {
                // Paused veya Stopped durumunda bekle
                await Task.Delay(50, ct).ConfigureAwait(false);
                lastUpdate = DateTime.UtcNow;
                accumulatedPixels = 0;
                continue;
            }

            var now = DateTime.UtcNow;
            var elapsed = (now - lastUpdate).TotalSeconds;
            lastUpdate = now;

            // Piksel hesaplama: hız (px/s) * geçen süre (s)
            accumulatedPixels += currentSpeed * elapsed;

            // Tam piksel sayısına ulaşıldığında offset'i güncelle
            if (accumulatedPixels >= 1.0)
            {
                int pixelsToMove = (int)accumulatedPixels;
                accumulatedPixels -= pixelsToMove;

                int newOffset;
                lock (_lock)
                {
                    _currentOffset += pixelsToMove;
                    newOffset = _currentOffset;
                }

                // UI thread'e bildir
                NotifyFrameUpdate(newOffset);
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

    private void StopAnimationInternal()
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
        
        // UI thread'e marshal et
        Dispatcher.UIThread.Post(() => StateChanged?.Invoke(state));
    }

    private void NotifyFrameUpdate(int offset)
    {
        if (OnFrameUpdate == null) return;
        
        // UI thread'e marshal et
        Dispatcher.UIThread.Post(() => OnFrameUpdate?.Invoke(offset));
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        lock (_lock)
        {
            StopAnimationInternal();
        }
        
        GC.SuppressFinalize(this);
    }
}
