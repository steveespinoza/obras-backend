namespace Obras.Api.Dtos;

public record MatSummaryDto(int Id, string Name, string Unit, decimal Quantity, string Brand, string Trabajador, string Especialidad, string Estado);

public record CambiarEstadoDto(string Estado);
