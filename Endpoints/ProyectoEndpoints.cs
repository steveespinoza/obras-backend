using Microsoft.EntityFrameworkCore;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Endpoints;
public record CreateProyectoDto(string Nombre, string Ubicacion, int? AdminId);
public record UpdateProyectoAdminDto(int? NuevoAdminId);
public static class ProyectoEndpoints
{
    public static void MapProyectoEndpoints(this WebApplication app)
    {
        // El grupo base requiere que estés autenticado, sin importar el rol todavía
        var group = app.MapGroup("/proyectos").RequireAuthorization();

        // GET: Ver proyectos (Jefe y Admin pueden verlos)
        group.MapGet("/", async (MaterialContext dbContext) =>
        {
            var proyectos = await dbContext.Proyectos
                .Include(p => p.Admin) // Traemos la info del Admin
                .Select(p => new { 
                    p.Id, 
                    p.Nombre, 
                    p.Ubicacion, 
                    p.AdminId,
                    // Enviamos el nombre para que el Jefe lo vea claro
                    AdminNombre = p.Admin != null ? $"{p.Admin.Nombre} {p.Admin.Apellido}" : "Pendiente"
                })
                .AsNoTracking()
                .ToListAsync();

            return Results.Ok(proyectos);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Jefe")); // <-- Ambos entran aquí

// POST: Crear nuevo proyecto (¡SOLO EL JEFE PUEDE HACER ESTO!)
        group.MapPost("/", async (CreateProyectoDto dto, MaterialContext dbContext) =>
        {
            var nuevoProyecto = new Proyecto
            {
                Nombre = dto.Nombre,
                Ubicacion = dto.Ubicacion,
                AdminId = dto.AdminId // Acepta null tranquilamente
            };

            dbContext.Proyectos.Add(nuevoProyecto);
            await dbContext.SaveChangesAsync(); 

            // Solo intentamos asignar el trabajador si realmente nos enviaron un AdminId
            if (dto.AdminId.HasValue)
            {
                // ¡NUEVO!: Incluimos la lista de Proyectos al buscar al trabajador
                var adminAsignado = await dbContext.Trabajadores
                    .Include(t => t.Proyectos) 
                    .FirstOrDefaultAsync(t => t.UsuarioAccesoId == dto.AdminId.Value);
                
                if (adminAsignado != null)
                {
                    // ¡NUEVO!: En lugar de igualar el ID, agregamos el objeto completo a su lista
                    adminAsignado.Proyectos.Add(nuevoProyecto);
                    await dbContext.SaveChangesAsync();
                }
            }

            return Results.Ok(new { Message = "Proyecto creado", ProyectoId = nuevoProyecto.Id });
        })
        .RequireAuthorization(policy => policy.RequireRole("Jefe")); // <-- Solo el Jefe entra aquí

        // GET: Ver UN proyecto en detalle (Con su Admin)
        group.MapGet("/{id}", async (int id, MaterialContext dbContext) =>
        {
            var proyecto = await dbContext.Proyectos
                .Include(p => p.Admin)
                .Select(p => new { 
                    p.Id, 
                    p.Nombre, 
                    p.Ubicacion, 
                    p.AdminId,
                    // Separamos Nombre y Apellido para el formulario de edición
                    AdminNombre = p.Admin != null ? p.Admin.Nombre : null,
                    AdminApellido = p.Admin != null ? p.Admin.Apellido : null,
                    AdminNombreCompleto = p.Admin != null ? $"{p.Admin.Nombre} {p.Admin.Apellido}" : null,
                    AdminUsername = p.Admin != null ? p.Admin.Username : null,
                    AdminTelefono = p.Admin != null ? p.Admin.Telefono : null
                })
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto is null) return Results.NotFound();

            return Results.Ok(proyecto);
        })
        .RequireAuthorization(policy => policy.RequireRole("Jefe", "Admin"));

        // 4. PUT: Cambiar el Administrador de un proyecto (SOLO JEFE)
        group.MapPut("/{id}/admin", async (int id, UpdateProyectoAdminDto dto, MaterialContext dbContext) =>
        {
            // Buscamos el proyecto
            var proyecto = await dbContext.Proyectos.FindAsync(id);
            if (proyecto == null) return Results.NotFound(new { Message = "Proyecto no encontrado" });

            var viejoAdminId = proyecto.AdminId;

            // 1. Actualizamos quién es el jefe del proyecto
            proyecto.AdminId = dto.NuevoAdminId;

            // 2. Le quitamos la obra de su lista al Admin antiguo
            if (viejoAdminId.HasValue)
            {
                var viejoTrabajador = await dbContext.Trabajadores
                    .Include(t => t.Proyectos)
                    .FirstOrDefaultAsync(t => t.UsuarioAccesoId == viejoAdminId.Value);
                
                if (viejoTrabajador != null) viejoTrabajador.Proyectos.Remove(proyecto);
            }

            // 3. Le asignamos la obra a la lista del nuevo Admin
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
            return Results.Ok(new { Message = "Administrador actualizado con éxito" });
            
        }).RequireAuthorization(policy => policy.RequireRole("Jefe"));
    }

    
}