using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class ConfigPage : ContentPage
{
    private readonly ConfigViewModel _vm;
    public ConfigPage(ConfigViewModel vm)
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
