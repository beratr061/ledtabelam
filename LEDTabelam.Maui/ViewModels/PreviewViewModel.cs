using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LEDTabelam.Maui.Models;
using SkiaSharp;

namespace LEDTabelam.Maui.ViewModels;

/// <summary>
/// Önizleme paneli ViewModel'i
/// LED tabela önizlemesi ve navigasyonu yönetir
/// Requirements: 4.1, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8
/// </summary>
public partial class PreviewViewModel : ObservableObject
{
    private CancellationTokenSource? _playbackCts;
    private const int MinZoom = 50;
    private const int MaxZoom = 400;
    private const int ZoomStep = 25;
    private const int DefaultPlaybackDelayMs = 3000;

    [ObservableProperty]
    private SKBitmap? _previewBitmap;

    [ObservableProperty]
    private int _zoomLevel = 100;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private bool _isPlaying = false;

    [ObservableProperty]
    private bool _isFullscreen = false;

    [ObservableProperty]
    private ProgramNode? _currentProgram;

    [ObservableProperty]
    private ContentItem? _currentContent;

    [ObservableProperty]
    private DisplaySettings _displaySettings = new();

    /// <summary>
    /// Zoom seviyesi yüzde olarak (örn: "100%")
    /// </summary>
    public string ZoomLevelText => $"{ZoomLevel}%";

    /// <summary>
    /// Sayfa bilgisi (örn: "1/4")
    /// </summary>
    public string PageInfo => $"{CurrentPage}/{TotalPages}";

    /// <summary>
    /// Zoom artırılabilir mi?
    /// </summary>
    public bool CanZoomIn => ZoomLevel < MaxZoom;

    /// <summary>
    /// Zoom azaltılabilir mi?
    /// </summary>
    public bool CanZoomOut => ZoomLevel > MinZoom;

    /// <summary>
    /// Sonraki sayfaya geçilebilir mi?
    /// </summary>
    public bool CanGoNext => CurrentPage < TotalPages;

    /// <summary>
    /// Önceki sayfaya geçilebilir mi?
    /// </summary>
    public bool CanGoPrevious => CurrentPage > 1;

    partial void OnZoomLevelChanged(int value)
    {
        OnPropertyChanged(nameof(ZoomLevelText));
        OnPropertyChanged(nameof(CanZoomIn));
        OnPropertyChanged(nameof(CanZoomOut));
    }

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(PageInfo));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        UpdateCurrentContent();
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(PageInfo));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
    }

    partial void OnCurrentProgramChanged(ProgramNode? value)
    {
        if (value != null)
        {
            TotalPages = Math.Max(1, value.Contents.Count);
            CurrentPage = 1;
        }
        else
        {
            TotalPages = 1;
            CurrentPage = 1;
        }
    }

    /// <summary>
    /// Programı önizleme için yükler
    /// </summary>
    public void LoadProgram(ProgramNode program)
    {
        StopPlayback();
        CurrentProgram = program;
        UpdateCurrentContent();
    }

    /// <summary>
    /// Önizleme bitmap'ini günceller
    /// </summary>
    public void UpdatePreview(SKBitmap? bitmap)
    {
        PreviewBitmap?.Dispose();
        PreviewBitmap = bitmap;
    }

    /// <summary>
    /// Zoom seviyesini artırır
    /// Requirement: 4.6, 4.7
    /// Property 6: Zoom Bounds Validation
    /// </summary>
    [RelayCommand]
    public void ZoomIn()
    {
        SetZoomLevel(ZoomLevel + ZoomStep);
    }

    /// <summary>
    /// Zoom seviyesini azaltır
    /// Requirement: 4.6, 4.7
    /// Property 6: Zoom Bounds Validation
    /// </summary>
    [RelayCommand]
    public void ZoomOut()
    {
        SetZoomLevel(ZoomLevel - ZoomStep);
    }

    /// <summary>
    /// Zoom seviyesini belirli bir değere ayarlar
    /// Requirement: 4.6, 4.7
    /// Property 6: Zoom Bounds Validation - Zoom %50-%400 aralığında sınırlandırılır
    /// </summary>
    public void SetZoomLevel(int level)
    {
        // Property 6: Zoom bounds validation - clamp to valid range
        ZoomLevel = Math.Clamp(level, MinZoom, MaxZoom);
    }

    /// <summary>
    /// Zoom seviyesini sıfırlar (%100)
    /// </summary>
    [RelayCommand]
    public void ResetZoom()
    {
        ZoomLevel = 100;
    }

    /// <summary>
    /// Sonraki sayfaya geçer
    /// Requirement: 4.4, 4.5
    /// Property 7: Page Navigation Consistency
    /// </summary>
    [RelayCommand]
    public void NextPage()
    {
        if (CurrentProgram == null || TotalPages <= 1)
            return;

        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
        else if (CurrentProgram.IsLoop)
        {
            // Loop modunda başa dön
            CurrentPage = 1;
        }
        // Loop değilse son sayfada kal
    }

    /// <summary>
    /// Önceki sayfaya geçer
    /// Requirement: 4.4, 4.5
    /// Property 7: Page Navigation Consistency
    /// </summary>
    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentProgram == null || TotalPages <= 1)
            return;

        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
        else if (CurrentProgram.IsLoop)
        {
            // Loop modunda sona git
            CurrentPage = TotalPages;
        }
        // Loop değilse ilk sayfada kal
    }

    /// <summary>
    /// Belirli bir sayfaya gider
    /// Property 7: Page Navigation Consistency - Sayfa [1, N] aralığında sınırlandırılır
    /// </summary>
    public void GoToPage(int page)
    {
        if (TotalPages <= 0)
        {
            CurrentPage = 1;
            return;
        }

        // Property 7: Page navigation consistency - clamp to valid range
        CurrentPage = Math.Clamp(page, 1, TotalPages);
    }

    /// <summary>
    /// Tam ekran modunu açar/kapatır
    /// Requirement: 4.8
    /// </summary>
    [RelayCommand]
    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    /// <summary>
    /// Oynatmayı başlatır/durdurur
    /// Requirement: 4.1
    /// </summary>
    [RelayCommand]
    public void TogglePlay()
    {
        if (IsPlaying)
        {
            StopPlayback();
        }
        else
        {
            StartPlayback();
        }
    }

    /// <summary>
    /// Oynatmayı başlatır
    /// </summary>
    [RelayCommand]
    public void Play()
    {
        if (!IsPlaying)
        {
            StartPlayback();
        }
    }

    /// <summary>
    /// Oynatmayı durdurur
    /// </summary>
    [RelayCommand]
    public void Stop()
    {
        StopPlayback();
    }

    private void StartPlayback()
    {
        if (CurrentProgram == null || CurrentProgram.Contents.Count == 0)
            return;

        StopPlayback();
        _playbackCts = new CancellationTokenSource();
        IsPlaying = true;

        _ = PlaybackLoopAsync(_playbackCts.Token);
    }

    private void StopPlayback()
    {
        if (_playbackCts != null)
        {
            _playbackCts.Cancel();
            _playbackCts.Dispose();
            _playbackCts = null;
        }
        IsPlaying = false;
    }

    private async Task PlaybackLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && CurrentProgram != null)
            {
                // Mevcut içeriğin süresini al
                var delay = CurrentContent?.DurationMs ?? DefaultPlaybackDelayMs;
                if (delay <= 0)
                    delay = DefaultPlaybackDelayMs;

                await Task.Delay(delay, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                // Sonraki sayfaya geç
                if (CurrentPage < TotalPages)
                {
                    CurrentPage++;
                }
                else if (CurrentProgram.IsLoop)
                {
                    CurrentPage = 1;
                }
                else
                {
                    // Loop değilse ve son sayfadaysa dur
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Playback cancelled
        }
        finally
        {
            IsPlaying = false;
        }
    }

    private void UpdateCurrentContent()
    {
        if (CurrentProgram == null || CurrentProgram.Contents.Count == 0)
        {
            CurrentContent = null;
            return;
        }

        var index = CurrentPage - 1;
        if (index >= 0 && index < CurrentProgram.Contents.Count)
        {
            CurrentContent = CurrentProgram.Contents[index];
        }
        else
        {
            CurrentContent = null;
        }
    }

    /// <summary>
    /// Kaynakları temizler
    /// </summary>
    public void Dispose()
    {
        StopPlayback();
        PreviewBitmap?.Dispose();
        PreviewBitmap = null;
    }
}
