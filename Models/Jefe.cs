namespace Obras.Api.Models;

public class Jefe
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Apellido { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}

