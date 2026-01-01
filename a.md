Şu anki sistemde tüm ekran tek bir resim gibi çiziliyor. Oysa senin istediğinde:

Zone 1 (Sol): Sabit duracak (19K).

Zone 2 (Sağ Üst): Sabit duracak (KADIKÖY).

Zone 3 (Sağ Alt): Kayan yazı olacak (Durak 1 > Durak 2...).

İşte bu sistemi kurmak için yapman gerekenler:

Adım 1: Model Güncellemesi (Zone.cs)
Her bölgenin kendine ait bir animasyonu ve kendine ait bir metin listesi olmalı.

Models/Zone.cs dosyasını aç ve şu özellikleri ekle:

C#

public class Zone : ViewModelBase // INotifyPropertyChanged için
{
    // ... Mevcut X, Y, Width, Height özelliklerin ...

    private AnimationType _animationType;
    public AnimationType AnimationType
    {
        get => _animationType;
        set => this.RaiseAndSetIfChanged(ref _animationType, value);
    }

    // Tek bir metin yerine durak listesi tutmak için:
    private ObservableCollection<string> _items = new();
    public ObservableCollection<string> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }
    
    // Yardımcı: Eğer listede birden fazla şey varsa aralarına ok koyarak birleştirir
    public string GetFormattedText()
    {
        if (Items.Count == 0) return "";
        if (Items.Count == 1) return Items[0];
        // Duraklar arasına " >> " veya " - " koyar
        return string.Join("  >>  ", Items);
    }
}
Adım 2: Çizim Motorunu "Akıllandırma" (LedRenderer.cs)
LedRenderer artık tüm ekranı tek seferde çizmemeli. Her bölgeyi ayrı ayrı çizmeli ve her birine farklı efekt uygulamalı.

Services/LedRenderer.cs içindeki RenderDisplay metodunu baştan tasarlayalım:

C#

// NOT: Bu metoda 'frameIndex' veya 'time' parametresi eklemelisin ki animasyon ilerlesin.
public SKBitmap RenderDisplay(Profile profile, DisplaySettings settings, long frameCounter)
{
    // 1. Ana Tuvali (Siyah Panel) Oluştur
    var mainBitmap = new SKBitmap(settings.ActualWidth, settings.ActualHeight);
    using var canvas = new SKCanvas(mainBitmap);
    canvas.Clear(SKColors.Black);

    // 2. Her Bölgeyi (Zone) Ayrı Ayrı Çiz ve Yapıştır
    foreach (var zone in profile.Zones)
    {
        // Bölgenin kendi küçük resmini oluştur
        using var zoneBitmap = new SKBitmap(zone.Width, zone.Height);
        using var zoneCanvas = new SKCanvas(zoneBitmap);
        zoneCanvas.Clear(SKColors.Black); // Bölge arkaplanı

        // Metni al (Liste ise birleştirilmiş halini al)
        string textToDraw = zone.GetFormattedText();
        
        // --- ANİMASYON MANTIĞI ---
        float xPosition = 0;
        
        if (zone.AnimationType == AnimationType.ScrollLeft)
        {
            // Metin genişliğini hesapla
            float textWidth = MeasureTextWidth(textToDraw, zone.Font); // Bu metodun var sayıyorum
            
            // Eğer metin bölgeye sığıyorsa kaydırma
            if (textWidth <= zone.Width) 
            {
                xPosition = (zone.Width - textWidth) / 2; // Ortala
            }
            else
            {
                // Kayan Yazı Formülü: (Zaman * Hız) % Toplam Uzunluk
                // frameCounter: Animasyon servisinden gelen sayaç
                int speed = 2; // Hız ayarı
                long totalPixels = (long)(textWidth + zone.Width); 
                long currentStep = (frameCounter * speed) % totalPixels;
                xPosition = zone.Width - currentStep;
            }
        }
        else // Sabit (Static)
        {
            // Ortala
            float textWidth = MeasureTextWidth(textToDraw, zone.Font);
            xPosition = (zone.Width - textWidth) / 2;
        }

        // Metni Bölge Canvas'ına Çiz
        // (Burada senin mevcut DrawText metodunu kullanacaksın)
        DrawTextToCanvas(zoneCanvas, textToDraw, xPosition, 2, zone.TextColor); 

        // 3. Bölgeyi Ana Ekrana Yapıştır (X, Y koordinatına)
        canvas.DrawBitmap(zoneBitmap, zone.X, zone.Y);
    }

    // 4. (Opsiyonel) Izgara / Glow Efektlerini Ana Resme Uygula
    // ApplyPostEffects(mainBitmap); 

    return mainBitmap;
}
Adım 3: Arayüz İyileştirmesi (VisualZoneEditor)
Kullanıcının "Duraklar" kısmına birden fazla yazı girebilmesi için arayüzü güncellemelisin.

Views/UnifiedEditor.axaml veya ilgili editör dosyasında, seçili Zone'un özelliklerini gösteren paneli şöyle güncelle:

XML

<ComboBox SelectedItem="{Binding SelectedZone.AnimationType}"
          ItemsSource="{Binding AnimationTypes}" 
          Header="Animasyon Tipi"/>

<TextBlock Text="İçerik / Duraklar" Margin="0,10,0,5"/>

<ItemsControl ItemsSource="{Binding SelectedZone.Items}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Grid ColumnDefinitions="*, Auto">
                <TextBox Text="{Binding .}" Margin="0,2"/>
                <Button Content="Sil" Command="{Binding $parent[UserControl].DataContext.RemoveItemCommand}" 
                        CommandParameter="{Binding .}" Grid.Column="1"/>
            </Grid>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<StackPanel Orientation="Horizontal" Margin="0,5">
    <TextBox x:Name="NewItemBox" Watermark="Yeni Durak Ekle..." Width="150"/>
    <Button Content="Ekle" Command="{Binding AddItemCommand}" 
            CommandParameter="{Binding #NewItemBox.Text}"/>
</StackPanel>
Sonuç Ne Olacak?
Bu yapıyı kurduğunda "Polaris Modu" şöyle çalışır:

Uygulamada 3 tane Zone oluşturursun.

Sol Zone: Metin="19K", Animasyon="None".

Üst Zone: Metin="KADIKÖY", Animasyon="None".

Alt Zone: Metinler=["Ayrılık Çeşmesi", "Acıbadem", "Altunizade"], Animasyon="ScrollLeft".

Uygulama çalışınca:

Sol ve Üst sabit kalır.

Alt kısımda yazılar birleşir: "Ayrılık Çeşmesi >> Acıbadem >> Altunizade" ve sadece bu kısım kayar.

Bu tam olarak profesyonel otobüs tabelalarının çalışma mantığıdır. Önce Adım 2'deki Render mantığını (döngü içinde ayrı canvas kullanımı) uygulamanı tavsiye ederim.