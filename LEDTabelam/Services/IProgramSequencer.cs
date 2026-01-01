using System;
using System.Collections.ObjectModel;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Program sıralayıcı interface'i - programları yönetir ve geçişleri kontrol eder
/// Requirements: 7.1, 7.4
/// </summary>
public interface IProgramSequencer
{
    #region State Properties

    /// <summary>
    /// Şu anki program index'i (0-based)
    /// </summary>
    int CurrentProgramIndex { get; }

    /// <summary>
    /// Şu anki aktif program
    /// </summary>
    TabelaProgram? CurrentProgram { get; }

    /// <summary>
    /// Oynatma durumu
    /// Requirements: 7.2, 7.3
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Döngü modu aktif mi
    /// true: Son programdan sonra ilk programa döner
    /// false: Son programda durur
    /// Requirements: 2.6, 7.6
    /// </summary>
    bool IsLooping { get; set; }

    /// <summary>
    /// Mevcut programda geçen süre (saniye)
    /// </summary>
    double ProgramElapsedTime { get; }

    #endregion

    #region Program Collection

    /// <summary>
    /// Program koleksiyonu
    /// </summary>
    ObservableCollection<TabelaProgram> Programs { get; set; }

    #endregion

    #region Control Methods

    /// <summary>
    /// Program döngüsünü başlatır
    /// Requirements: 7.2
    /// </summary>
    void Play();

    /// <summary>
    /// Program döngüsünü duraklatır (mevcut programda kalır)
    /// Requirements: 7.3
    /// </summary>
    void Pause();

    /// <summary>
    /// Program döngüsünü durdurur ve ilk programa döner
    /// </summary>
    void Stop();

    /// <summary>
    /// Sonraki programa geçer
    /// Requirements: 7.4
    /// </summary>
    void NextProgram();

    /// <summary>
    /// Önceki programa geçer
    /// Requirements: 7.4
    /// </summary>
    void PreviousProgram();

    /// <summary>
    /// Belirtilen index'teki programa gider
    /// </summary>
    /// <param name="index">Program index'i (0-based)</param>
    void GoToProgram(int index);

    #endregion

    #region Tick Method

    /// <summary>
    /// Her frame'de çağrılır (AnimationService'den)
    /// Program ve ara durak timer'larını günceller
    /// </summary>
    /// <param name="deltaTime">Son frame'den bu yana geçen süre (saniye)</param>
    void OnTick(double deltaTime);

    #endregion

    #region Events

    /// <summary>
    /// Program değiştiğinde tetiklenir
    /// </summary>
    event Action<TabelaProgram>? ProgramChanged;

    /// <summary>
    /// Ara durak değiştiğinde tetiklenir
    /// </summary>
    event Action<TabelaItem, IntermediateStop>? StopChanged;

    /// <summary>
    /// Ana içeriğe dönüldüğünde tetiklenir (ara durak döngüsünde)
    /// Requirements: 8.1, 8.2
    /// </summary>
    event Action<TabelaItem>? MainContentShowing;

    #endregion
}
