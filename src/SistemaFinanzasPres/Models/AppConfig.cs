namespace SistemaFinanzasPres.Models;

public class AppConfig
{
    public int Id { get; set; } = 1;

    public decimal SalarioNeto { get; set; }
    public decimal OtrosIngresos { get; set; }

    public decimal DiezmoPct { get; set; } = 0.10m;

    public decimal MetaNecesidadesPct { get; set; } = 0.50m;
    public decimal MetaDeseosPct { get; set; } = 0.30m;
    public decimal MetaAhorroPct { get; set; } = 0.20m;

    public int FondoEmergenciaMeses { get; set; } = 4;
    public decimal MetaAhorroMinimoPct { get; set; } = 0.20m;
    public decimal MetaAhorroOptimoPct { get; set; } = 0.30m;

    public int CategoriaDiezmoId { get; set; }

    public decimal IngresoTotal => SalarioNeto + OtrosIngresos;
}
