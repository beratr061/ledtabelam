using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LEDTabelam.Models;

namespace LEDTabelam.Services;

/// <summary>
/// Program sıralayıcı implementasyonu - programları yönetir ve geçişleri kontrol eder
/// Requirements: 2.4, 2.6, 3.3, 5.4, 5.5, 6.3, 7.2, 7.3, 7.6, 8.3
/// </summary>
public class ProgramSequencer : IProgramSequencer
{
    #region Private Fields

    private ObservableCollection<TabelaProgram> _programs = new();
    private int _currentProgramIndex = 0;
    private bool _isPlaying = false;
    private bool _isLooping = true;
    private double _programElapsedTime = 0;

    // Her TabelaItem için ara durak timer state'i
    private readonly Dictionary<int, StopTimerState> _stopTimers = new();

    // Geçiş animasyonu state'i
    // Requirements: 3.3, 6.3
    private bool _isInProgramTransition = false;
    private double _programTransitionElapsedTime = 0;
    private TabelaProgram? _transitionFromProgram;
    private TabelaProgram? _transitionToProgram;

    // Ara durak geçiş animasyonu state'i (her item için)
    private readonly Dictionary<int, StopTransitionState> _stopTransitions = new();

    #endregion

    #region State Properties

    /// <inheritdoc/>
    public int CurrentProgramIndex => _currentProgramIndex;

    /// <inheritdoc/>
    public TabelaProgram? CurrentProgram => 
        _programs.Count > 0 && _currentProgramIndex >= 0 && _currentProgramIndex < _programs.Count 
            ? _programs[_currentProgramIndex] 
            : null;

    /// <inheritdoc/>
    public bool IsPlaying => _isPlaying;

    /// <inheritdoc/>
    public bool IsLooping
    {
        get => _isLooping;
        set => _isLooping = value;
    }

    /// <inheritdoc/>
    public double ProgramElapsedTime => _programElapsedTime;

    /// <summary>
    /// Program geçiş animasyonu devam ediyor mu
    /// Requirements: 3.3
    /// </summary>
    public bool IsInProgramTransition => _isInProgramTransition;

    /// <summary>
    /// Program geçiş animasyonu ilerleme oranı (0.0 - 1.0)
    /// Requirements: 3.3
    /// </summary>
    public double ProgramTransitionProgress
    {
        get
        {
            if (!_isInProgramTransition || _transitionToProgram == null)
                return 0;
            
            var transitionDuration = _transitionToProgram.TransitionDurationMs / 1000.0;
            if (transitionDuration <= 0)
                return 1;
            
            return Math.Clamp(_programTransitionElapsedTime / transitionDuration, 0, 1);
        }
    }

    /// <summary>
    /// Geçiş yapılan kaynak program
    /// </summary>
    public TabelaProgram? TransitionFromProgram => _transitionFromProgram;

    /// <summary>
    /// Geçiş yapılan hedef program
    /// </summary>
    public TabelaProgram? TransitionToProgram => _transitionToProgram;

    #endregion

    #region Program Collection

    /// <inheritdoc/>
    public ObservableCollection<TabelaProgram> Programs
    {
        get => _programs;
        set
        {
            _programs = value ?? new ObservableCollection<TabelaProgram>();
            _currentProgramIndex = 0;
            _programElapsedTime = 0;
            ResetAllStopTimers();
            UpdateCurrentProgramActive();
        }
    }

    #endregion

    #region Control Methods

    /// <inheritdoc/>
    public void Play()
    {
        if (_programs.Count == 0) return;
        
        _isPlaying = true;
        
        // Ara durak timer'larını başlat
        InitializeStopTimersForCurrentProgram();
        
        UpdateCurrentProgramActive();
    }

    /// <inheritdoc/>
    public void Pause()
    {
        _isPlaying = false;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _isPlaying = false;
        _currentProgramIndex = 0;
        _programElapsedTime = 0;
        ResetAllStopTimers();
        UpdateCurrentProgramActive();
    }

    /// <inheritdoc/>
    public void NextProgram()
    {
        if (_programs.Count == 0) return;

        int nextIndex = _currentProgramIndex + 1;

        if (nextIndex >= _programs.Count)
        {
            if (_isLooping)
            {
                // Döngü modu: İlk programa dön
                // Requirements: 2.6
                nextIndex = 0;
            }
            else
            {
                // Non-loop modu: Son programda kal ve dur
                // Requirements: 7.6
                _isPlaying = false;
                return;
            }
        }

        GoToProgram(nextIndex);
    }

    /// <inheritdoc/>
    public void PreviousProgram()
    {
        if (_programs.Count == 0) return;

        int prevIndex = _currentProgramIndex - 1;

        if (prevIndex < 0)
        {
            if (_isLooping)
            {
                // Döngü modu: Son programa git
                prevIndex = _programs.Count - 1;
            }
            else
            {
                // Non-loop modu: İlk programda kal
                prevIndex = 0;
            }
        }

        GoToProgram(prevIndex);
    }

    /// <inheritdoc/>
    public void GoToProgram(int index)
    {
        if (_programs.Count == 0) return;

        // Index'i geçerli aralığa clamp et
        index = Math.Clamp(index, 0, _programs.Count - 1);

        // Farklı bir programa geçiş mi yoksa aynı programa döngü mü?
        bool isSameProgram = (index == _currentProgramIndex);

        if (!isSameProgram)
        {
            var previousProgram = CurrentProgram;
            var nextProgram = _programs[index];
            
            if (previousProgram != null)
            {
                previousProgram.IsActive = false;
            }

            // Geçiş animasyonu başlat (Direct değilse)
            // Requirements: 3.3
            if (nextProgram.Transition != ProgramTransitionType.Direct && nextProgram.TransitionDurationMs > 0)
            {
                _isInProgramTransition = true;
                _programTransitionElapsedTime = 0;
                _transitionFromProgram = previousProgram;
                _transitionToProgram = nextProgram;
                
                // Geçiş başladı event'i
                ProgramTransitionStarted?.Invoke(previousProgram!, nextProgram, nextProgram.Transition);
            }

            _currentProgramIndex = index;
            _programElapsedTime = 0;
            
            // Yeni programın ara durak timer'larını başlat (farklı programa geçişte)
            // Requirements: 8.3 - Eşzamanlı döngü bağımsızlığı
            InitializeStopTimersForCurrentProgram();
            
            UpdateCurrentProgramActive();
            
            if (CurrentProgram != null)
            {
                ProgramChanged?.Invoke(CurrentProgram);
            }
        }
        else
        {
            // Aynı programa döngü (tek program durumu)
            // Program elapsed time'ı sıfırla
            _programElapsedTime = 0;
            
            // Ara durak timer'larını da sıfırla - döngü her zaman ana içerikten başlasın
            foreach (var kvp in _stopTimers)
            {
                kvp.Value.CurrentStopIndex = 0;
                kvp.Value.ElapsedTime = 0;
                kvp.Value.JustReturnedToMain = true; // Ana içeriği korumak için flag set et
            }
            
            // Ana içeriğe dönüldüğünü bildir
            foreach (var item in CurrentProgram.Items)
            {
                if (item.HasIntermediateStops)
                {
                    MainContentShowing?.Invoke(item);
                }
            }
        }
    }

    #endregion

    #region Tick Method

    /// <inheritdoc/>
    public void OnTick(double deltaTime)
    {
        if (!_isPlaying || _programs.Count == 0 || CurrentProgram == null)
            return;

        // Program geçiş animasyonunu güncelle
        // Requirements: 3.3
        if (_isInProgramTransition)
        {
            UpdateProgramTransition(deltaTime);
        }

        // Program timer'ını güncelle (geçiş sırasında da devam eder)
        _programElapsedTime += deltaTime;

        // Program süresi doldu mu kontrol et
        // Requirements: 2.4
        // Eğer ara durak varsa, program süresi ara durak döngüsüne göre hesaplanır
        double effectiveProgramDuration = GetEffectiveProgramDuration(CurrentProgram);
        if (_programElapsedTime >= effectiveProgramDuration)
        {
            NextProgram();
            return;
        }

        // Ara durak timer'larını güncelle
        // Requirements: 8.3 - Program ve ara durak timer'ları bağımsız çalışır
        UpdateStopTimers(deltaTime);
        
        // Ara durak geçiş animasyonlarını güncelle
        // Requirements: 6.3
        UpdateStopTransitions(deltaTime);
    }

    /// <summary>
    /// Program geçiş animasyonunu günceller
    /// Requirements: 3.3
    /// </summary>
    private void UpdateProgramTransition(double deltaTime)
    {
        if (!_isInProgramTransition || _transitionToProgram == null)
            return;

        _programTransitionElapsedTime += deltaTime;
        
        var transitionDuration = _transitionToProgram.TransitionDurationMs / 1000.0;
        var progress = Math.Clamp(_programTransitionElapsedTime / transitionDuration, 0, 1);
        
        // İlerleme event'i
        ProgramTransitionProgress_Event?.Invoke(progress);

        // Geçiş tamamlandı mı?
        if (_programTransitionElapsedTime >= transitionDuration)
        {
            _isInProgramTransition = false;
            _programTransitionElapsedTime = 0;
            
            // Tamamlandı event'i
            ProgramTransitionCompleted?.Invoke(_transitionToProgram);
            
            _transitionFromProgram = null;
            _transitionToProgram = null;
        }
    }

    /// <summary>
    /// Ara durak geçiş animasyonlarını günceller
    /// Requirements: 6.3
    /// </summary>
    private void UpdateStopTransitions(double deltaTime)
    {
        if (CurrentProgram == null) return;

        var completedTransitions = new List<int>();

        foreach (var kvp in _stopTransitions)
        {
            var itemId = kvp.Key;
            var transitionState = kvp.Value;

            transitionState.ElapsedTime += deltaTime;
            
            var transitionDuration = transitionState.AnimationDurationMs / 1000.0;
            var progress = Math.Clamp(transitionState.ElapsedTime / transitionDuration, 0, 1);
            
            // İlerleme event'i
            StopTransitionProgress?.Invoke(transitionState.Item, progress);

            // Geçiş tamamlandı mı?
            if (transitionState.ElapsedTime >= transitionDuration)
            {
                completedTransitions.Add(itemId);
                
                // Tamamlandı event'i
                StopTransitionCompleted?.Invoke(transitionState.Item, transitionState.ToStop);
            }
        }

        // Tamamlanan geçişleri temizle
        foreach (var itemId in completedTransitions)
        {
            _stopTransitions.Remove(itemId);
        }
    }

    #endregion

    #region Events

    /// <inheritdoc/>
    public event Action<TabelaProgram>? ProgramChanged;

    /// <inheritdoc/>
    public event Action<TabelaItem, IntermediateStop>? StopChanged;

    /// <inheritdoc/>
    public event Action<TabelaItem>? MainContentShowing;

    /// <summary>
    /// Program geçiş animasyonu başladığında tetiklenir
    /// Requirements: 3.3
    /// </summary>
    public event Action<TabelaProgram, TabelaProgram, ProgramTransitionType>? ProgramTransitionStarted;

    /// <summary>
    /// Program geçiş animasyonu ilerlediğinde tetiklenir (her tick'te)
    /// Requirements: 3.3
    /// </summary>
    public event Action<double>? ProgramTransitionProgress_Event;

    /// <summary>
    /// Program geçiş animasyonu tamamlandığında tetiklenir
    /// Requirements: 3.3
    /// </summary>
    public event Action<TabelaProgram>? ProgramTransitionCompleted;

    /// <summary>
    /// Ara durak geçiş animasyonu başladığında tetiklenir
    /// Requirements: 6.3
    /// </summary>
    public event Action<TabelaItem, IntermediateStop, IntermediateStop, StopAnimationType>? StopTransitionStarted;

    /// <summary>
    /// Ara durak geçiş animasyonu ilerlediğinde tetiklenir
    /// Requirements: 6.3
    /// </summary>
    public event Action<TabelaItem, double>? StopTransitionProgress;

    /// <summary>
    /// Ara durak geçiş animasyonu tamamlandığında tetiklenir
    /// Requirements: 6.3
    /// </summary>
    public event Action<TabelaItem, IntermediateStop>? StopTransitionCompleted;

    #endregion

    #region Private Methods

    /// <summary>
    /// Mevcut programın aktif durumunu günceller
    /// </summary>
    private void UpdateCurrentProgramActive()
    {
        foreach (var program in _programs)
        {
            program.IsActive = false;
        }

        if (CurrentProgram != null)
        {
            CurrentProgram.IsActive = true;
        }
    }

    /// <summary>
    /// Mevcut program için ara durak timer'larını başlatır
    /// </summary>
    private void InitializeStopTimersForCurrentProgram()
    {
        _stopTimers.Clear();

        if (CurrentProgram == null) return;

        foreach (var item in CurrentProgram.Items)
        {
            if (item.HasIntermediateStops)
            {
                var stopSettings = item.IntermediateStops;
                double duration = GetStopDuration(item, CurrentProgram.DurationSeconds);

                _stopTimers[item.Id] = new StopTimerState
                {
                    CurrentStopIndex = 0,
                    ElapsedTime = 0,
                    Duration = duration,
                    JustReturnedToMain = false // İlk başlangıçta false, zaten ana içerikten başlıyoruz
                };
            }
        }
    }

    /// <summary>
    /// Tüm ara durak timer'larını sıfırlar
    /// </summary>
    private void ResetAllStopTimers()
    {
        _stopTimers.Clear();
        InitializeStopTimersForCurrentProgram();
    }

    /// <summary>
    /// Ara durak timer'larını günceller
    /// Requirements: 5.4, 5.5, 8.3
    /// </summary>
    private void UpdateStopTimers(double deltaTime)
    {
        if (CurrentProgram == null) return;

        foreach (var item in CurrentProgram.Items)
        {
            if (!item.HasIntermediateStops) continue;

            if (!_stopTimers.TryGetValue(item.Id, out var timerState))
            {
                // Timer yoksa oluştur
                double duration = GetStopDuration(item, CurrentProgram.DurationSeconds);
                timerState = new StopTimerState
                {
                    CurrentStopIndex = 0,
                    ElapsedTime = 0,
                    Duration = duration,
                    JustReturnedToMain = false
                };
                _stopTimers[item.Id] = timerState;
            }

            // Ana içeriğe yeni dönüldüyse, süre dolana kadar geçiş yapma
            if (timerState.JustReturnedToMain)
            {
                timerState.ElapsedTime += deltaTime;
                
                // Süre doldu mu kontrol et
                if (timerState.ElapsedTime >= timerState.Duration)
                {
                    // Ana içerik süresi doldu, şimdi ilk durağa geç
                    timerState.JustReturnedToMain = false;
                    timerState.CurrentStopIndex = 1; // İlk durağa geç
                    timerState.ElapsedTime = 0;
                    
                    var stops = item.IntermediateStops.Stops;
                    
                    // Event tetikle
                    if (stops.Count > 0)
                    {
                        StopChanged?.Invoke(item, stops[0]);
                    }
                }
                continue; // Bu tick'te başka işlem yapma
            }

            timerState.ElapsedTime += deltaTime;

            // Durak süresi doldu mu kontrol et - sadece bir kez geçiş yap
            if (timerState.ElapsedTime >= timerState.Duration)
            {
                var stops = item.IntermediateStops.Stops;
                var currentIndex = timerState.CurrentStopIndex;
                
                // Toplam adım sayısı = 1 (ana içerik) + durak sayısı
                int totalSteps = stops.Count + 1;
                int nextIndex = (currentIndex + 1) % totalSteps;

                timerState.CurrentStopIndex = nextIndex;
                
                // Süreyi yeniden hesapla (AutoCalculateDuration için)
                double newDuration = GetStopDuration(item, CurrentProgram.DurationSeconds);
                timerState.Duration = newDuration;
                
                // ElapsedTime'ı sıfırla
                timerState.ElapsedTime = 0;

                // Ana içeriğe dönüldüyse flag'i set et ve event tetikle
                if (nextIndex == 0)
                {
                    timerState.JustReturnedToMain = true;
                    // Ana içeriğe dönüldüğünü bildir
                    MainContentShowing?.Invoke(item);
                }

                // Event tetikle - durak değişti
                if (nextIndex > 0 && nextIndex <= stops.Count)
                {
                    StopChanged?.Invoke(item, stops[nextIndex - 1]);
                }
            }
        }
    }

    /// <summary>
    /// Bir item için durak süresini hesaplar
    /// Requirements: 8.4
    /// </summary>
    private double GetStopDuration(TabelaItem item, double programDuration)
    {
        var stopSettings = item.IntermediateStops;

        if (stopSettings.AutoCalculateDuration && stopSettings.Stops.Count > 0)
        {
            // Otomatik hesaplama: program_süresi / toplam_adım_sayısı
            // Toplam adım = 1 (ana içerik) + durak sayısı
            int totalSteps = stopSettings.Stops.Count + 1;
            return programDuration / totalSteps;
        }

        return stopSettings.DurationSeconds;
    }

    /// <summary>
    /// Program için efektif süreyi hesaplar
    /// Eğer ara durak varsa: toplam_adım × durak_süresi
    /// Yoksa: program'ın kendi DurationSeconds değeri
    /// </summary>
    private double GetEffectiveProgramDuration(TabelaProgram program)
    {
        // Ara durağı olan item'ları bul
        double maxDuration = program.DurationSeconds;

        foreach (var item in program.Items)
        {
            if (item.HasIntermediateStops)
            {
                var stopSettings = item.IntermediateStops;
                // Toplam adım = 1 (ana içerik) + durak sayısı
                int totalSteps = stopSettings.Stops.Count + 1;
                // Her adım için süre
                double stepDuration = stopSettings.DurationSeconds;
                // Toplam süre = adım sayısı × adım süresi
                double totalDuration = totalSteps * stepDuration;
                
                // En uzun süreyi al (birden fazla item varsa)
                if (totalDuration > maxDuration)
                {
                    maxDuration = totalDuration;
                }
            }
        }

        return maxDuration;
    }

    /// <summary>
    /// Belirtilen item için mevcut durak index'ini döndürür
    /// </summary>
    public int GetCurrentStopIndex(int itemId)
    {
        return _stopTimers.TryGetValue(itemId, out var state) ? state.CurrentStopIndex : 0;
    }

    /// <summary>
    /// Belirtilen item için mevcut durağı döndürür
    /// </summary>
    public IntermediateStop? GetCurrentStop(TabelaItem item)
    {
        if (!item.HasIntermediateStops) return null;

        int index = GetCurrentStopIndex(item.Id);
        var stops = item.IntermediateStops.Stops;

        return index >= 0 && index < stops.Count ? stops[index] : null;
    }

    /// <summary>
    /// Belirtilen item için ara durak geçiş animasyonu devam ediyor mu
    /// Requirements: 6.3
    /// </summary>
    public bool IsInStopTransition(int itemId)
    {
        return _stopTransitions.ContainsKey(itemId);
    }

    /// <summary>
    /// Belirtilen item için ara durak geçiş ilerleme oranını döndürür (0.0 - 1.0)
    /// Requirements: 6.3
    /// </summary>
    public double GetStopTransitionProgress(int itemId)
    {
        if (!_stopTransitions.TryGetValue(itemId, out var state))
            return 0;

        var transitionDuration = state.AnimationDurationMs / 1000.0;
        if (transitionDuration <= 0)
            return 1;

        return Math.Clamp(state.ElapsedTime / transitionDuration, 0, 1);
    }

    /// <summary>
    /// Belirtilen item için ara durak geçiş animasyon tipini döndürür
    /// Requirements: 6.3
    /// </summary>
    public StopAnimationType? GetStopTransitionAnimation(int itemId)
    {
        if (!_stopTransitions.TryGetValue(itemId, out var state))
            return null;

        return state.Animation;
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Ara durak timer durumu
    /// </summary>
    private class StopTimerState
    {
        public int CurrentStopIndex { get; set; }
        public double ElapsedTime { get; set; }
        public double Duration { get; set; }
        /// <summary>
        /// Ana içeriğe yeni dönüldüğünde true olur.
        /// ElapsedTime Duration'a ulaşana kadar true kalır.
        /// Bu sayede ana içerik tam Duration süre gösterilir.
        /// </summary>
        public bool JustReturnedToMain { get; set; }
    }

    /// <summary>
    /// Ara durak geçiş animasyonu durumu
    /// Requirements: 6.3
    /// </summary>
    private class StopTransitionState
    {
        public TabelaItem Item { get; set; } = null!;
        public IntermediateStop FromStop { get; set; } = null!;
        public IntermediateStop ToStop { get; set; } = null!;
        public StopAnimationType Animation { get; set; }
        public int AnimationDurationMs { get; set; }
        public double ElapsedTime { get; set; }
    }

    #endregion
}
