using Obras.Api.Dtos;

namespace Obras.Api.Services;

public interface IAlmacenService
{
    Task<List<MaterialAlmacenDto>> ObtenerMaterialesPorProyectoAsync(int proyectoId);
    Task<MaterialAlmacenDto> AgregarMaterialAsync(CreateMaterialAlmacenDto dto, int proyectoId);
    Task<bool> EliminarMaterialAsync(int id, int proyectoId);
}