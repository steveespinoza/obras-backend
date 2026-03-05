using Microsoft.EntityFrameworkCore;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;
using System.Security.Claims; // <-- NUEVO: Necesario para leer los Claims

namespace Obras.Api.Endpoints;

public static class AlmacenEndpoints
{
    public static void MapAlmacenEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/almacen").RequireAuthorization();

        // GET: Obtener solo los materiales de MI proyecto
        group.MapGet("/", async (MaterialContext dbContext, ClaimsPrincipal user) => 
        {
            // 1. Extraemos el ProyectoId del Token
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            return await dbContext.MaterialesAlmacen
                // 2. Filtramos la base de datos mágicamente
                .Where(m => m.ProyectoId == proyectoId)
                .Select(m => new MaterialAlmacenDto(m.Id, m.Name))
                .AsNoTracking()
                .ToListAsync();
        });

        // POST: Agregar un material AL PROYECTO ACTUAL
        group.MapPost("/", async (CreateMaterialAlmacenDto nuevoItem, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            MaterialAlmacen item = new() 
            { 
                Name = nuevoItem.Name,
                ProyectoId = proyectoId // <-- Asignamos el material a esta obra
            };
            
            dbContext.MaterialesAlmacen.Add(item);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new MaterialAlmacenDto(item.Id, item.Name));
        });

        // DELETE: Borrar del catálogo (Validando que sea de tu proyecto)
        group.MapDelete("/{id}", async (int id, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            // Borramos solo si el ID coincide Y pertenece al proyecto del usuario
            var filasBorradas = await dbContext.MaterialesAlmacen
                .Where(m => m.Id == id && m.ProyectoId == proyectoId)
                .ExecuteDeleteAsync();
            
            if (filasBorradas == 0) return Results.NotFound();

            return Results.NoContent();
        });
    }
}