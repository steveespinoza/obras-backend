using Obras.Api.Dtos;
using Obras.Api.Endpoints;

namespace Obras.Api.Services;

// DTOs para la salida de datos
public record ProyectoSummaryDto(int Id, string Nombre, string Ubicacion, int? AdminId, string AdminNombre);
public record ProyectoDetailDto(int Id, string Nombre, string Ubicacion, int? AdminId, string? AdminNombre, string? AdminApellido, string? AdminNombreCompleto, string? AdminUsername, string? AdminTelefono);

public interface IProyectoService
{
    Task<List<ProyectoSummaryDto>> ObtenerTodosAsync();
    Task<ProyectoDetailDto?> ObtenerPorIdAsync(int id);
    Task<int> CrearAsync(CreateProyectoDto dto);
    Task<bool> ActualizarAdminAsync(int id, UpdateProyectoAdminDto dto);
}