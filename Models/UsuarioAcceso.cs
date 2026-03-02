namespace Obras.Api.Models;

public class UsuarioAcceso
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string Especialidad { get; set; }
    public required string Telefono { get; set; }

    // Relación de navegación
    public Trabajador? Trabajador { get; set; }
}