using Obras.Api.Dtos;

namespace Obras.Api.Services;

public interface IRequerimientoService
{
// Reemplaza la línea vieja por esta:
    Task<PaginacionDto<RequerimientoSummaryDto>> ObtenerTodosAsync(int proyectoId, int pagina, int cantidad, string rol, int trabajadorId);
    Task<RequerimientoDetailsDto?> ObtenerPorIdAsync(int id, int proyectoId);
    Task<int> CrearAsync(CreateRequerimientoDto dto, int proyectoId);
    Task<bool> CambiarEstadoAsync(int id, string estado, int proyectoId);
    
    // Devolvemos una tupla para saber si falló por una regla de negocio
    Task<(bool Exito, string MensajeError)> ActualizarCompletoAsync(int id, UpdateRequerimientoDto dto, int proyectoId);
    
    Task<object> ObtenerReporteAsync(string material, DateTime inicio, DateTime fin, int proyectoId);
    Task<bool> EliminarAsync(int id, int proyectoId);
}