namespace SistemaFinanzasPres.Models;

public enum Pillar
{
    PrimerFruto = 0,
    Necesidad = 1,
    Deseo = 2,
    Ahorro = 3,
}

public static class PillarExtensions
{
    public static string Display(this Pillar p) => p switch
    {
        Pillar.PrimerFruto => "🙏 Primer Fruto",
        Pillar.Necesidad => "🏠 Necesidad",
        Pillar.Deseo => "🎉 Deseo",
        Pillar.Ahorro => "💰 Ahorro",
        _ => p.ToString(),
    };

    public static string Short(this Pillar p) => p switch
    {
        Pillar.PrimerFruto => "🙏 P.F.",
        Pillar.Necesidad => "🏠 Nec.",
        Pillar.Deseo => "🎉 Des.",
        Pillar.Ahorro => "💰 Aho.",
        _ => p.ToString(),
    };
}
