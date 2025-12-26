using System;
using System.Timers;

namespace LEDTabelam.Services;

/// <summary>
/// Animasyon servisi implementasyonu - Kayan yazı ve geçiş animasyonları
/// Requirements: 8.1, 8.2, 8.3, 8.4, 8.5
/// </summary>
public class AnimationService : IAnimationService, IDisposable
{
    private readonly Timer _timer;
    private AnimationState _state = AnimationState.Stopped;
    private int _currentOffset;
    private int _speed = 20; // Varsayılan: 20 px/s (Requirement 8.5)
    private DateTime _lastUpdate;
    private double _accumulatedPixels;
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
    /// Timer interval (ms) - 60 FPS için ~16.67ms
    /// </summary>
    private const double TimerIntervalMs = 16.67;

    public AnimationService()
    {
        _timer = new Timer(TimerIntervalMs);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
    }

    /// <inheritdoc/>
    public int CurrentOffset => _currentOffset;

    /// <inheritdoc/>
    public bool IsPlaying => _state == AnimationState.Playing;

    /// <inheritdoc/>
    public bool IsPaused => _state == AnimationState.Paused;

    /// <inheritdoc/>
    public int Speed => _speed;

    /// <inheritdoc/>
    public event Action<AnimationState>? StateChanged;

    /// <inheritdoc/>
    public event Action<int>? OnFrameUpdate;

    /// <inheritdoc/>
    public void StartScrollAnimation(int speed)
    {
        if (_disposed) return;

        // State machine: Stopped -> Playing (valid)
        // Playing -> Playing (restart, valid)
        // Paused -> Playing (resume, but this method restarts)
        SetSpeed(speed);
        _currentOffset = 0;
        _accumulatedPixels = 0;
        _lastUpdate = DateTime.UtcNow;
        
        _state = AnimationState.Playing;
        _timer.Start();
        
        StateChanged?.Invoke(_state);
        OnFrameUpdate?.Invoke(_currentOffset);
    }

    /// <inheritdoc/>
    public void StopAnimation()
    {
        if (_disposed) return;

        // State machine: Any state -> Stopped (valid)
        _timer.Stop();
        _currentOffset = 0;
        _accumulatedPixels = 0;
        _state = AnimationState.Stopped;
        
        StateChanged?.Invoke(_state);
        OnFrameUpdate?.Invoke(_currentOffset);
    }

    /// <inheritdoc/>
    public void PauseAnimation()
    {
        if (_disposed) return;

        // State machine: Playing -> Paused (valid)
        // Stopped -> Paused (invalid, ignore)
        // Paused -> Paused (no-op)
        if (_state != AnimationState.Playing) return;

        _timer.Stop();
        _state = AnimationState.Paused;
        
        StateChanged?.Invoke(_state);
    }

    /// <inheritdoc/>
    public void ResumeAnimation()
    {
        if (_disposed) return;

        // State machine: Paused -> Playing (valid)
        // Stopped -> Playing (invalid, ignore)
        // Playing -> Playing (no-op)
        if (_state != AnimationState.Paused) return;

        _lastUpdate = DateTime.UtcNow;
        _accumulatedPixels = 0;
        _state = AnimationState.Playing;
        _timer.Start();
        
        StateChanged?.Invoke(_state);
    }

    /// <inheritdoc/>
    public void SetSpeed(int speed)
    {
        _speed = Math.Clamp(speed, MinSpeed, MaxSpeed);
    }

    /// <inheritdoc/>
    public void SetOffset(int offset)
    {
        _currentOffset = Math.Max(0, offset);
        _accumulatedPixels = 0;
        OnFrameUpdate?.Invoke(_currentOffset);
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_state != AnimationState.Playing) return;

        var now = DateTime.UtcNow;
        var elapsed = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // Piksel hesaplama: hız (px/s) * geçen süre (s)
        _accumulatedPixels += _speed * elapsed;

        // Tam piksel sayısına ulaşıldığında offset'i güncelle
        if (_accumulatedPixels >= 1.0)
        {
            var pixelsToMove = (int)_accumulatedPixels;
            _accumulatedPixels -= pixelsToMove;
            _currentOffset += pixelsToMove;
            
            OnFrameUpdate?.Invoke(_currentOffset);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
