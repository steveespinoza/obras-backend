namespace Obras.Api.Dtos;

public record MatDetailsDto(
    int Id,
    string Name,
    string Unit,
    decimal Quantity,   
    string Brand,
    int TrabajadorId
);
