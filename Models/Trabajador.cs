namespace Obras.Api.Models;

public class Trabajador
{
    public int Id { get; set; }
    public required string NombreCompleto { get; set; } // Se llenará uniendo Nombre y Apellido

    // Foreign Key (Relación 1 a 1 con UsuarioAcceso)
    public int UsuarioAccesoId { get; set; }
    public UsuarioAcceso? UsuarioAcceso { get; set; }
}