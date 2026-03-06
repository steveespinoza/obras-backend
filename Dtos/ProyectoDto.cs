namespace Obras.Api.Dtos;

public record CreateProyectoDto(string Nombre, string Ubicacion, int? AdminId);

public record UpdateProyectoAdminDto(int? NuevoAdminId);