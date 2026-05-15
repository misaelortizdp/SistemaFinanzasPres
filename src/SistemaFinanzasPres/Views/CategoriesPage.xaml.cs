using SistemaFinanzasPres.Models;
using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _vm;
    public CategoriesPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnActiveToggled(object sender, ToggledEventArgs e)
    {
        if (sender is Switch sw && sw.BindingContext is Category c)
        {
            if (c.IsActive == e.Value) return;
            await _vm.ToggleActiveCommand.ExecuteAsync(c);
        }
    }
}
