namespace Obras.Api.Models;

public class Trabajador
{
    public int Id { get; set; }
    public required string NombreCompleto { get; set; } 

    public int UsuarioAccesoId { get; set; }
    public UsuarioAcceso? UsuarioAcceso { get; set; }

    // ¡NUEVO! Reemplazamos "int? ProyectoId" por una Lista
    // Relación Muchos a Muchos: Un trabajador puede estar en varias obras
    public List<Proyecto> Proyectos { get; set; } = new();
}