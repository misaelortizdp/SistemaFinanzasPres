namespace SistemaFinanzasPres.Api.Dtos;

public record LineaPatrimonioDto(string Nombre, decimal Monto);

public class PatrimonioActualDto
{
    public List<LineaPatrimonioDto> Activos { get; set; } = new();
    public List<LineaPatrimonioDto> Pasivos { get; set; } = new();
    public decimal TotalActivos { get; set; }
    public decimal TotalPasivos { get; set; }
    public decimal PatrimonioNeto { get; set; }
}

public class SnapshotPatrimonialDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Activos { get; set; }
    public decimal Pasivos { get; set; }
    public decimal PatrimonioNeto { get; set; }
    public string? Notas { get; set; }
}

public class CrearSnapshotDto
{
    public string? Notas { get; set; }
}
