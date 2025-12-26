using System;
using System.Reactive;
using Avalonia.Media;
using ReactiveUI;
using LEDTabelam.Models;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Basitleştirilmiş tabela düzenleyici ViewModel'i
/// Hat Kodu (sol) + Güzergah (sağ, 1 veya 2 satır) mantığı
/// </summary>
public class SimpleTabelaViewModel : ViewModelBase
{
    private int _slotNumber = 1;
    private string _hatKodu = string.Empty;
    private string _guzergahSatir1 = string.Empty;
    private string _guzergahSatir2 = string.Empty;
    private bool _ikiSatirGuzergah = false;
    private int _hatKoduGenislik = 15;
    private Color _hatKoduRenk = Color.FromRgb(255, 176, 0); // Amber
    private Color _guzergahRenk = Color.FromRgb(0, 255, 0); // Yeşil
    private HorizontalAlignment _hAlign = HorizontalAlignment.Center;
    private VerticalAlignment _vAlign = VerticalAlignment.Center;
    
    // Sembol ve Kayan özellikleri
    private bool _hatKoduSembol = false;
    private bool _hatKoduKayan = false;
    private bool _guzergah1Sembol = false;
    private bool _guzergah1Kayan = false;
    private bool _guzergah2Sembol = false;
    private bool _guzergah2Kayan = false;

    /// <summary>
    /// Slot numarası (1-999)
    /// </summary>
    public int SlotNumber
    {
        get => _slotNumber;
        set
        {
            var validValue = Math.Clamp(value, 1, 999);
            this.RaiseAndSetIfChanged(ref _slotNumber, validValue);
        }
    }

    /// <summary>
    /// Hat kodu (örn: "MK", "34", "19K", "10B")
    /// </summary>
    public string HatKodu
    {
        get => _hatKodu;
        set
        {
            this.RaiseAndSetIfChanged(ref _hatKodu, value);
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// Güzergah 1. satır
    /// </summary>
    public string GuzergahSatir1
    {
        get => _guzergahSatir1;
        set
        {
            this.RaiseAndSetIfChanged(ref _guzergahSatir1, value);
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// Güzergah 2. satır (opsiyonel)
    /// </summary>
    public string GuzergahSatir2
    {
        get => _guzergahSatir2;
        set
        {
            this.RaiseAndSetIfChanged(ref _guzergahSatir2, value);
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// 2 satırlı güzergah modu aktif mi
    /// </summary>
    public bool IkiSatirGuzergah
    {
        get => _ikiSatirGuzergah;
        set
        {
            this.RaiseAndSetIfChanged(ref _ikiSatirGuzergah, value);
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// Hat kodu zone genişliği (%)
    /// </summary>
    public int HatKoduGenislik
    {
        get => _hatKoduGenislik;
        set
        {
            var validValue = Math.Clamp(value, 5, 50);
            this.RaiseAndSetIfChanged(ref _hatKoduGenislik, validValue);
            this.RaisePropertyChanged(nameof(GuzergahGenislik));
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// Güzergah zone genişliği (%) - otomatik hesaplanır
    /// </summary>
    public int GuzergahGenislik => 100 - HatKoduGenislik;

    /// <summary>
    /// Hat kodu rengi
    /// </summary>
    public Color HatKoduRenk
    {
        get => _hatKoduRenk;
        set
        {
            this.RaiseAndSetIfChanged(ref _hatKoduRenk, value);
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// Güzergah rengi
    /// </summary>
    public Color GuzergahRenk
    {
        get => _guzergahRenk;
        set
        {
            this.RaiseAndSetIfChanged(ref _guzergahRenk, value);
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// Yatay hizalama
    /// </summary>
    public HorizontalAlignment HAlign
    {
        get => _hAlign;
        set
        {
            this.RaiseAndSetIfChanged(ref _hAlign, value);
            OnTabelaChanged();
        }
    }

    /// <summary>
    /// Dikey hizalama
    /// </summary>
    public VerticalAlignment VAlign
    {
        get => _vAlign;
        set
        {
            this.RaiseAndSetIfChanged(ref _vAlign, value);
            OnTabelaChanged();
        }
    }

    // Sembol özellikleri
    public bool HatKoduSembol
    {
        get => _hatKoduSembol;
        set => this.RaiseAndSetIfChanged(ref _hatKoduSembol, value);
    }

    public bool HatKoduKayan
    {
        get => _hatKoduKayan;
        set => this.RaiseAndSetIfChanged(ref _hatKoduKayan, value);
    }

    public bool Guzergah1Sembol
    {
        get => _guzergah1Sembol;
        set => this.RaiseAndSetIfChanged(ref _guzergah1Sembol, value);
    }

    public bool Guzergah1Kayan
    {
        get => _guzergah1Kayan;
        set
        {
            this.RaiseAndSetIfChanged(ref _guzergah1Kayan, value);
            OnTabelaChanged();
        }
    }

    public bool Guzergah2Sembol
    {
        get => _guzergah2Sembol;
        set => this.RaiseAndSetIfChanged(ref _guzergah2Sembol, value);
    }

    public bool Guzergah2Kayan
    {
        get => _guzergah2Kayan;
        set
        {
            this.RaiseAndSetIfChanged(ref _guzergah2Kayan, value);
            OnTabelaChanged();
        }
    }

    #region Commands

    public ReactiveCommand<Unit, Unit> AddNewCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }

    #endregion

    /// <summary>
    /// Tabela değişiklik eventi
    /// </summary>
    public event Action? TabelaChanged;

    public SimpleTabelaViewModel()
    {
        AddNewCommand = ReactiveCommand.Create(AddNew);
        DeleteCommand = ReactiveCommand.Create(Delete);
        SaveCommand = ReactiveCommand.Create(Save);
    }

    private void AddNew()
    {
        Clear();
        SlotNumber++;
    }

    private void Delete()
    {
        Clear();
    }

    private void Save()
    {
        // Kaydetme işlemi - şimdilik sadece event tetikle
        OnTabelaChanged();
    }

    /// <summary>
    /// Mevcut zone yapılandırmasını döndürür
    /// </summary>
    public System.Collections.Generic.List<Zone> GetZones()
    {
        var zones = new System.Collections.Generic.List<Zone>();

        // Hat Kodu Zone'u (sol)
        if (!string.IsNullOrEmpty(HatKodu))
        {
            zones.Add(new Zone
            {
                Index = 0,
                WidthPercent = HatKoduGenislik,
                ContentType = HatKoduKayan ? ZoneContentType.ScrollingText : ZoneContentType.Text,
                Content = HatKodu,
                HAlign = HAlign,
                VAlign = VAlign,
                TextColor = HatKoduRenk,
                IsScrolling = HatKoduKayan
            });
        }

        // Güzergah Zone'u (sağ)
        string guzergahContent;
        if (IkiSatirGuzergah && !string.IsNullOrEmpty(GuzergahSatir2))
        {
            guzergahContent = $"{GuzergahSatir1}\n{GuzergahSatir2}";
        }
        else
        {
            guzergahContent = GuzergahSatir1;
        }

        bool isScrolling = Guzergah1Kayan || Guzergah2Kayan;

        if (!string.IsNullOrEmpty(guzergahContent))
        {
            zones.Add(new Zone
            {
                Index = zones.Count,
                WidthPercent = string.IsNullOrEmpty(HatKodu) ? 100 : GuzergahGenislik,
                ContentType = isScrolling ? ZoneContentType.ScrollingText : ZoneContentType.Text,
                Content = guzergahContent,
                HAlign = HAlign,
                VAlign = VAlign,
                TextColor = GuzergahRenk,
                IsScrolling = isScrolling
            });
        }

        // Genişlikleri normalize et
        if (zones.Count > 0)
        {
            var totalWidth = 0.0;
            foreach (var zone in zones)
            {
                totalWidth += zone.WidthPercent;
            }
            
            if (Math.Abs(totalWidth - 100) > 0.01)
            {
                var scale = 100.0 / totalWidth;
                foreach (var zone in zones)
                {
                    zone.WidthPercent *= scale;
                }
            }
        }

        return zones;
    }

    /// <summary>
    /// Tabela verilerini temizler
    /// </summary>
    public void Clear()
    {
        HatKodu = string.Empty;
        GuzergahSatir1 = string.Empty;
        GuzergahSatir2 = string.Empty;
        IkiSatirGuzergah = false;
        HatKoduSembol = false;
        HatKoduKayan = false;
        Guzergah1Sembol = false;
        Guzergah1Kayan = false;
        Guzergah2Sembol = false;
        Guzergah2Kayan = false;
    }

    /// <summary>
    /// Slot'tan verileri yükler
    /// </summary>
    public void LoadFromSlot(TabelaSlot slot)
    {
        if (slot == null) return;

        SlotNumber = slot.SlotNumber;
        HatKodu = slot.RouteNumber;
        
        // Güzergah metnini satırlara ayır
        if (!string.IsNullOrEmpty(slot.RouteText))
        {
            var lines = slot.RouteText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            GuzergahSatir1 = lines.Length > 0 ? lines[0] : string.Empty;
            GuzergahSatir2 = lines.Length > 1 ? lines[1] : string.Empty;
            IkiSatirGuzergah = lines.Length > 1 && !string.IsNullOrEmpty(GuzergahSatir2);
        }
        else
        {
            GuzergahSatir1 = string.Empty;
            GuzergahSatir2 = string.Empty;
            IkiSatirGuzergah = false;
        }

        // Zone'lardan renkleri al
        if (slot.Zones != null && slot.Zones.Count > 0)
        {
            var hatKoduZone = slot.Zones.Find(z => z.Index == 0);
            if (hatKoduZone != null)
            {
                HatKoduRenk = hatKoduZone.TextColor;
                HatKoduGenislik = (int)hatKoduZone.WidthPercent;
                HatKoduKayan = hatKoduZone.IsScrolling;
            }

            var guzergahZone = slot.Zones.Find(z => z.Index == 1);
            if (guzergahZone != null)
            {
                GuzergahRenk = guzergahZone.TextColor;
                Guzergah1Kayan = guzergahZone.IsScrolling;
            }
        }

        // Hizalama
        HAlign = slot.HAlign;
        VAlign = slot.VAlign;
    }

    /// <summary>
    /// Slot'a verileri kaydeder
    /// </summary>
    public void SaveToSlot(TabelaSlot slot)
    {
        if (slot == null) return;

        slot.SlotNumber = SlotNumber;
        slot.RouteNumber = HatKodu;
        
        // Güzergah metnini birleştir
        if (IkiSatirGuzergah && !string.IsNullOrEmpty(GuzergahSatir2))
        {
            slot.RouteText = $"{GuzergahSatir1}\n{GuzergahSatir2}";
        }
        else
        {
            slot.RouteText = GuzergahSatir1;
        }

        slot.HAlign = HAlign;
        slot.VAlign = VAlign;
        slot.Zones = GetZones();
    }

    private void OnTabelaChanged()
    {
        TabelaChanged?.Invoke();
    }
}
