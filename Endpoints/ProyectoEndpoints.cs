using Obras.Api.Constants; // Importamos nuestras constantes seguras
using Obras.Api.Dtos;
using Obras.Api.Services;  // Importamos el servicio

namespace Obras.Api.Endpoints;

public static class ProyectoEndpoints
{
    public static void MapProyectoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/proyectos").RequireAuthorization();

        // GET: Ver proyectos
        group.MapGet("/", async (IProyectoService proyectoService) =>
        {
            var proyectos = await proyectoService.ObtenerTodosAsync();
            return Results.Ok(proyectos);
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Jefe)); 

        // POST: Crear nuevo proyecto 
        group.MapPost("/", async (CreateProyectoDto dto, IProyectoService proyectoService) =>
        {
            var proyectoId = await proyectoService.CrearAsync(dto);
            return Results.Ok(new { Message = "Proyecto creado", ProyectoId = proyectoId });
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Jefe)); 

        // GET: Ver UN proyecto en detalle 
        group.MapGet("/{id}", async (int id, IProyectoService proyectoService) =>
        {
            var proyecto = await proyectoService.ObtenerPorIdAsync(id);
            return proyecto is not null ? Results.Ok(proyecto) : Results.NotFound();
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Jefe, AppRoles.Admin));

        // PUT: Cambiar el Administrador de un proyecto 
        group.MapPut("/{id}/admin", async (int id, UpdateProyectoAdminDto dto, IProyectoService proyectoService) =>
        {
            var exito = await proyectoService.ActualizarAdminAsync(id, dto);
            
            return exito 
                ? Results.Ok(new { Message = "Administrador actualizado con éxito" }) 
                : Results.NotFound(new { Message = "Proyecto no encontrado" });
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Jefe));
    }
}