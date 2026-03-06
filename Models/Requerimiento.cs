using Obras.Api.Constants; // Importamos las constantes

namespace Obras.Api.Models;

public class Requerimiento
{
    public int Id { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    
    // Usamos la constante aquí
    public string Estado { get; set; } = RequerimientoEstados.Pendiente; 

    public int TrabajadorId { get; set; }
    public Trabajador? Trabajador { get; set; }

    public List<DetalleRequerimiento> Detalles { get; set; } = new();

    public int ProyectoId { get; set; }
    public Proyecto? Proyecto { get; set; }
}