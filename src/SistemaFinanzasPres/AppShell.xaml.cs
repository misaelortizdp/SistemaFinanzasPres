using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(TransactionEditPage), typeof(TransactionEditPage));
        Routing.RegisterRoute(nameof(CategoriesPage), typeof(CategoriesPage));
        Routing.RegisterRoute(nameof(AccountsPage), typeof(AccountsPage));
        Routing.RegisterRoute(nameof(IncomeEditPage), typeof(IncomeEditPage));
        Routing.RegisterRoute(nameof(DebtsPage), typeof(DebtsPage));
        Routing.RegisterRoute(nameof(DebtEditPage), typeof(DebtEditPage));
    }
}
