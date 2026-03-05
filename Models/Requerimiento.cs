namespace Obras.Api.Models;

public class Requerimiento
{
    public int Id { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public string Estado { get; set; } = "Pendiente"; 

    public int TrabajadorId { get; set; }
    public Trabajador? Trabajador { get; set; }

    public List<DetalleRequerimiento> Detalles { get; set; } = new();

    // ¡NUEVO! A qué proyecto pertenece este pedido
    public int ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; }
}