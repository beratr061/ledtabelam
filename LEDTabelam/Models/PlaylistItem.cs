using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Playlist mesaj öğesi
/// Requirements: 15.1, 15.2, 15.7
/// </summary>
public class PlaylistItem : ReactiveObject
{
    private int _order;
    private string _text = string.Empty;
    private int _durationSeconds = 3;
    private TransitionType _transition = TransitionType.Fade;

    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int Order
    {
        get => _order;
        set => this.RaiseAndSetIfChanged(ref _order, value);
    }

    /// <summary>
    /// Mesaj metni
    /// </summary>
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    /// <summary>
    /// Gösterim süresi (saniye)
    /// Varsayılan: 3 saniye
    /// </summary>
    public int DurationSeconds
    {
        get => _durationSeconds;
        set => this.RaiseAndSetIfChanged(ref _durationSeconds, value);
    }

    /// <summary>
    /// Geçiş efekti tipi
    /// </summary>
    public TransitionType Transition
    {
        get => _transition;
        set => this.RaiseAndSetIfChanged(ref _transition, value);
    }
}
