using System;
using System.Collections.Generic;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Playlist yönetim servisi interface'i
/// Requirements: 15.1, 15.4, 15.5
/// </summary>
public interface IPlaylistManager
{
    /// <summary>
    /// Playlist öğeleri
    /// </summary>
    IReadOnlyList<PlaylistItem> Items { get; }

    /// <summary>
    /// Mevcut oynatma indeksi
    /// </summary>
    int CurrentIndex { get; }

    /// <summary>
    /// Şu an oynatılan öğe
    /// </summary>
    PlaylistItem? CurrentItem { get; }

    /// <summary>
    /// Playlist oynatılıyor mu
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Döngü modu aktif mi
    /// </summary>
    bool IsLoopEnabled { get; set; }

    /// <summary>
    /// Varsayılan gösterim süresi (saniye)
    /// </summary>
    int DefaultDuration { get; set; }

    /// <summary>
    /// Varsayılan geçiş efekti
    /// </summary>
    TransitionType DefaultTransition { get; set; }

    /// <summary>
    /// Playlist öğe sayısı
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Mesaj ekler
    /// </summary>
    /// <param name="text">Mesaj metni</param>
    /// <returns>Eklenen öğe</returns>
    PlaylistItem AddItem(string text);

    /// <summary>
    /// Mesaj ekler (tam kontrol)
    /// </summary>
    /// <param name="text">Mesaj metni</param>
    /// <param name="durationSeconds">Gösterim süresi</param>
    /// <param name="transition">Geçiş efekti</param>
    /// <returns>Eklenen öğe</returns>
    PlaylistItem AddItem(string text, int durationSeconds, TransitionType transition);

    /// <summary>
    /// Belirtilen indekse mesaj ekler
    /// </summary>
    /// <param name="index">Eklenecek indeks</param>
    /// <param name="item">Playlist öğesi</param>
    void InsertItem(int index, PlaylistItem item);

    /// <summary>
    /// Mesajı kaldırır
    /// </summary>
    /// <param name="index">Kaldırılacak indeks</param>
    /// <returns>Başarılı ise true</returns>
    bool RemoveItem(int index);

    /// <summary>
    /// Mesajı taşır (sürükle-bırak için)
    /// </summary>
    /// <param name="fromIndex">Kaynak indeks</param>
    /// <param name="toIndex">Hedef indeks</param>
    void MoveItem(int fromIndex, int toIndex);

    /// <summary>
    /// Tüm mesajları temizler
    /// </summary>
    void Clear();

    /// <summary>
    /// Belirtilen indeksteki mesajı döndürür
    /// </summary>
    /// <param name="index">İndeks</param>
    /// <returns>Playlist öğesi veya null</returns>
    PlaylistItem? GetItem(int index);

    /// <summary>
    /// Playlist oynatmayı başlatır
    /// </summary>
    void Play();

    /// <summary>
    /// Playlist oynatmayı duraklatır
    /// </summary>
    void Pause();

    /// <summary>
    /// Playlist oynatmayı durdurur ve başa döner
    /// </summary>
    void Stop();

    /// <summary>
    /// Sonraki mesaja geçer
    /// </summary>
    void Next();

    /// <summary>
    /// Önceki mesaja geçer
    /// </summary>
    void Previous();

    /// <summary>
    /// Belirtilen indekse atlar
    /// </summary>
    /// <param name="index">Hedef indeks</param>
    void GoTo(int index);

    /// <summary>
    /// Mesaj değiştiğinde tetiklenir
    /// </summary>
    event Action<PlaylistItem>? MessageChanged;

    /// <summary>
    /// Geçiş başladığında tetiklenir
    /// </summary>
    event Action<PlaylistItem?, PlaylistItem, TransitionType>? TransitionStarted;

    /// <summary>
    /// Oynatma durumu değiştiğinde tetiklenir
    /// </summary>
    event Action<bool>? PlayingStateChanged;
}
