using System;

namespace LEDTabelam.Services;

/// <summary>
/// Çözünürlük doğrulama servisi implementasyonu
/// Requirements: 1.5, 1.6
/// </summary>
public class ResolutionValidator : IResolutionValidator
{
    /// <summary>
    /// Minimum izin verilen çözünürlük değeri (1 piksel)
    /// Requirements: 1.5 - 1-512 piksel aralığında değerleri kabul et
    /// </summary>
    public int MinResolution => 1;

    /// <summary>
    /// Maksimum izin verilen çözünürlük değeri (512 piksel)
    /// Requirements: 1.5 - 1-512 piksel aralığında değerleri kabul et
    /// </summary>
    public int MaxResolution => 512;

    /// <inheritdoc/>
    public bool IsValidResolution(int value)
    {
        // Requirements: 1.5 - 1-512 piksel aralığında değerleri kabul et
        return value >= MinResolution && value <= MaxResolution;
    }

    /// <inheritdoc/>
    public int ClampResolution(int value)
    {
        return Math.Clamp(value, MinResolution, MaxResolution);
    }

    /// <inheritdoc/>
    public ResolutionValidationResult ValidateResolution(int value, int lastValidValue)
    {
        var result = new ResolutionValidationResult();

        if (IsValidResolution(value))
        {
            // Değer geçerli aralıkta
            result.IsValid = true;
            result.Value = value;
            result.WasCorrected = false;
        }
        else
        {
            // Requirements: 1.6 - Geçersiz değer girilirse hata mesajı göster ve son geçerli değeri koru
            result.IsValid = false;
            result.Value = lastValidValue;
            result.WasCorrected = true;

            if (value < MinResolution)
            {
                result.ErrorMessage = $"Çözünürlük değeri {MinResolution} pikselden küçük olamaz. Son geçerli değer korundu: {lastValidValue}";
            }
            else if (value > MaxResolution)
            {
                result.ErrorMessage = $"Çözünürlük değeri {MaxResolution} pikselden büyük olamaz. Son geçerli değer korundu: {lastValidValue}";
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public ResolutionPairValidationResult ValidateResolutionPair(int width, int height, int lastValidWidth, int lastValidHeight)
    {
        var result = new ResolutionPairValidationResult();

        var widthResult = ValidateResolution(width, lastValidWidth);
        var heightResult = ValidateResolution(height, lastValidHeight);

        result.Width = widthResult.Value;
        result.Height = heightResult.Value;
        result.WidthWasCorrected = widthResult.WasCorrected;
        result.HeightWasCorrected = heightResult.WasCorrected;
        result.IsValid = widthResult.IsValid && heightResult.IsValid;

        // Hata mesajlarını birleştir
        if (!result.IsValid)
        {
            var messages = new System.Collections.Generic.List<string>();
            
            if (!widthResult.IsValid && widthResult.ErrorMessage != null)
            {
                messages.Add($"Genişlik: {widthResult.ErrorMessage}");
            }
            
            if (!heightResult.IsValid && heightResult.ErrorMessage != null)
            {
                messages.Add($"Yükseklik: {heightResult.ErrorMessage}");
            }

            result.ErrorMessage = string.Join(" ", messages);
        }

        return result;
    }
}
