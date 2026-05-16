using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Data;

public class AppDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BudgetItem> Budgets => Set<BudgetItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<DebtPayment> DebtPayments => Set<DebtPayment>();
    public DbSet<NetWorthSnapshot> NetWorthSnapshots => Set<NetWorthSnapshot>();

    public static string DbPath =>
        Path.Combine(FileSystem.AppDataDirectory, "finanzas.db3");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Filename={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Category>().HasIndex(c => c.Name).IsUnique();

        b.Entity<BudgetItem>()
            .HasIndex(x => new { x.CategoryId, x.Year, x.Month })
            .IsUnique();

        b.Entity<BudgetItem>().Property(x => x.Amount).HasConversion<double>();
        b.Entity<Transaction>().Property(x => x.Amount).HasConversion<double>();
        b.Entity<Account>().Property(x => x.Balance).HasConversion<double>();

        b.Entity<AppConfig>().Property(x => x.SalarioNeto).HasConversion<double>();
        b.Entity<AppConfig>().Property(x => x.OtrosIngresos).HasConversion<double>();
        b.Entity<AppConfig>().Property(x => x.MetaNecesidadesPct).HasConversion<double>();
        b.Entity<AppConfig>().Property(x => x.MetaDeseosPct).HasConversion<double>();
        b.Entity<AppConfig>().Property(x => x.MetaAhorroPct).HasConversion<double>();
        b.Entity<AppConfig>().Property(x => x.MetaAhorroMinimoPct).HasConversion<double>();
        b.Entity<AppConfig>().Property(x => x.MetaAhorroOptimoPct).HasConversion<double>();
        b.Entity<AppConfig>().Property(x => x.DiezmoPct).HasConversion<double>();

        b.Entity<SavingsGoal>().Property(x => x.Target).HasConversion<double>();
        b.Entity<SavingsGoal>().Property(x => x.Achieved).HasConversion<double>();
        b.Entity<SavingsGoal>().Property(x => x.MonthlyPlanned).HasConversion<double>();

        b.Entity<Income>().Property(x => x.Amount).HasConversion<double>();

        b.Entity<Debt>().Property(x => x.OriginalAmount).HasConversion<double>();
        b.Entity<Debt>().Property(x => x.CurrentBalance).HasConversion<double>();
        b.Entity<Debt>().Property(x => x.InterestRate).HasConversion<double>();
        b.Entity<Debt>().Property(x => x.MinPayment).HasConversion<double>();

        b.Entity<DebtPayment>().Property(x => x.Amount).HasConversion<double>();
        b.Entity<DebtPayment>().Property(x => x.InterestPortion).HasConversion<double>();
        b.Entity<DebtPayment>().Property(x => x.PrincipalPortion).HasConversion<double>();
        b.Entity<DebtPayment>()
            .HasOne(p => p.Debt)
            .WithMany()
            .HasForeignKey(p => p.DebtId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<NetWorthSnapshot>().Property(x => x.Assets).HasConversion<double>();
        b.Entity<NetWorthSnapshot>().Property(x => x.Liabilities).HasConversion<double>();
        b.Entity<NetWorthSnapshot>().Property(x => x.NetWorth).HasConversion<double>();
    }
}
