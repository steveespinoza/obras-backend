using Microsoft.EntityFrameworkCore;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Services;

public class AlmacenService(MaterialContext dbContext) : IAlmacenService
{
    public async Task<List<MaterialAlmacenDto>> ObtenerMaterialesPorProyectoAsync(int proyectoId)
    {
        return await dbContext.MaterialesAlmacen
            .Where(m => m.ProyectoId == proyectoId)
            .Select(m => new MaterialAlmacenDto(m.Id, m.Name))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<MaterialAlmacenDto> AgregarMaterialAsync(CreateMaterialAlmacenDto dto, int proyectoId)
    {
        var item = new MaterialAlmacen 
        { 
            Name = dto.Name,
            ProyectoId = proyectoId 
        };
            
        dbContext.MaterialesAlmacen.Add(item);
        await dbContext.SaveChangesAsync();

        return new MaterialAlmacenDto(item.Id, item.Name);
    }

    public async Task<bool> EliminarMaterialAsync(int id, int proyectoId)
    {
        var filasBorradas = await dbContext.MaterialesAlmacen
            .Where(m => m.Id == id && m.ProyectoId == proyectoId)
            .ExecuteDeleteAsync();
            
        return filasBorradas > 0;
    }
}