namespace LEDTabelam.Maui;

public partial class MainPageSimple : ContentPage
{
    public MainPageSimple()
    {
        InitializeComponent();
    }

    private async void OnTestClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Test", "Buton çalışıyor!", "Tamam");
    }
}
