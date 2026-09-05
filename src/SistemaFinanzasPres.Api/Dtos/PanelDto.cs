namespace SistemaFinanzasPres.Api.Dtos;

public class ResumenPanelDto
{
    public List<TendenciaMesDto> Tendencia { get; set; } = [];
    public List<GastoCategoriaPanelDto> GastosPorCategoria { get; set; } = [];
    public DistribucionDto Distribucion { get; set; } = new();
    public ProyeccionDto Proyeccion { get; set; } = new();
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
    public decimal Deuda { get; set; }
    public decimal Ahorro { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal IngresoDisponible { get; set; }
    public decimal DiezmoMonto { get; set; }
}

public class ProyeccionDto
{
    public int DiasTranscurridos { get; set; }
    public int DiasTotales { get; set; }
    public decimal GastoActual { get; set; }
    public decimal GastoProyectado { get; set; }
    public decimal TasaQuemaDiaria { get; set; }
    public decimal PresupuestoDiarioPermitido { get; set; }
    public decimal IngresoDisponible { get; set; }
}

public class KpisPanelDto
{
    public decimal TasaAhorroPct { get; set; }
    public decimal AhorroMensual { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal IngresoDisponible { get; set; }
    public decimal DiezmoMonto { get; set; }
    public decimal PctNecesidades { get; set; }
    public decimal PctDeseos { get; set; }
    public decimal PctDeuda { get; set; }
    public decimal PctAhorro { get; set; }
    public decimal FondoEmergenciaActual { get; set; }
    public decimal FondoEmergenciaMeta { get; set; }
    public int FondoEmergenciaMeses { get; set; }
    public decimal TotalDeudas { get; set; }
    public int DeudasActivas { get; set; }
}

public class TendenciaDetalleDto
{
    public List<TendenciaMesDetalleDto> Meses { get; set; } = [];
    public string? MejorMes { get; set; }
    public string? PeorMes { get; set; }
    public decimal PromedioTasaAhorro { get; set; }
    public decimal PromedioGastos { get; set; }
}

public class TendenciaMesDetalleDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos { get; set; }
    public decimal IngresoDisponible { get; set; }
    public decimal Ahorro { get; set; }
    public decimal TasaAhorroPct { get; set; }
    public string Flecha { get; set; } = "—";
}
