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
            TransitionType.SlideUp => ApplySlideUp(fromBitmap, toBitmap, progress),
            TransitionType.SlideDown => ApplySlideDown(fromBitmap, toBitmap, progress),
            TransitionType.Blink => ApplyBlink(fromBitmap, toBitmap, progress),
            TransitionType.Laser => ApplyLaser(fromBitmap, toBitmap, progress),
            TransitionType.Curtain => ApplyCurtain(fromBitmap, toBitmap, progress),
            TransitionType.Dissolve => ApplyDissolve(fromBitmap, toBitmap, progress),
            TransitionType.Wipe => ApplyWipe(fromBitmap, toBitmap, progress),
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

    private SKBitmap ApplySlideUp(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        var offset = (float)(height * progress);

        // Draw source sliding out to the top
        canvas.DrawBitmap(from, 0, -offset);

        // Draw target sliding in from the bottom
        canvas.DrawBitmap(to, 0, height - offset);

        return result;
    }

    private SKBitmap ApplySlideDown(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        var offset = (float)(height * progress);

        // Draw source sliding out to the bottom
        canvas.DrawBitmap(from, 0, offset);

        // Draw target sliding in from the top
        canvas.DrawBitmap(to, 0, -height + offset);

        return result;
    }

    private SKBitmap ApplyBlink(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Blink effect: alternate between from and to based on progress
        // 3 blinks during transition
        var blinkPhase = (int)(progress * 6) % 2;
        
        if (blinkPhase == 0)
        {
            canvas.DrawBitmap(from, 0, 0);
        }
        else
        {
            canvas.DrawBitmap(to, 0, 0);
        }

        return result;
    }

    private SKBitmap ApplyLaser(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Laser effect: reveal target from left to right, column by column
        var revealX = (int)(width * progress);

        // Draw revealed portion of target
        if (revealX > 0)
        {
            var srcRect = new SKRect(0, 0, revealX, height);
            var destRect = new SKRect(0, 0, revealX, height);
            canvas.DrawBitmap(to, srcRect, destRect);
        }

        // Draw remaining portion of source
        if (revealX < width)
        {
            var srcRect = new SKRect(revealX, 0, width, height);
            var destRect = new SKRect(revealX, 0, width, height);
            canvas.DrawBitmap(from, srcRect, destRect);
        }

        // Draw laser line effect at the reveal edge
        if (revealX > 0 && revealX < width)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.White,
                StrokeWidth = 2,
                IsAntialias = false
            };
            canvas.DrawLine(revealX, 0, revealX, height, paint);
        }

        return result;
    }

    private SKBitmap ApplyCurtain(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Curtain effect: open from center
        var halfWidth = width / 2;
        var openAmount = (int)(halfWidth * progress);

        // Draw target in the center opening
        if (openAmount > 0)
        {
            var centerStart = halfWidth - openAmount;
            var centerEnd = halfWidth + openAmount;
            var srcRect = new SKRect(centerStart, 0, centerEnd, height);
            var destRect = new SKRect(centerStart, 0, centerEnd, height);
            canvas.DrawBitmap(to, srcRect, destRect);
        }

        // Draw left curtain (source)
        if (halfWidth - openAmount > 0)
        {
            var srcRect = new SKRect(0, 0, halfWidth - openAmount, height);
            var destRect = new SKRect(0, 0, halfWidth - openAmount, height);
            canvas.DrawBitmap(from, srcRect, destRect);
        }

        // Draw right curtain (source)
        if (halfWidth + openAmount < width)
        {
            var srcRect = new SKRect(halfWidth + openAmount, 0, width, height);
            var destRect = new SKRect(halfWidth + openAmount, 0, width, height);
            canvas.DrawBitmap(from, srcRect, destRect);
        }

        return result;
    }

    private SKBitmap ApplyDissolve(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Draw source first
        canvas.DrawBitmap(from, 0, 0);

        // Dissolve effect: randomly reveal pixels of target
        // Use a deterministic pattern based on pixel position
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Create a pseudo-random threshold based on position
                var threshold = ((x * 7 + y * 13) % 100) / 100.0;
                
                if (progress > threshold && x < to.Width && y < to.Height)
                {
                    var pixel = to.GetPixel(x, y);
                    result.SetPixel(x, y, pixel);
                }
            }
        }

        return result;
    }

    private SKBitmap ApplyWipe(SKBitmap from, SKBitmap to, double progress)
    {
        var width = Math.Max(from.Width, to.Width);
        var height = Math.Max(from.Height, to.Height);
        var result = new SKBitmap(width, height);

        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);

        // Wipe effect: diagonal wipe from top-left to bottom-right
        var wipePosition = (int)((width + height) * progress);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixelPosition = x + y;
                
                if (pixelPosition < wipePosition)
                {
                    // Show target
                    if (x < to.Width && y < to.Height)
                    {
                        var pixel = to.GetPixel(x, y);
                        result.SetPixel(x, y, pixel);
                    }
                }
                else
                {
                    // Show source
                    if (x < from.Width && y < from.Height)
                    {
                        var pixel = from.GetPixel(x, y);
                        result.SetPixel(x, y, pixel);
                    }
                }
            }
        }

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
