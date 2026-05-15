using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SistemaFinanzasPres.Data;
using SistemaFinanzasPres.Models;

namespace SistemaFinanzasPres.ViewModels;

public partial class CategoriesViewModel : BaseViewModel
{
    private readonly AppDbContext _db;

    public CategoriesViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Categorías";
    }

    public ObservableCollection<Category> Items { get; } = new();
    public List<Pillar> PillarOptions { get; } = Enum.GetValues<Pillar>().ToList();

    [ObservableProperty] private string newName = string.Empty;
    [ObservableProperty] private Pillar newPillar = Pillar.Necesidad;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Pillar).ThenBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();
        Items.Clear();
        foreach (var c in data) Items.Add(c);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName)) return;
        var name = NewName.Trim();
        if (await _db.Categories.AnyAsync(c => c.Name == name))
        {
            await Shell.Current.DisplayAlert("Categorías", "Ya existe una categoría con ese nombre.", "OK");
            return;
        }
        var max = await _db.Categories.MaxAsync(c => (int?)c.SortOrder) ?? 0;
        _db.Categories.Add(new Category { Name = name, Pillar = NewPillar, SortOrder = max + 1 });
        await _db.SaveChangesAsync();
        NewName = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ChangePillarAsync(Category? c)
    {
        if (c == null) return;
        var options = Enum.GetValues<Pillar>().Select(p => p.Display()).ToArray();
        var sel = await Shell.Current.DisplayActionSheet($"Pilar de {c.Name}", "Cancelar", null, options);
        if (string.IsNullOrEmpty(sel) || sel == "Cancelar") return;
        var newPil = Enum.GetValues<Pillar>().First(p => p.Display() == sel);
        var entity = await _db.Categories.FindAsync(c.Id);
        if (entity == null) return;
        entity.Pillar = newPil;
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RenameAsync(Category? c)
    {
        if (c == null) return;
        var nuevo = await Shell.Current.DisplayPromptAsync("Renombrar", "Nuevo nombre:", initialValue: c.Name);
        if (string.IsNullOrWhiteSpace(nuevo)) return;
        var entity = await _db.Categories.FindAsync(c.Id);
        if (entity == null) return;
        entity.Name = nuevo.Trim();
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(Category? c)
    {
        if (c == null) return;
        var entity = await _db.Categories.FindAsync(c.Id);
        if (entity == null) return;
        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(Category? c)
    {
        if (c == null) return;
        var hasTx = await _db.Transactions.AnyAsync(t => t.CategoryId == c.Id);
        if (hasTx)
        {
            await Shell.Current.DisplayAlert("No se puede eliminar",
                "Esta categoría tiene gastos asociados. Desactívala en su lugar.", "OK");
            return;
        }
        var ok = await Shell.Current.DisplayAlert("Eliminar", $"¿Eliminar '{c.Name}'?", "Sí", "No");
        if (!ok) return;
        var entity = await _db.Categories.FindAsync(c.Id);
        if (entity == null) return;
        _db.Categories.Remove(entity);
        var budgets = await _db.Budgets.Where(b => b.CategoryId == c.Id).ToListAsync();
        _db.Budgets.RemoveRange(budgets);
        await _db.SaveChangesAsync();
        await LoadAsync();
    }
}
