using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Obras.Api.Dtos;

public record LoginDto(string Username, string Password);   