using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class SavingsGoalsPage : ContentPage
{
    private readonly SavingsGoalsViewModel _vm;

    public SavingsGoalsPage(SavingsGoalsViewModel vm)
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
