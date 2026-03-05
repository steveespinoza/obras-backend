namespace Obras.Api.Models;

public class MaterialAlmacen
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // ¡NUEVO! Cada obra tiene su propio catálogo
    public int ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; }
}