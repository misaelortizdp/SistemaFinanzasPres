namespace SistemaFinanzasPres.Api.Dtos;

public class LineaPresupuestoDto
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public string? NombreCategoria { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Monto { get; set; }
    public decimal Ejecutado { get; set; }
}
