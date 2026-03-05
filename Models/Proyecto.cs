namespace Obras.Api.Models;

public class Proyecto
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Ubicacion { get; set; }

    // ¡NUEVO!: Le agregamos el '?' para que la obra pueda nacer sin Admin
    public int? AdminId { get; set; }
    public UsuarioAcceso? Admin { get; set; }

    public List<Trabajador> Trabajadores { get; set; } = new();
    public List<MaterialAlmacen> CatalogoAlmacen { get; set; } = new();
    public List<Requerimiento> Requerimientos { get; set; } = new();
}