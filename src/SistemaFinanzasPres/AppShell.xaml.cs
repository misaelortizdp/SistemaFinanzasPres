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
    }
}
