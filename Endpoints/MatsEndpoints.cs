using Obras.Api.Dtos;
using Obras.Api.Services; // <-- Usamos los servicios
using System.Security.Claims;

namespace Obras.Api.Endpoints;

public static class RequerimientosEndpoints
{
    public static void MapRequerimientosEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/requerimientos").RequireAuthorization();

        // GET: Obtener lista de pedidos
// GET: Obtener lista de pedidos
        group.MapGet("/", async (int? pagina, int? cantidad, IRequerimientoService reqService, ClaimsPrincipal user) =>
        {
            int p = pagina ?? 1;
            int c = cantidad ?? 50; 
            
            // Extraemos los datos seguros del Token (JWT)
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            string rol = user.FindFirst(ClaimTypes.Role)!.Value;
            int trabajadorId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            
            // El servicio ahora sabrá exactamente quién está pidiendo la información
            var requerimientos = await reqService.ObtenerTodosAsync(proyectoId, p, c, rol, trabajadorId);
            return Results.Ok(requerimientos);
        });

        // GET: Obtener UN pedido con detalles
        group.MapGet("/{id}", async (int id, IRequerimientoService reqService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var req = await reqService.ObtenerPorIdAsync(id, proyectoId);
            
            return req is not null ? Results.Ok(req) : Results.NotFound();
        });

        // POST: Crear un nuevo pedido
        group.MapPost("/", async (CreateRequerimientoDto dto, IRequerimientoService reqService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var nuevoId = await reqService.CrearAsync(dto, proyectoId);
            
            return Results.Ok(new { Mensaje = "Requerimiento guardado", Id = nuevoId });
        });

        // PUT: Cambiar estado (Aprobado/Rechazado)
        group.MapPut("/{id}/estado", async (int id, CambiarEstadoRequerimientoDto dto, IRequerimientoService reqService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var exito = await reqService.CambiarEstadoAsync(id, dto.Estado, proyectoId);
            
            return exito ? Results.NoContent() : Results.NotFound();
        });

        // PUT: Actualizar un pedido completo
        group.MapPut("/{id}", async (int id, UpdateRequerimientoDto dto, IRequerimientoService reqService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var resultado = await reqService.ActualizarCompletoAsync(id, dto, proyectoId);

            if (!resultado.Exito)
            {
                if (resultado.MensajeError == "No encontrado") return Results.NotFound();
                return Results.BadRequest(new { Message = resultado.MensajeError });
            }

            return Results.NoContent();
        });

        // GET: Reporte
        group.MapGet("/reporte", async (string material, DateTime inicio, DateTime fin, IRequerimientoService reqService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var reporte = await reqService.ObtenerReporteAsync(material, inicio, fin, proyectoId);
            return Results.Ok(reporte);
        });

        // DELETE: Borrar un pedido completo
        group.MapDelete("/{id}", async (int id, IRequerimientoService reqService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var exito = await reqService.EliminarAsync(id, proyectoId);
            
            return exito ? Results.NoContent() : Results.NotFound();
        });
    }
}