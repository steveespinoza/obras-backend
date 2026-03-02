using Microsoft.EntityFrameworkCore;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Endpoints;

public static class AlmacenEndpoints
{
    public static void MapAlmacenEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/almacen").RequireAuthorization();

        // GET: Obtener todos los materiales del catálogo
        group.MapGet("/", async (MaterialContext dbContext) => 
            await dbContext.MaterialesAlmacen
                .Select(m => new MaterialAlmacenDto(m.Id, m.Name))
                .AsNoTracking()
                .ToListAsync()
        );

        // POST: Agregar un nuevo material al catálogo
        group.MapPost("/", async (CreateMaterialAlmacenDto nuevoItem, MaterialContext dbContext) =>
        {
            MaterialAlmacen item = new() { Name = nuevoItem.Name };
            dbContext.MaterialesAlmacen.Add(item);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new MaterialAlmacenDto(item.Id, item.Name));
        });

        // DELETE: Borrar del catálogo
        group.MapDelete("/{id}", async (int id, MaterialContext dbContext) =>
        {
            await dbContext.MaterialesAlmacen.Where(m => m.Id == id).ExecuteDeleteAsync();
            return Results.NoContent();
        });
    }
}