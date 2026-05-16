using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class PatrimonioPage : ContentPage
{
    private readonly PatrimonioViewModel _vm;

    public PatrimonioPage(PatrimonioViewModel vm)
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
