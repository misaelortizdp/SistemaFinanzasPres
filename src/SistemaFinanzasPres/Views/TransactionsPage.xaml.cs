using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class TransactionsPage : ContentPage
{
    private readonly TransactionsViewModel _vm;
    public TransactionsPage(TransactionsViewModel vm)
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
