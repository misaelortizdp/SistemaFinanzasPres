using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class TrendsPage : ContentPage
{
    private readonly TrendsViewModel _vm;

    public TrendsPage(TrendsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
