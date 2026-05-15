using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class IncomeEditPage : ContentPage
{
    private readonly IncomeEditViewModel _vm;
    public IncomeEditPage(IncomeEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.IncomeId == 0) await _vm.LoadAsync();
    }
}
