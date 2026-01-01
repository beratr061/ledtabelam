using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Animasyon servisi implementasyonu - MAUI için MainThread kullanır
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

    private Func<AnimationTick, SKBitmap?>? _renderCallback;

    private const int TargetFps = 60;
    private const double FrameIntervalMs = 1000.0 / TargetFps;

    public bool IsPlaying
    {
        get { lock (_lock) return _state == AnimationState.Playing; }
    }

    public bool IsPaused
    {
        get { lock (_lock) return _state == AnimationState.Paused; }
    }

    public double TotalTime
    {
        get { lock (_lock) return _totalTime; }
    }

    public event Action<AnimationState>? StateChanged;
    public event Action<AnimationTick>? OnTick;
    public event Action<RenderedFrame>? OnFrameReady;

    public void Start()
    {
        if (_disposed) return;

        lock (_lock)
        {
            StopInternal();
            _totalTime = 0;
            _frameNumber = 0;
            _state = AnimationState.Playing;
        }

        _cts = new CancellationTokenSource();
        _animationTask = Task.Run(() => AnimationLoop(_cts.Token));

        NotifyStateChanged(AnimationState.Playing);
    }

    public void Stop()
    {
        if (_disposed) return;

        lock (_lock)
        {
            StopInternal();
            _totalTime = 0;
            _frameNumber = 0;
            _state = AnimationState.Stopped;
        }

        NotifyStateChanged(AnimationState.Stopped);
    }

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

    public void SetRenderCallback(Func<AnimationTick, SKBitmap?>? renderCallback)
    {
        lock (_lock)
        {
            _renderCallback = renderCallback;
        }
    }

    private async Task AnimationLoop(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastUpdate = stopwatch.Elapsed.TotalSeconds;

        while (!ct.IsCancellationRequested)
        {
            AnimationState currentState;
            Func<AnimationTick, SKBitmap?>? renderCallback;

            lock (_lock)
            {
                currentState = _state;
                renderCallback = _renderCallback;
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

            deltaTime = Math.Min(deltaTime, 0.1);

            long frameNum;
            double totalTime;
            lock (_lock)
            {
                _totalTime += deltaTime;
                _frameNumber++;
                frameNum = _frameNumber;
                totalTime = _totalTime;
            }

            var tick = new AnimationTick
            {
                DeltaTime = deltaTime,
                TotalTime = totalTime,
                FrameNumber = frameNum
            };

            OnTick?.Invoke(tick);

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

                    // MAUI MainThread kullan
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            OnFrameReady?.Invoke(frame);
                        }
                        finally
                        {
                            // UI kullandıktan sonra dispose et
                        }
                    });
                }
            }

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
        MainThread.BeginInvokeOnMainThread(() => StateChanged?.Invoke(state));
    }

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
