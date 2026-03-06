using Obras.Api.Dtos;

namespace Obras.Api.Services;

public record UserSummaryDto(int Id, string NombreCompleto, string Username, string Especialidad, string Telefono, string ProyectoNombre);
public record LoginResponseDto(int Id, string Name, string Role, int? ProyectoId, string Token);

public interface IUserService
{
    Task<List<UserSummaryDto>> ObtenerTodosAsync(string rol, int miProyectoId);
    Task<LoginResponseDto?> LoginAsync(LoginDto loginInfo);
    
    // Devolvemos una tupla (Éxito, MensajeError) para controlar las respuestas HTTP
    Task<(bool Exito, string Mensaje)> RegistrarAsync(CreateUserDto dto, string rolCreador, int adminProyectoId);
    Task<(bool Exito, string Mensaje, bool NoEncontrado)> ActualizarAsync(int id, UpdateUserDto dto);
}