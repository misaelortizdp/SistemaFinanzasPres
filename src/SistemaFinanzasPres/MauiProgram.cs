using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Services;
using SistemaFinanzasPres.ViewModels;
using SistemaFinanzasPres.Views;

namespace SistemaFinanzasPres;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddTransient<AppDbContext>();

        builder.Services.AddSingleton<MonthService>();
        builder.Services.AddTransient<BudgetService>();
        builder.Services.AddTransient<KpiService>();
        builder.Services.AddTransient<IncomeService>();
        builder.Services.AddTransient<DebtService>();

        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<BudgetViewModel>();
        builder.Services.AddTransient<TransactionsViewModel>();
        builder.Services.AddTransient<TransactionEditViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<AccountsViewModel>();
        builder.Services.AddTransient<KpisViewModel>();
        builder.Services.AddTransient<ConfigViewModel>();
        builder.Services.AddTransient<IncomesViewModel>();
        builder.Services.AddTransient<IncomeEditViewModel>();
        builder.Services.AddTransient<DebtsViewModel>();
        builder.Services.AddTransient<DebtEditViewModel>();

        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<BudgetPage>();
        builder.Services.AddTransient<TransactionsPage>();
        builder.Services.AddTransient<TransactionEditPage>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<AccountsPage>();
        builder.Services.AddTransient<KpisPage>();
        builder.Services.AddTransient<ConfigPage>();
        builder.Services.AddTransient<IncomesPage>();
        builder.Services.AddTransient<IncomeEditPage>();
        builder.Services.AddTransient<DebtsPage>();
        builder.Services.AddTransient<DebtEditPage>();

        return builder.Build();
    }
}
