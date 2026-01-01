using CommunityToolkit.Mvvm.ComponentModel;

namespace LEDTabelam.Maui.Models;

/// <summary>
/// Playlist mesaj öğesi
/// </summary>
public partial class PlaylistItem : ObservableObject
{
    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private int _durationSeconds = 3;

    [ObservableProperty]
    private TransitionType _transition = TransitionType.Fade;
}
