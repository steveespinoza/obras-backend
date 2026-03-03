namespace Obras.Api.Models;

public class DetalleRequerimiento
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Unit { get; set; }
    public decimal Quantity { get; set; }
    public required string Brand { get; set; }

    // Llave foránea hacia el Requerimiento padre
    public int RequerimientoId { get; set; }
    public Requerimiento? Requerimiento { get; set; }
}