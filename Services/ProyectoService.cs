using Microsoft.EntityFrameworkCore;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Services;

public class ProyectoService(MaterialContext dbContext) : IProyectoService
{
    public async Task<List<ProyectoSummaryDto>> ObtenerTodosAsync()
    {
        return await dbContext.Proyectos
            .Include(p => p.Admin)
            .Select(p => new ProyectoSummaryDto(
                p.Id, 
                p.Nombre, 
                p.Ubicacion, 
                p.AdminId,
                p.Admin != null ? $"{p.Admin.Nombre} {p.Admin.Apellido}" : "Pendiente"
            ))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ProyectoDetailDto?> ObtenerPorIdAsync(int id)
    {
        // 1. Buscamos el proyecto en la BD sin intentar mapearlo a SQL
        var p = await dbContext.Proyectos
            .Include(x => x.Admin)
            .FirstOrDefaultAsync(x => x.Id == id);

        // 2. Si no existe, salimos
        if (p == null) return null;

        // 3. Construimos el DTO tranquilamente en memoria (C# puro)
        return new ProyectoDetailDto(
            p.Id, 
            p.Nombre, 
            p.Ubicacion, 
            p.AdminId,
            p.Admin?.Nombre, // Usamos el operador seguro '?'
            p.Admin?.Apellido,
            p.Admin != null ? $"{p.Admin.Nombre} {p.Admin.Apellido}" : null,
            p.Admin?.Username,
            p.Admin?.Telefono
        );
    }

    public async Task<int> CrearAsync(CreateProyectoDto dto)
    {
        var nuevoProyecto = new Proyecto
        {
            Nombre = dto.Nombre,
            Ubicacion = dto.Ubicacion,
            AdminId = dto.AdminId 
        };

        dbContext.Proyectos.Add(nuevoProyecto);
        await dbContext.SaveChangesAsync(); 

        // Lógica de asignación al trabajador
        if (dto.AdminId.HasValue)
        {
            var adminAsignado = await dbContext.Trabajadores
                .Include(t => t.Proyectos) 
                .FirstOrDefaultAsync(t => t.UsuarioAccesoId == dto.AdminId.Value);
                
            if (adminAsignado != null)
            {
                adminAsignado.Proyectos.Add(nuevoProyecto);
                await dbContext.SaveChangesAsync();
            }
        }

        return nuevoProyecto.Id;
    }

    public async Task<bool> ActualizarAdminAsync(int id, UpdateProyectoAdminDto dto)
    {
        var proyecto = await dbContext.Proyectos.FindAsync(id);
        if (proyecto == null) return false;

        var viejoAdminId = proyecto.AdminId;
        proyecto.AdminId = dto.NuevoAdminId;

        // Le quitamos la obra de su lista al Admin antiguo
        if (viejoAdminId.HasValue)
        {
            var viejoTrabajador = await dbContext.Trabajadores
                .Include(t => t.Proyectos)
                .FirstOrDefaultAsync(t => t.UsuarioAccesoId == viejoAdminId.Value);
                
            if (viejoTrabajador != null) viejoTrabajador.Proyectos.Remove(proyecto);
        }

        // Le asignamos la obra a la lista del nuevo Admin
        if (dto.NuevoAdminId.HasValue)
        {
            var nuevoTrabajador = await dbContext.Trabajadores
                .Include(t => t.Proyectos)
                .FirstOrDefaultAsync(t => t.UsuarioAccesoId == dto.NuevoAdminId.Value);
                
            if (nuevoTrabajador != null && !nuevoTrabajador.Proyectos.Any(p => p.Id == proyecto.Id))
            {
                nuevoTrabajador.Proyectos.Add(proyecto);
            }
        }

        await dbContext.SaveChangesAsync();
        return true;
    }
}