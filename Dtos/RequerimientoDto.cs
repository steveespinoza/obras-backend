using System.ComponentModel.DataAnnotations;

namespace Obras.Api.Dtos;

// 1. DTOs para CREAR (Entrada de datos desde React)
public record CreateDetalleDto(
    [Required][StringLength(50)] string Name,
    [Required][StringLength(20)] string Unit,
    [Range(0.01, 10000)] decimal Quantity,  
    [Required] string Brand
);

public record CreateRequerimientoDto(
    [Range(1, int.MaxValue)] int TrabajadorId,
    [Required][MinLength(1, ErrorMessage = "Debe incluir al menos un material")] List<CreateDetalleDto> Detalles
);

// 2. DTOs para LEER (Salida hacia React)
// Para la lista principal (no cargamos todos los detalles para ahorrar memoria)
public record RequerimientoSummaryDto(
    int Id, 
    DateTime FechaSolicitud, 
    string Estado, 
    string Trabajador, 
    string Especialidad, 
    int CantidadMaterialesDiferentes
);

// Para ver los detalles completos de un requerimiento específico
public record DetalleLecturaDto(int Id, string Name, string Unit, decimal Quantity, string Brand);

public record RequerimientoDetailsDto(
    int Id, 
    DateTime FechaSolicitud, 
    string Estado, 
    string Trabajador,
    List<DetalleLecturaDto> Detalles
);

public record CambiarEstadoRequerimientoDto(string Estado);

// --- AGREGAR ESTO AL FINAL DE TU ARCHIVO RequerimientoDtos.cs ---

public record UpdateDetalleDto(
    int? Id, // Opcional: Si viene nulo, significa que es un material nuevo añadido al pedido
    [Required][StringLength(50)] string Name,
    [Required][StringLength(20)] string Unit,
    [Range(0.01, 10000)] decimal Quantity,
    [Required] string Brand
);

public record UpdateRequerimientoDto(
    [Required][MinLength(1, ErrorMessage = "Debe incluir al menos un material")] List<UpdateDetalleDto> Detalles
);