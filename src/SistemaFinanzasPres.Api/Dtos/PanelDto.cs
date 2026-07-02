namespace SistemaFinanzasPres.Api.Dtos;

public class ResumenPanelDto
{
    public List<TendenciaMesDto> Tendencia { get; set; } = [];
    public List<GastoCategoriaPanelDto> GastosPorCategoria { get; set; } = [];
    public DistribucionDto Distribucion { get; set; } = new();
}

public class TendenciaMesDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos { get; set; }
}

public class GastoCategoriaPanelDto
{
    public string Nombre { get; set; } = "";
    public decimal Monto { get; set; }
    public string? Color { get; set; }
    public string? Icono { get; set; }
}

public class DistribucionDto
{
    public decimal Necesidades { get; set; }
    public decimal Deseos { get; set; }
    public decimal Ahorro { get; set; }
    public decimal TotalIngresos { get; set; }
}
