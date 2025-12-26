using System;
using System.Timers;
using LEDTabelam.Models;
using SkiaSharp;

namespace LEDTabelam.Services;

/// <summary>
/// Geçiş efekti servisi implementasyonu
/// Requirements: 15.7
/// </summary>
public class TransitionService : ITransitionService, IDisposable
{
    private readonly Timer _timer;
    private SKBitmap? _fromBitmap;
    private SKBitmap? _toBitmap;
    private TransitionType _currentTransition;
    private DateTime _startTime;
    private int _durationMs;
    private Action<SKBitmap>? _onFrame;
    private Action? _onComplete;
    private bool _isTransitioning;
    private bool _disposed;

    /// <summary>
    /// Minimum geçiş süresi (ms)
    /// </summary>
    public const int MinDurationMs = 100;

    /// <summary>
    /// Maksimum geçiş süresi (ms)
    /// </summary>
    public const int MaxDurationMs = 2000;

    /// <summary>
    /// Varsayılan geçiş süresi (ms)
    /// </summary>
    public const int DefaultTransitionDurationMs = 300;

    /// <summary>
    /// Timer interval (ms) - ~60 FPS
    /// </summary>
    private const double TimerIntervalMs = 16.67;

    public TransitionService()
    {
        _timer = new Timer(TimerIntervalMs);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        DefaultDurationMs = DefaultTransitionDurationMs;
    }

    /// <inheritdoc/>
    public bool IsTransitioning => _isTransitioning;

    /// <inheritdoc/>
    public int DefaultDurationMs { get; set; }

    /// <inheritdoc/>
    public SKBitmap ApplyTransition(SKBitmap? fromBitmap, SKBitmap toBitmap, TransitionType transitionType, double progress)
    {
        if (toBitmap == null)
            throw new ArgumentNullException(nameof(toBitmap));

        progress = Math.Clamp(progress, 0.0, 1.0);

        // If no transition or progress is complete, return target
        if (transitionType == TransitionType.None || progress >= 1.0)
        {
            return CloneBitmap(toBitmap);
        }

        // If no source bitmap, just fade in the target
        if (fromBitmap == null)
        {
            return ApplyFadeIn(toBitmap, progress);
        }

        return transitionType switch
        {
            TransitionType.Fade => ApplyFade(fromBitmap, toBitmap, progress),
            TransitionType.SlideLeft => ApplySlideLeft(fromBitmap, toBitmap, progress),
            TransitionType.SlideRight => ApplySlideRight(fromBitmap, toBitmap, progress),
            _ => CloneBitmap(toBitmap)
        };
    }

    /// <inheritdoc/>
    public void StartTransition(SKBitmap? fromBitmap, SKBitmap toBitmap, TransitionType transitionType,
        int durationMs, Action<SKBitmap> onFrame, Action? onComplete = null)
    {
        if (toBitmap == null)
            throw new ArgumentNullException(nameof(toBitmap));
        if (onFrame == null)
            throw new ArgumentNullException(nameof(onFrame));

        StopTransition();

        // No transition needed
        if (transitionType == TransitionType.None)
        {
            onFrame(CloneBitmap(toBitmap));
            onComplete?.Invoke();
            return;
        }

        _fromBitmap = fromBitmap != null ? CloneBitmap(fromBitmap) : null;
        _toBitmap = CloneBitmap(toBitmap);
        _currentTransition = transitionType;
        _durationMs = Math.Clamp(durationMs, MinDurationMs, MaxDurationMs);
        _onFrame = onFrame;
        _onComplete = onComplete;
        _startTime = DateTime.UtcNow;
        _isTransitioning = true;

        _timer.Start();
    }

    /// <inheritdoc/>
    public void StopTransition()
    {
        _timer.Stop();
        _isTransitioning = false;

        _fromBitmap?.Dispose();
        _fromBitmap = null;
        _toBitmap?.Dispose();
        _toBitmap = null;
        _onFrame = null;
        _onComplete = null;
    }

    #region Transition Effects

    private SKBitmap ApplyFade(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Draw source with decreasing alpha
        using (var paint = new SKPaint())
        {
            paint.Color = paint.Color.WithAlpha((byte)(255 * (1 - progress)));
            canvas.DrawBitmap(from, 0, 0, paint);
        }

        // Draw target with increasing alpha
        using (var paint = new SKPaint())
        {
            paint.Color = paint.Color.WithAlpha((byte)(255 * progress));
            canvas.DrawBitmap(to, 0, 0, paint);
        }

        return result;
    }

    private SKBitmap ApplyFadeIn(SKBitmap to, double progress)
    {
        var result = new SKBitmap(to.Width, to.Height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint();
        paint.Color = paint.Color.WithAlpha((byte)(255 * progress));
        canvas.DrawBitmap(to, 0, 0, paint);

        return result;
    }

    private SKBitmap ApplySlideLeft(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Calculate offset
        var offset = (float)(width * progress);

        // Draw source sliding out to the left
        canvas.DrawBitmap(from, -offset, 0);

        // Draw target sliding in from the right
        canvas.DrawBitmap(to, width - offset, 0);

        return result;
    }

    private SKBitmap ApplySlideRight(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Calculate offset
        var offset = (float)(width * progress);

        // Draw source sliding out to the right
        canvas.DrawBitmap(from, offset, 0);

        // Draw target sliding in from the left
        canvas.DrawBitmap(to, -width + offset, 0);

        return result;
    }

    #endregion

    #region Private Methods

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isTransitioning || _toBitmap == null) return;

        var elapsed = (DateTime.UtcNow - _startTime).TotalMilliseconds;
        var progress = Math.Min(elapsed / _durationMs, 1.0);

        try
        {
            var frame = ApplyTransition(_fromBitmap, _toBitmap, _currentTransition, progress);
            _onFrame?.Invoke(frame);

            if (progress >= 1.0)
            {
                var onComplete = _onComplete;
                StopTransition();
                onComplete?.Invoke();
            }
        }
        catch
        {
            StopTransition();
        }
    }

    private static SKBitmap CloneBitmap(SKBitmap source)
    {
        var clone = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(clone);
        canvas.DrawBitmap(source, 0, 0);
        return clone;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        StopTransition();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}
