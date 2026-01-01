using System;
using System.Threading;
using System.Threading.Tasks;
using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.Services;

/// <summary>
/// Efekt servisi implementasyonu
/// İçerik öğelerine giriş/çıkış efektleri uygular
/// </summary>
public class EffectService : IEffectService
{
    private CancellationTokenSource? _effectCts;
    private bool _isPlaying;
    private const int FrameDelayMs = 16; // ~60 FPS

    /// <inheritdoc/>
    public bool IsPlaying => _isPlaying;

    /// <inheritdoc/>
    public void ApplyEntryEffect(ContentItem content, SKCanvas canvas, SKBitmap sourceBitmap, double progress)
    {
        if (content == null || canvas == null || sourceBitmap == null)
            return;

        var effect = content.EntryEffect;
        ApplyEffect(canvas, sourceBitmap, effect, progress, content.X, content.Y);
    }

    /// <inheritdoc/>
    public void ApplyExitEffect(ContentItem content, SKCanvas canvas, SKBitmap sourceBitmap, double progress)
    {
        if (content == null || canvas == null || sourceBitmap == null)
            return;

        var effect = content.ExitEffect;
        // Exit effect progress is inverted (1.0 -> 0.0)
        ApplyEffect(canvas, sourceBitmap, effect, 1.0 - progress, content.X, content.Y);
    }

    /// <inheritdoc/>
    public async Task PlayEffectAsync(ContentItem content, EffectConfig effect, Action<double> renderCallback, CancellationToken cancellationToken = default)
    {
        if (content == null || effect == null || renderCallback == null)
            return;

        // Immediate effect - no animation needed
        if (effect.EffectType == EffectType.Immediate || effect.EffectType == EffectType.None)
        {
            renderCallback(1.0);
            return;
        }

        StopEffect();
        _effectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isPlaying = true;

        try
        {
            var startTime = DateTime.UtcNow;
            var duration = TimeSpan.FromMilliseconds(Math.Max(1, effect.SpeedMs));

            while (!_effectCts.Token.IsCancellationRequested)
            {
                var elapsed = DateTime.UtcNow - startTime;
                var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration.TotalMilliseconds);

                renderCallback(progress);

                if (progress >= 1.0)
                    break;

                await Task.Delay(FrameDelayMs, _effectCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Effect was cancelled
        }
        finally
        {
            _isPlaying = false;
        }
    }

    /// <inheritdoc/>
    public void StopEffect()
    {
        if (_effectCts != null)
        {
            _effectCts.Cancel();
            _effectCts.Dispose();
            _effectCts = null;
        }
        _isPlaying = false;
    }

    /// <inheritdoc/>
    public SKMatrix CalculateTransform(EffectType effectType, EffectDirection direction, double progress, SKRect bounds)
    {
        // Handle NaN and Infinity, then clamp to valid range
        progress = SanitizeProgress(progress);

        return effectType switch
        {
            EffectType.SlideIn => CalculateSlideTransform(direction, progress, bounds),
            EffectType.FadeIn => SKMatrix.Identity, // Fade doesn't use transform
            EffectType.Immediate => SKMatrix.Identity,
            EffectType.None => SKMatrix.Identity,
            _ => SKMatrix.Identity
        };
    }

    /// <inheritdoc/>
    public byte CalculateOpacity(EffectType effectType, double progress)
    {
        // Handle NaN and Infinity, then clamp to valid range
        progress = SanitizeProgress(progress);

        return effectType switch
        {
            EffectType.FadeIn => (byte)(255 * progress),
            EffectType.SlideIn => 255, // Slide doesn't affect opacity
            EffectType.Immediate => 255,
            EffectType.None => 255,
            _ => 255
        };
    }

    /// <summary>
    /// Sanitizes progress value by handling NaN, Infinity, and clamping to [0.0, 1.0]
    /// </summary>
    private static double SanitizeProgress(double progress)
    {
        if (double.IsNaN(progress) || double.IsNegativeInfinity(progress))
            return 0.0;
        if (double.IsPositiveInfinity(progress))
            return 1.0;
        return Math.Clamp(progress, 0.0, 1.0);
    }

    private void ApplyEffect(SKCanvas canvas, SKBitmap sourceBitmap, EffectConfig effect, double progress, int x, int y)
    {
        var bounds = new SKRect(x, y, x + sourceBitmap.Width, y + sourceBitmap.Height);
        var transform = CalculateTransform(effect.EffectType, effect.Direction, progress, bounds);
        var opacity = CalculateOpacity(effect.EffectType, progress);

        canvas.Save();

        // Apply transform
        if (!transform.IsIdentity)
        {
            canvas.SetMatrix(transform);
        }

        // Apply opacity
        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(opacity)
        };

        canvas.DrawBitmap(sourceBitmap, x, y, paint);
        canvas.Restore();
    }

    private static SKMatrix CalculateSlideTransform(EffectDirection direction, double progress, SKRect bounds)
    {
        float offsetX = 0;
        float offsetY = 0;
        float remainingProgress = (float)(1.0 - progress);

        switch (direction)
        {
            case EffectDirection.Left:
                offsetX = -bounds.Width * remainingProgress;
                break;
            case EffectDirection.Right:
                offsetX = bounds.Width * remainingProgress;
                break;
            case EffectDirection.Up:
                offsetY = -bounds.Height * remainingProgress;
                break;
            case EffectDirection.Down:
                offsetY = bounds.Height * remainingProgress;
                break;
        }

        return SKMatrix.CreateTranslation(offsetX, offsetY);
    }
}
