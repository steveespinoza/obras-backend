using Microsoft.EntityFrameworkCore;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Endpoints;

public static class MatsEndpoints
{
    const string GetMatEndpointName = "GetMat";

    public static void MapMatsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/mats").RequireAuthorization();

        // GET /mats
        group.MapGet("/", async (MaterialContext dbContext) => 
            await dbContext.Mats
                .Include(mat => mat.Trabajador)
                    .ThenInclude(t => t.UsuarioAcceso) // <-- NUEVO: Entramos a la tabla de Acceso
                .Select(mat => new MatSummaryDto(
                    mat.Id,
                    mat.Name,
                    mat.Unit,
                    mat.Quantity,
                    mat.Brand,
                    mat.Trabajador!.NombreCompleto, // <-- 1. Nombre Completo
                    mat.Trabajador.UsuarioAcceso!.Especialidad, // <-- 2. Especialidad
                    mat.Estado
                ))
                .AsNoTracking()
                .ToListAsync()
        );

        // GET /mats/1
        group.MapGet("/{id}", async (int id, MaterialContext dbContext) =>
        {
            var mat = await dbContext.Mats.FindAsync(id);
            return mat is null ? Results.NotFound() : Results.Ok(
                new MatDetailsDto(mat.Id, mat.Name, mat.Unit, mat.Quantity, mat.Brand, mat.TrabajadorId)
            );
        }).WithName(GetMatEndpointName);

        // POST /mats
        group.MapPost("/", async (CreateMatDto newMat, MaterialContext dbContext) =>
        {
            Material mat = new()
            {
                Name = newMat.Name,
                Unit = newMat.Unit,
                Quantity = newMat.Quantity,
                Brand = newMat.Brand,
                TrabajadorId = newMat.TrabajadorId // <-- Cambiado
            };
            dbContext.Mats.Add(mat);
            await dbContext.SaveChangesAsync();

            MatDetailsDto matDto = new(mat.Id, mat.Name, mat.Unit, mat.Quantity, mat.Brand, mat.TrabajadorId);
            return Results.CreatedAtRoute(GetMatEndpointName, new { id = matDto.Id }, matDto);
        });

        // PUT /mats/1
        group.MapPut("/{id}", async (int id, UpdateMatDto updatedMat, MaterialContext dbContext) =>
        {
            var existingMat = await dbContext.Mats.FindAsync(id);
            if (existingMat is null) return Results.NotFound();

            existingMat.Name = updatedMat.Name;
            existingMat.Unit = updatedMat.Unit;
            existingMat.Quantity = updatedMat.Quantity;
            existingMat.Brand = updatedMat.Brand;
            existingMat.TrabajadorId = updatedMat.TrabajadorId; // <-- Cambiado

            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /mats/1
        group.MapDelete("/{id}", async (int id, MaterialContext dbContext) =>
        {
            await dbContext.Mats.Where(mat => mat.Id == id).ExecuteDeleteAsync();
            return Results.NoContent();
        });

        group.MapPut("/{id}/estado", async (int id, CambiarEstadoDto dto, MaterialContext dbContext) =>
        {
            var mat = await dbContext.Mats.FindAsync(id);
            if (mat is null) return Results.NotFound();

            mat.Estado = dto.Estado; // Actualizamos solo el estado
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}