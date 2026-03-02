using System.ComponentModel.DataAnnotations;

namespace Obras.Api.Dtos;

public record  UpdateMatDto(
    [Required][StringLength(50)] string Name,
    [Required][StringLength(20)] string Unit,
    [Range(1,100)]decimal Quantity,   
    string Brand,
    [Range(1,50)]int TrabajadorId
);
