using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela üzerindeki tek bir öğe (metin, sembol vb.)
/// HD2020 tarzı - her öğe için ayrı pozisyon ve ayarlar
/// </summary>
public class TabelaItem : ReactiveObject
{
    private int _id;
    private string _name = string.Empty;
    private string _content = string.Empty;
    private TabelaItemType _itemType = TabelaItemType.Text;
    
    // Pozisyon ve Boyut
    private int _x = 0;
    private int _y = 0;
    private int _width = 50;
    private int _height = 16;
    
    // Görünüm
    private Color _color = Color.FromRgb(255, 176, 0); // Amber
    private Models.HorizontalAlignment _hAlign = Models.HorizontalAlignment.Left;
    
    // Çok renkli metin desteği
    private bool _useColoredSegments = false;
    private ObservableCollection<ColoredTextSegment> _coloredSegments = new();
    private Models.VerticalAlignment _vAlign = Models.VerticalAlignment.Center;
    private string _fontName = "PolarisRGB6x10M"; // Varsayılan font
    private int _letterSpacing = 1; // Harf aralığı (piksel)
    
    // Sembol özellikleri
    private string _symbolName = string.Empty; // AssetLibrary'deki sembol adı
    private int _symbolSize = 16; // Sembol boyutu (16 veya 32)
    
    // Animasyon
    private bool _isScrolling = false;
    private ScrollDirection _scrollDirection = ScrollDirection.Left;
    private int _scrollSpeed = 20;
    private double _scrollOffset = 0; // Kayan yazı için piksel offset
    
    // Çerçeve
    private BorderSettings _border = new();
    
    // Durum
    private bool _isSelected = false;
    private bool _isVisible = true;
    
    // Ara Durak Sistemi
    private IntermediateStopSettings _intermediateStops = new();

    /// <summary>
    /// Öğe ID'si
    /// </summary>
    public int Id
    {
        get => _id;
        set => this.RaiseAndSetIfChanged(ref _id, value);
    }

    /// <summary>
    /// Öğe adı (kullanıcı tanımlı)
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Öğe içeriği (metin veya dosya yolu)
    /// </summary>
    public string Content
    {
        get => _content;
        set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    /// <summary>
    /// Öğe tipi
    /// </summary>
    public TabelaItemType ItemType
    {
        get => _itemType;
        set => this.RaiseAndSetIfChanged(ref _itemType, value);
    }

    /// <summary>
    /// X pozisyonu (piksel)
    /// </summary>
    public int X
    {
        get => _x;
        set => this.RaiseAndSetIfChanged(ref _x, value);
    }

    /// <summary>
    /// Y pozisyonu (piksel)
    /// </summary>
    public int Y
    {
        get => _y;
        set => this.RaiseAndSetIfChanged(ref _y, value);
    }

    /// <summary>
    /// Genişlik (piksel)
    /// </summary>
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    /// <summary>
    /// Yükseklik (piksel)
    /// </summary>
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    /// <summary>
    /// Metin/öğe rengi (tek renk modu için)
    /// </summary>
    public Color Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }

    /// <summary>
    /// Çok renkli segment modu aktif mi
    /// true ise ColoredSegments kullanılır, false ise Content + Color kullanılır
    /// </summary>
    public bool UseColoredSegments
    {
        get => _useColoredSegments;
        set => this.RaiseAndSetIfChanged(ref _useColoredSegments, value);
    }

    /// <summary>
    /// Renkli metin segmentleri - her segment farklı renkte olabilir
    /// Örnek: "AÇIK" -> [("A", Yeşil), ("Ç", Kırmızı), ("I", Sarı), ("K", Mavi)]
    /// </summary>
    public ObservableCollection<ColoredTextSegment> ColoredSegments
    {
        get => _coloredSegments;
        set => this.RaiseAndSetIfChanged(ref _coloredSegments, value ?? new ObservableCollection<ColoredTextSegment>());
    }

    /// <summary>
    /// Tüm segmentlerin birleştirilmiş metni
    /// </summary>
    public string GetFullText()
    {
        if (!UseColoredSegments || ColoredSegments.Count == 0)
            return Content;
        
        var sb = new System.Text.StringBuilder();
        foreach (var segment in ColoredSegments)
        {
            sb.Append(segment.Text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Tek renkli metni çok renkli segmentlere dönüştürür (her harf ayrı segment)
    /// </summary>
    public void ConvertToColoredSegments()
    {
        ColoredSegments.Clear();
        foreach (char c in Content)
        {
            ColoredSegments.Add(new ColoredTextSegment(c.ToString(), Color));
        }
        UseColoredSegments = true;
    }

    /// <summary>
    /// Çok renkli segmentleri tek renkli metne dönüştürür
    /// </summary>
    public void ConvertToSingleColor()
    {
        Content = GetFullText();
        UseColoredSegments = false;
    }

    /// <summary>
    /// Yatay hizalama
    /// </summary>
    public Models.HorizontalAlignment HAlign
    {
        get => _hAlign;
        set => this.RaiseAndSetIfChanged(ref _hAlign, value);
    }

    /// <summary>
    /// Dikey hizalama
    /// </summary>
    public Models.VerticalAlignment VAlign
    {
        get => _vAlign;
        set => this.RaiseAndSetIfChanged(ref _vAlign, value);
    }

    /// <summary>
    /// Font adı (her öğe için ayrı font)
    /// </summary>
    public string FontName
    {
        get => _fontName;
        set => this.RaiseAndSetIfChanged(ref _fontName, value);
    }

    /// <summary>
    /// Harf aralığı (piksel)
    /// </summary>
    public int LetterSpacing
    {
        get => _letterSpacing;
        set => this.RaiseAndSetIfChanged(ref _letterSpacing, Math.Clamp(value, 0, 20));
    }

    /// <summary>
    /// Sembol adı (AssetLibrary'deki ikon adı)
    /// </summary>
    public string SymbolName
    {
        get => _symbolName;
        set => this.RaiseAndSetIfChanged(ref _symbolName, value);
    }

    /// <summary>
    /// Sembol boyutu (16 veya 32 piksel)
    /// </summary>
    public int SymbolSize
    {
        get => _symbolSize;
        set => this.RaiseAndSetIfChanged(ref _symbolSize, Math.Clamp(value, 16, 32));
    }

    /// <summary>
    /// Kayan yazı aktif mi
    /// </summary>
    public bool IsScrolling
    {
        get => _isScrolling;
        set => this.RaiseAndSetIfChanged(ref _isScrolling, value);
    }

    /// <summary>
    /// Kayma yönü
    /// </summary>
    public ScrollDirection ScrollDirection
    {
        get => _scrollDirection;
        set => this.RaiseAndSetIfChanged(ref _scrollDirection, value);
    }

    /// <summary>
    /// Kayma hızı (piksel/saniye)
    /// </summary>
    public int ScrollSpeed
    {
        get => _scrollSpeed;
        set => this.RaiseAndSetIfChanged(ref _scrollSpeed, value);
    }

    /// <summary>
    /// Kayan yazı offset'i (piksel) - animasyon sırasında güncellenir
    /// </summary>
    public double ScrollOffset
    {
        get => _scrollOffset;
        set => this.RaiseAndSetIfChanged(ref _scrollOffset, value);
    }

    /// <summary>
    /// Scroll offset'i sıfırlar
    /// </summary>
    public void ResetScrollOffset()
    {
        ScrollOffset = 0;
    }

    /// <summary>
    /// Scroll offset'i deltaTime'a göre günceller
    /// </summary>
    public void UpdateScrollOffset(double deltaTime, int contentWidth, int contentHeight)
    {
        if (!IsScrolling) return;
        
        double speed = ScrollSpeed * deltaTime;
        
        switch (ScrollDirection)
        {
            case ScrollDirection.Left:
                ScrollOffset -= speed;
                // Metin tamamen dışarı çıktıysa başa sar
                if (ScrollOffset < -contentWidth)
                    ScrollOffset = Width;
                break;
                
            case ScrollDirection.Right:
                ScrollOffset += speed;
                if (ScrollOffset > Width)
                    ScrollOffset = -contentWidth;
                break;
                
            case ScrollDirection.Up:
                ScrollOffset -= speed;
                if (ScrollOffset < -contentHeight)
                    ScrollOffset = Height;
                break;
                
            case ScrollDirection.Down:
                ScrollOffset += speed;
                if (ScrollOffset > Height)
                    ScrollOffset = -contentHeight;
                break;
        }
    }

    /// <summary>
    /// Seçili mi (UI için)
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// Görünür mü
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    /// <summary>
    /// Çerçeve ayarları
    /// </summary>
    public BorderSettings Border
    {
        get => _border;
        set => this.RaiseAndSetIfChanged(ref _border, value ?? new BorderSettings());
    }

    /// <summary>
    /// Ara durak ayarları
    /// Requirements: 4.1, 9.2
    /// </summary>
    public IntermediateStopSettings IntermediateStops
    {
        get => _intermediateStops;
        set => this.RaiseAndSetIfChanged(ref _intermediateStops, value ?? new IntermediateStopSettings());
    }

    /// <summary>
    /// Ara durak aktif mi ve durak var mı
    /// Requirements: 4.1
    /// </summary>
    public bool HasIntermediateStops => 
        IntermediateStops.IsEnabled && IntermediateStops.Stops.Count > 0;

    /// <summary>
    /// Öğenin sağ kenarı (X + Width)
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Öğenin alt kenarı (Y + Height)
    /// </summary>
    public int Bottom => Y + Height;
}

/// <summary>
/// Tabela öğe tipleri
/// </summary>
public enum TabelaItemType
{
    Text,
    Symbol,
    Image,
    Clock,
    Date
}

/// <summary>
/// Kayma yönleri
/// </summary>
public enum ScrollDirection
{
    Left,
    Right,
    Up,
    Down
}
