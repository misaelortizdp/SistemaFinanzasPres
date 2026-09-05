namespace SistemaFinanzasPres.Api.Dtos;

public class ConfigUsuarioDto
{
    public decimal DiezmoPct { get; set; } = 0.10m;
    public decimal MetaNecesidadesPct { get; set; } = 0.50m;
    public decimal MetaDeseosPct { get; set; } = 0.30m;
    public decimal MetaDeudaPct { get; set; } = 0m;
    public decimal MetaAhorroPct { get; set; } = 0.20m;
    public int FondoEmergenciaMeses { get; set; } = 4;
    public decimal MetaAhorroMinimoPct { get; set; } = 0.20m;
    public decimal MetaAhorroOptimoPct { get; set; } = 0.30m;
    public int? CategoriaDiezmoId { get; set; }
    public bool SnapshotAutomatico { get; set; } = true;
    public int SnapshotDia { get; set; } = 1;
}
