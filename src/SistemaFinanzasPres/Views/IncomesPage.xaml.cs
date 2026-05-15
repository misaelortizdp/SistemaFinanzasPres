using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class IncomesPage : ContentPage
{
    private readonly IncomesViewModel _vm;
    public IncomesPage(IncomesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
