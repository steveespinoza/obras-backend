using System.ComponentModel.DataAnnotations;

namespace Obras.Api.Dtos;

public record CreateMaterialAlmacenDto(
    [Required][StringLength(50)] string Name
);