using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class DebtsPage : ContentPage
{
    private readonly DebtsViewModel _vm;
    public DebtsPage(DebtsViewModel vm)
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
