using System;
using System.Collections.Generic;
using System.Timers;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Playlist yönetim servisi implementasyonu
/// Requirements: 15.1, 15.4, 15.5
/// </summary>
public class PlaylistManager : IPlaylistManager, IDisposable
{
    private readonly List<PlaylistItem> _items = new();
    private readonly Timer _playlistTimer;
    private int _currentIndex = -1;
    private bool _isPlaying;
    private bool _isLoopEnabled = true;
    private int _defaultDuration = 3;
    private TransitionType _defaultTransition = TransitionType.Fade;
    private bool _disposed;

    /// <summary>
    /// PlaylistManager constructor
    /// </summary>
    public PlaylistManager()
    {
        _playlistTimer = new Timer();
        _playlistTimer.Elapsed += OnTimerElapsed;
        _playlistTimer.AutoReset = false;
    }

    #region Properties

    /// <inheritdoc/>
    public IReadOnlyList<PlaylistItem> Items => _items.AsReadOnly();

    /// <inheritdoc/>
    public int CurrentIndex => _currentIndex;

    /// <inheritdoc/>
    public PlaylistItem? CurrentItem => 
        _currentIndex >= 0 && _currentIndex < _items.Count ? _items[_currentIndex] : null;

    /// <inheritdoc/>
    public bool IsPlaying => _isPlaying;

    /// <inheritdoc/>
    public bool IsLoopEnabled
    {
        get => _isLoopEnabled;
        set => _isLoopEnabled = value;
    }

    /// <inheritdoc/>
    public int DefaultDuration
    {
        get => _defaultDuration;
        set => _defaultDuration = Math.Max(1, value);
    }

    /// <inheritdoc/>
    public TransitionType DefaultTransition
    {
        get => _defaultTransition;
        set => _defaultTransition = value;
    }

    /// <inheritdoc/>
    public int Count => _items.Count;

    #endregion

    #region Events

    /// <inheritdoc/>
    public event Action<PlaylistItem>? MessageChanged;

    /// <inheritdoc/>
    public event Action<PlaylistItem?, PlaylistItem, TransitionType>? TransitionStarted;

    /// <inheritdoc/>
    public event Action<bool>? PlayingStateChanged;

    #endregion

    #region Item Management

    /// <inheritdoc/>
    public PlaylistItem AddItem(string text)
    {
        return AddItem(text, _defaultDuration, _defaultTransition);
    }

    /// <inheritdoc/>
    public PlaylistItem AddItem(string text, int durationSeconds, TransitionType transition)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty", nameof(text));

        var item = new PlaylistItem
        {
            Order = _items.Count + 1,
            Text = text,
            DurationSeconds = Math.Max(1, durationSeconds),
            Transition = transition
        };

        _items.Add(item);
        return item;
    }

    /// <inheritdoc/>
    public void InsertItem(int index, PlaylistItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        index = Math.Clamp(index, 0, _items.Count);
        _items.Insert(index, item);
        UpdateItemOrders();

        // Adjust current index if needed
        if (_currentIndex >= index)
        {
            _currentIndex++;
        }
    }

    /// <inheritdoc/>
    public bool RemoveItem(int index)
    {
        if (index < 0 || index >= _items.Count)
            return false;

        _items.RemoveAt(index);
        UpdateItemOrders();

        // Adjust current index if needed
        if (_currentIndex >= _items.Count)
        {
            _currentIndex = _items.Count - 1;
        }
        else if (_currentIndex > index)
        {
            _currentIndex--;
        }

        // If we removed the current item while playing, move to next
        if (_isPlaying && _items.Count > 0 && _currentIndex >= 0)
        {
            PlayCurrentItem();
        }
        else if (_items.Count == 0)
        {
            Stop();
        }

        return true;
    }

    /// <inheritdoc/>
    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _items.Count) return;
        if (toIndex < 0 || toIndex >= _items.Count) return;
        if (fromIndex == toIndex) return;

        var item = _items[fromIndex];
        _items.RemoveAt(fromIndex);
        _items.Insert(toIndex, item);
        UpdateItemOrders();

        // Adjust current index
        if (_currentIndex == fromIndex)
        {
            _currentIndex = toIndex;
        }
        else if (fromIndex < _currentIndex && toIndex >= _currentIndex)
        {
            _currentIndex--;
        }
        else if (fromIndex > _currentIndex && toIndex <= _currentIndex)
        {
            _currentIndex++;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        Stop();
        _items.Clear();
        _currentIndex = -1;
    }

    /// <inheritdoc/>
    public PlaylistItem? GetItem(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            return _items[index];
        }
        return null;
    }

    #endregion

    #region Playback Control

    /// <inheritdoc/>
    public void Play()
    {
        if (_items.Count == 0) return;

        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }

        _isPlaying = true;
        PlayingStateChanged?.Invoke(true);
        PlayCurrentItem();
    }

    /// <inheritdoc/>
    public void Pause()
    {
        if (!_isPlaying) return;

        _playlistTimer.Stop();
        _isPlaying = false;
        PlayingStateChanged?.Invoke(false);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _playlistTimer.Stop();
        _isPlaying = false;
        _currentIndex = _items.Count > 0 ? 0 : -1;
        PlayingStateChanged?.Invoke(false);
    }

    /// <inheritdoc/>
    public void Next()
    {
        if (_items.Count == 0) return;

        var previousItem = CurrentItem;
        _currentIndex++;

        if (_currentIndex >= _items.Count)
        {
            if (_isLoopEnabled)
            {
                _currentIndex = 0;
            }
            else
            {
                _currentIndex = _items.Count - 1;
                if (_isPlaying)
                {
                    Stop();
                }
                return;
            }
        }

        if (_isPlaying)
        {
            PlayCurrentItem(previousItem);
        }
        else
        {
            NotifyMessageChanged(previousItem);
        }
    }

    /// <inheritdoc/>
    public void Previous()
    {
        if (_items.Count == 0) return;

        var previousItem = CurrentItem;
        _currentIndex--;

        if (_currentIndex < 0)
        {
            if (_isLoopEnabled)
            {
                _currentIndex = _items.Count - 1;
            }
            else
            {
                _currentIndex = 0;
                return;
            }
        }

        if (_isPlaying)
        {
            PlayCurrentItem(previousItem);
        }
        else
        {
            NotifyMessageChanged(previousItem);
        }
    }

    /// <inheritdoc/>
    public void GoTo(int index)
    {
        if (_items.Count == 0) return;
        if (index < 0 || index >= _items.Count) return;

        var previousItem = CurrentItem;
        _currentIndex = index;

        if (_isPlaying)
        {
            PlayCurrentItem(previousItem);
        }
        else
        {
            NotifyMessageChanged(previousItem);
        }
    }

    #endregion

    #region Private Methods

    private void PlayCurrentItem(PlaylistItem? previousItem = null)
    {
        if (_currentIndex < 0 || _currentIndex >= _items.Count) return;

        var currentItem = _items[_currentIndex];

        // Notify transition
        TransitionStarted?.Invoke(previousItem, currentItem, currentItem.Transition);

        // Notify message change
        MessageChanged?.Invoke(currentItem);

        // Set timer for next item
        _playlistTimer.Stop();
        _playlistTimer.Interval = currentItem.DurationSeconds * 1000;
        _playlistTimer.Start();
    }

    private void NotifyMessageChanged(PlaylistItem? previousItem)
    {
        var currentItem = CurrentItem;
        if (currentItem != null)
        {
            TransitionStarted?.Invoke(previousItem, currentItem, currentItem.Transition);
            MessageChanged?.Invoke(currentItem);
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isPlaying) return;

        // Move to next item
        Next();
    }

    private void UpdateItemOrders()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].Order = i + 1;
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _playlistTimer.Stop();
        _playlistTimer.Elapsed -= OnTimerElapsed;
        _playlistTimer.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}
