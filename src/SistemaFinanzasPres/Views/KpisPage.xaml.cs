using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class KpisPage : ContentPage
{
    private readonly KpisViewModel _vm;
    public KpisPage(KpisViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}
