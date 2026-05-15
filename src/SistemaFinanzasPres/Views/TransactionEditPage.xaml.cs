using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class TransactionEditPage : ContentPage
{
    private readonly TransactionEditViewModel _vm;
    public TransactionEditPage(TransactionEditViewModel vm)
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
