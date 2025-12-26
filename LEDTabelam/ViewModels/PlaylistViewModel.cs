using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Timers;
using ReactiveUI;
using LEDTabelam.Models;
using LEDTabelam.Services;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Playlist ViewModel'i - Sıralı mesaj yönetimi
/// Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7
/// </summary>
public class PlaylistViewModel : ViewModelBase
{
    private readonly IAnimationService _animationService;
    private readonly Timer _playlistTimer;

    private PlaylistItem? _selectedItem;
    private PlaylistItem? _currentPlayingItem;
    private int _currentIndex = 0;
    private bool _isPlaying = false;
    private bool _isLoopEnabled = true;
    private int _defaultDuration = 3;
    private TransitionType _defaultTransition = TransitionType.Fade;

    #region Collections

    /// <summary>
    /// Playlist öğeleri
    /// </summary>
    public ObservableCollection<PlaylistItem> Items { get; } = new();

    #endregion

    #region Properties

    /// <summary>
    /// Seçili playlist öğesi
    /// </summary>
    public PlaylistItem? SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    /// <summary>
    /// Şu an oynatılan öğe
    /// </summary>
    public PlaylistItem? CurrentPlayingItem
    {
        get => _currentPlayingItem;
        private set => this.RaiseAndSetIfChanged(ref _currentPlayingItem, value);
    }

    /// <summary>
    /// Mevcut oynatma indeksi
    /// </summary>
    public int CurrentIndex
    {
        get => _currentIndex;
        private set => this.RaiseAndSetIfChanged(ref _currentIndex, value);
    }

    /// <summary>
    /// Playlist oynatılıyor mu
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        private set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    /// <summary>
    /// Döngü modu aktif mi
    /// </summary>
    public bool IsLoopEnabled
    {
        get => _isLoopEnabled;
        set => this.RaiseAndSetIfChanged(ref _isLoopEnabled, value);
    }

    /// <summary>
    /// Varsayılan gösterim süresi (saniye)
    /// </summary>
    public int DefaultDuration
    {
        get => _defaultDuration;
        set
        {
            var validValue = Math.Max(1, value);
            this.RaiseAndSetIfChanged(ref _defaultDuration, validValue);
        }
    }

    /// <summary>
    /// Varsayılan geçiş efekti
    /// </summary>
    public TransitionType DefaultTransition
    {
        get => _defaultTransition;
        set => this.RaiseAndSetIfChanged(ref _defaultTransition, value);
    }

    /// <summary>
    /// Geçiş efekti seçenekleri
    /// </summary>
    public TransitionType[] TransitionOptions { get; } = Enum.GetValues<TransitionType>();

    /// <summary>
    /// Playlist öğe sayısı
    /// </summary>
    public int ItemCount => Items.Count;

    /// <summary>
    /// Playlist boş mu
    /// </summary>
    public bool IsEmpty => Items.Count == 0;

    #endregion

    #region Commands

    /// <summary>
    /// Yeni mesaj ekleme komutu
    /// </summary>
    public ReactiveCommand<string, Unit> AddItemCommand { get; }

    /// <summary>
    /// Seçili mesajı silme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveSelectedCommand { get; }

    /// <summary>
    /// Tüm mesajları temizleme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }

    /// <summary>
    /// Seçili mesajı yukarı taşıma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveUpCommand { get; }

    /// <summary>
    /// Seçili mesajı aşağı taşıma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> MoveDownCommand { get; }

    /// <summary>
    /// Playlist oynatma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> PlayCommand { get; }

    /// <summary>
    /// Playlist duraklatma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> PauseCommand { get; }

    /// <summary>
    /// Playlist durdurma komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    /// <summary>
    /// Sonraki mesaja geçme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> NextCommand { get; }

    /// <summary>
    /// Önceki mesaja geçme komutu
    /// </summary>
    public ReactiveCommand<Unit, Unit> PreviousCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Mesaj değiştiğinde tetiklenir
    /// </summary>
    public event Action<PlaylistItem>? MessageChanged;

    /// <summary>
    /// Geçiş başladığında tetiklenir
    /// </summary>
    public event Action<PlaylistItem, PlaylistItem, TransitionType>? TransitionStarted;

    #endregion

    /// <summary>
    /// PlaylistViewModel constructor
    /// </summary>
    public PlaylistViewModel(IAnimationService animationService)
    {
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));

        // Timer oluştur
        _playlistTimer = new Timer();
        _playlistTimer.Elapsed += OnTimerElapsed;

        // Komutları oluştur
        AddItemCommand = ReactiveCommand.Create<string>(AddItem);

        var hasSelection = this.WhenAnyValue(x => x.SelectedItem).Select(x => x != null);
        RemoveSelectedCommand = ReactiveCommand.Create(RemoveSelected, hasSelection);

        var hasItems = this.WhenAnyValue(x => x.ItemCount).Select(x => x > 0);
        ClearAllCommand = ReactiveCommand.Create(ClearAll, hasItems);

        var canMoveUp = this.WhenAnyValue(x => x.SelectedItem, x => x.Items.Count,
            (item, _) => item != null && Items.IndexOf(item!) > 0);
        MoveUpCommand = ReactiveCommand.Create(MoveUp, canMoveUp);

        var canMoveDown = this.WhenAnyValue(x => x.SelectedItem, x => x.Items.Count,
            (item, _) => item != null && Items.IndexOf(item!) < Items.Count - 1);
        MoveDownCommand = ReactiveCommand.Create(MoveDown, canMoveDown);

        var canPlay = this.WhenAnyValue(x => x.IsPlaying, x => x.ItemCount,
            (playing, count) => !playing && count > 0);
        PlayCommand = ReactiveCommand.Create(Play, canPlay);

        var canPause = this.WhenAnyValue(x => x.IsPlaying);
        PauseCommand = ReactiveCommand.Create(Pause, canPause);

        StopCommand = ReactiveCommand.Create(Stop);

        var canNavigate = this.WhenAnyValue(x => x.ItemCount).Select(x => x > 1);
        NextCommand = ReactiveCommand.Create(Next, canNavigate);
        PreviousCommand = ReactiveCommand.Create(Previous, canNavigate);

        // Items koleksiyonu değişikliklerini izle
        Items.CollectionChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(ItemCount));
            this.RaisePropertyChanged(nameof(IsEmpty));
            UpdateItemOrders();
        };
    }

    #region Public Methods

    /// <summary>
    /// Mesaj ekler
    /// </summary>
    public void AddItem(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var item = new PlaylistItem
        {
            Order = Items.Count + 1,
            Text = text,
            DurationSeconds = DefaultDuration,
            Transition = DefaultTransition
        };

        Items.Add(item);
        SelectedItem = item;
    }

    /// <summary>
    /// Belirtilen indekse mesaj ekler
    /// </summary>
    public void InsertItem(int index, PlaylistItem item)
    {
        if (index < 0) index = 0;
        if (index > Items.Count) index = Items.Count;

        Items.Insert(index, item);
        UpdateItemOrders();
    }

    /// <summary>
    /// Mesajı taşır (sürükle-bırak için)
    /// </summary>
    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Items.Count) return;
        if (toIndex < 0 || toIndex >= Items.Count) return;
        if (fromIndex == toIndex) return;

        var item = Items[fromIndex];
        Items.RemoveAt(fromIndex);
        Items.Insert(toIndex, item);
        UpdateItemOrders();
    }

    /// <summary>
    /// Belirtilen indeksteki mesajı döndürür
    /// </summary>
    public PlaylistItem? GetItem(int index)
    {
        if (index >= 0 && index < Items.Count)
        {
            return Items[index];
        }
        return null;
    }

    #endregion

    #region Private Methods

    private void RemoveSelected()
    {
        if (SelectedItem != null)
        {
            var index = Items.IndexOf(SelectedItem);
            Items.Remove(SelectedItem);

            // Yeni seçim
            if (Items.Count > 0)
            {
                SelectedItem = Items[Math.Min(index, Items.Count - 1)];
            }
            else
            {
                SelectedItem = null;
            }
        }
    }

    private void ClearAll()
    {
        Stop();
        Items.Clear();
        SelectedItem = null;
    }

    private void MoveUp()
    {
        if (SelectedItem == null) return;

        var index = Items.IndexOf(SelectedItem);
        if (index > 0)
        {
            MoveItem(index, index - 1);
        }
    }

    private void MoveDown()
    {
        if (SelectedItem == null) return;

        var index = Items.IndexOf(SelectedItem);
        if (index < Items.Count - 1)
        {
            MoveItem(index, index + 1);
        }
    }

    private void Play()
    {
        if (Items.Count == 0) return;

        IsPlaying = true;
        CurrentIndex = 0;
        PlayCurrentItem();
    }

    private void Pause()
    {
        IsPlaying = false;
        _playlistTimer.Stop();
    }

    private void Stop()
    {
        IsPlaying = false;
        _playlistTimer.Stop();
        CurrentIndex = 0;
        CurrentPlayingItem = null;
    }

    private void Next()
    {
        if (Items.Count == 0) return;

        CurrentIndex++;
        if (CurrentIndex >= Items.Count)
        {
            if (IsLoopEnabled)
            {
                CurrentIndex = 0;
            }
            else
            {
                Stop();
                return;
            }
        }

        if (IsPlaying)
        {
            PlayCurrentItem();
        }
        else
        {
            CurrentPlayingItem = Items[CurrentIndex];
            if (CurrentPlayingItem != null)
            {
                MessageChanged?.Invoke(CurrentPlayingItem);
            }
        }
    }

    private void Previous()
    {
        if (Items.Count == 0) return;

        CurrentIndex--;
        if (CurrentIndex < 0)
        {
            if (IsLoopEnabled)
            {
                CurrentIndex = Items.Count - 1;
            }
            else
            {
                CurrentIndex = 0;
            }
        }

        if (IsPlaying)
        {
            PlayCurrentItem();
        }
        else
        {
            CurrentPlayingItem = Items[CurrentIndex];
            if (CurrentPlayingItem != null)
            {
                MessageChanged?.Invoke(CurrentPlayingItem);
            }
        }
    }

    private void PlayCurrentItem()
    {
        if (CurrentIndex < 0 || CurrentIndex >= Items.Count) return;

        var previousItem = CurrentPlayingItem;
        CurrentPlayingItem = Items[CurrentIndex];

        // Geçiş efekti bildirimi
        if (previousItem != null && CurrentPlayingItem != null)
        {
            TransitionStarted?.Invoke(previousItem, CurrentPlayingItem, CurrentPlayingItem.Transition);
        }

        // Mesaj değişikliği bildirimi
        if (CurrentPlayingItem != null)
        {
            MessageChanged?.Invoke(CurrentPlayingItem);

            // Timer'ı ayarla
            _playlistTimer.Stop();
            _playlistTimer.Interval = CurrentPlayingItem.DurationSeconds * 1000;
            _playlistTimer.Start();
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // UI thread'inde çalıştır
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (IsPlaying)
            {
                Next();
            }
        });
    }

    private void UpdateItemOrders()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].Order = i + 1;
        }
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _playlistTimer.Stop();
            _playlistTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
