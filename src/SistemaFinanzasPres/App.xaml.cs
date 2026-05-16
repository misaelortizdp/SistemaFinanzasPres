using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Services;

namespace SistemaFinanzasPres;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell()) { Title = "Finanzas Personales" };
        _ = InitializeDatabaseAsync();
        return window;
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeeder.EnsureCreatedAndSeedAsync(db);

        try
        {
            var netWorth = scope.ServiceProvider.GetRequiredService<NetWorthService>();
            await netWorth.EnsureMonthlySnapshotAsync();
        }
        catch { /* no bloquear el arranque si falla */ }
    }
}
