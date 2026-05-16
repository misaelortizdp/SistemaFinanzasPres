using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class SavingsGoalEditPage : ContentPage
{
    private readonly SavingsGoalEditViewModel _vm;

    public SavingsGoalEditPage(SavingsGoalEditViewModel vm)
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
