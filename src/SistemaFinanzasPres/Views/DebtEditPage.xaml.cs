using SistemaFinanzasPres.ViewModels;

namespace SistemaFinanzasPres.Views;

public partial class DebtEditPage : ContentPage
{
    public DebtEditPage(DebtEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
