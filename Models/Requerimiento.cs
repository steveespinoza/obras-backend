namespace Obras.Api.Models;

public class Requerimiento
{
    public int Id { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobado, Rechazado

    // Relación con el trabajador que lo pidió
    public int TrabajadorId { get; set; }
    public Trabajador? Trabajador { get; set; }

    // Relación 1 a Muchos: Un requerimiento tiene muchos detalles (materiales)
    public List<DetalleRequerimiento> Detalles { get; set; } = new();
}