namespace Obras.Api.Models;

public class Material
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Unit { get; set; }
    public decimal Quantity { get; set; }
    public required string Brand { get; set; }
    
    // NUEVO CAMPO: Estado del requerimiento
    public string Estado { get; set; } = "Pendiente"; 

    public Trabajador? Trabajador { get; set; }
    public int TrabajadorId { get; set; }
}