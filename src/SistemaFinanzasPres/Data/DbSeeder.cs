using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.Data;

public static class DbSeeder
{
    public static async Task EnsureCreatedAndSeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Categories.AnyAsync())
        {
            var cats = new List<Category>
            {
                new() { Name = "Diezmo / Donación", Pillar = Pillar.PrimerFruto, SortOrder = 1 },

                new() { Name = "Vivienda",        Pillar = Pillar.Necesidad, SortOrder = 10 },
                new() { Name = "Servicios",       Pillar = Pillar.Necesidad, SortOrder = 11 },
                new() { Name = "Alimentación",    Pillar = Pillar.Necesidad, SortOrder = 12 },
                new() { Name = "Transporte",      Pillar = Pillar.Necesidad, SortOrder = 13 },
                new() { Name = "Salud / Seguros", Pillar = Pillar.Necesidad, SortOrder = 14 },

                new() { Name = "Entretenimiento", Pillar = Pillar.Deseo, SortOrder = 20 },
                new() { Name = "Restaurantes",    Pillar = Pillar.Deseo, SortOrder = 21 },
                new() { Name = "Suscripciones",   Pillar = Pillar.Deseo, SortOrder = 22 },
                new() { Name = "Ropa / Personal", Pillar = Pillar.Deseo, SortOrder = 23 },
                new() { Name = "Otros gastos",    Pillar = Pillar.Deseo, SortOrder = 24 },

                new() { Name = "Fondo de Emergencia", Pillar = Pillar.Ahorro, SortOrder = 30 },
                new() { Name = "Ahorro",              Pillar = Pillar.Ahorro, SortOrder = 31 },
                new() { Name = "Inversión",           Pillar = Pillar.Ahorro, SortOrder = 32 },
            };
            db.Categories.AddRange(cats);
            await db.SaveChangesAsync();
        }

        if (!await db.Accounts.AnyAsync())
        {
            db.Accounts.AddRange(
                new Account { Name = "Efectivo",          Balance = 0m, SortOrder = 1 },
                new Account { Name = "Cuenta bancaria",   Balance = 0m, SortOrder = 2 },
                new Account { Name = "Tarjeta de crédito",Balance = 0m, SortOrder = 3 }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.AppConfigs.AnyAsync())
        {
            var diezmo = await db.Categories.FirstAsync(c => c.Pillar == Pillar.PrimerFruto);
            db.AppConfigs.Add(new AppConfig
            {
                Id = 1,
                SalarioNeto = 0m,
                OtrosIngresos = 0m,
                MetaNecesidadesPct = 0.50m,
                MetaDeseosPct = 0.30m,
                MetaAhorroPct = 0.20m,
                FondoEmergenciaMeses = 4,
                MetaAhorroMinimoPct = 0.20m,
                MetaAhorroOptimoPct = 0.30m,
                CategoriaDiezmoId = diezmo.Id,
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Budgets.AnyAsync())
        {
            var now = DateTime.Today;
            foreach (var cat in await db.Categories.ToListAsync())
            {
                db.Budgets.Add(new BudgetItem
                {
                    CategoryId = cat.Id,
                    Year = now.Year,
                    Month = now.Month,
                    Amount = 0m,
                });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.SavingsGoals.AnyAsync())
        {
            db.SavingsGoals.AddRange(
                new SavingsGoal { Step = 1, Priority = "URGENTE",     Name = "Fondo de Emergencia",            Target = 0m, SortOrder = 1 },
                new SavingsGoal { Step = 2, Priority = "SIGUIENTE",   Name = "Eliminar deudas alto interés",   Target = 0m, SortOrder = 2 },
                new SavingsGoal { Step = 3, Priority = "DESPUÉS",     Name = "Ahorro a mediano plazo / CDT",   Target = 0m, SortOrder = 3 },
                new SavingsGoal { Step = 4, Priority = "FUTURO",      Name = "Inversión (ETFs / Fondos)",      Target = 0m, SortOrder = 4 },
                new SavingsGoal { Step = 5, Priority = "LARGO PLAZO", Name = "Pensión / Patrimonio",           Target = 0m, SortOrder = 5 }
            );
            await db.SaveChangesAsync();
        }
    }
}
