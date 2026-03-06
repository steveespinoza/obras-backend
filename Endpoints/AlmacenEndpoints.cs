using Obras.Api.Dtos;
using Obras.Api.Services; // <-- Importamos los servicios
using System.Security.Claims;

namespace Obras.Api.Endpoints;

public static class AlmacenEndpoints
{
    public static void MapAlmacenEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/almacen").RequireAuthorization();

        // GET: Obtener solo los materiales de MI proyecto
        group.MapGet("/", async (IAlmacenService almacenService, ClaimsPrincipal user) => 
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var materiales = await almacenService.ObtenerMaterialesPorProyectoAsync(proyectoId);
            
            return Results.Ok(materiales);
        });

        // POST: Agregar un material AL PROYECTO ACTUAL
        group.MapPost("/", async (CreateMaterialAlmacenDto nuevoItem, IAlmacenService almacenService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var materialCreado = await almacenService.AgregarMaterialAsync(nuevoItem, proyectoId);
            
            return Results.Ok(materialCreado);
        });

        // DELETE: Borrar del catálogo
        group.MapDelete("/{id}", async (int id, IAlmacenService almacenService, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var eliminadoExitosamente = await almacenService.EliminarMaterialAsync(id, proyectoId);
            
            return eliminadoExitosamente ? Results.NoContent() : Results.NotFound();
        });
    }
}