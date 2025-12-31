using System.Collections.Generic;
using ReactiveUI;

namespace LEDTabelam.Models;

/// <summary>
/// Tabela slot tanımı (001-999 arası numaralı)
/// Her slot, tabeladaki tüm öğeleri (metin, sembol, pozisyon, renk, font vb.) saklar
/// Requirements: 20.1, 20.2
/// </summary>
public class TabelaSlot : ReactiveObject
{
    private int _slotNumber;
    private string _name = string.Empty;
    private List<TabelaItem> _items = new();

    /// <summary>
    /// Slot numarası (1-999)
    /// </summary>
    public int SlotNumber
    {
        get => _slotNumber;
        set => this.RaiseAndSetIfChanged(ref _slotNumber, value);
    }

    /// <summary>
    /// Slot adı (kullanıcı tanımlı, örn: "M1 Korucuk - Gar")
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Tabeladaki tüm öğeler (metin, sembol vb.)
    /// Her öğenin pozisyonu, boyutu, rengi, fontu, çerçevesi vb. saklanır
    /// </summary>
    public List<TabelaItem> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value ?? new List<TabelaItem>());
    }

    /// <summary>
    /// Slot'un tanımlı olup olmadığını kontrol eder
    /// </summary>
    public bool IsDefined => Items.Count > 0 || !string.IsNullOrEmpty(Name);

    /// <summary>
    /// Slot'un özet açıklaması (liste görünümü için)
    /// </summary>
    public string Summary
    {
        get
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;
            
            if (Items.Count == 0)
                return "(Boş)";
            
            // İlk metin öğesinin içeriğini göster
            foreach (var item in Items)
            {
                if (item.ItemType == TabelaItemType.Text && !string.IsNullOrEmpty(item.Content))
                {
                    var text = item.Content;
                    return text.Length > 30 ? text.Substring(0, 30) + "..." : text;
                }
            }
            
            return $"{Items.Count} öğe";
        }
    }
}
