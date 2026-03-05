// Modifica el DTO de registro
public record CreateUserDto(string Nombre, string Apellido, string Username, string Password, string Especialidad, string Telefono, int? ProyectoId);

// El Password es opcional (string?) porque si no lo escriben, no lo cambiamos.
public record UpdateUserDto(string Nombre, string Apellido, string Username, string? Password, string? Telefono);