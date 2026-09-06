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
        Routing.RegisterRoute(nameof(DebtEditPage), typeof(DebtEditPage));
        Routing.RegisterRoute(nameof(PatrimonioPage), typeof(PatrimonioPage));
        Routing.RegisterRoute(nameof(SavingsGoalsPage), typeof(SavingsGoalsPage));
        Routing.RegisterRoute(nameof(SavingsGoalEditPage), typeof(SavingsGoalEditPage));

        Navigated += OnShellNavigated;
    }

    private async void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (e.Source != ShellNavigationSource.ShellItemChanged
            && e.Source != ShellNavigationSource.ShellSectionChanged
            && e.Source != ShellNavigationSource.ShellContentChanged)
            return;

        try
        {
            while (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync(false);
        }
        catch { /* ignore */ }
    }
}
