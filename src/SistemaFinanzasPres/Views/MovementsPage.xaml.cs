using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class MovementsPage : ContentPage
{
    private readonly MovementsViewModel _vm;
    public MovementsPage(MovementsViewModel vm)
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
