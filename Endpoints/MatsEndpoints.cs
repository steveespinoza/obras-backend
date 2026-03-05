using Microsoft.EntityFrameworkCore;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;
using System.Security.Claims;
namespace Obras.Api.Endpoints;

public static class RequerimientosEndpoints
{
    public static void MapRequerimientosEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/requerimientos").RequireAuthorization();

// GET: Obtener lista de pedidos (Solo del proyecto actual)
        group.MapGet("/", async (MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            return await dbContext.Requerimientos
                .Include(r => r.Trabajador)
                    .ThenInclude(t => t.UsuarioAcceso)
                .Include(r => r.Detalles)
                .Where(r => r.ProyectoId == proyectoId) // <-- ¡El filtro maestro!
                .Select(r => new RequerimientoSummaryDto(
                    r.Id,
                    r.FechaSolicitud,
                    r.Estado,
                    r.Trabajador!.NombreCompleto,
                    r.Trabajador.UsuarioAcceso!.Especialidad,
                    r.Detalles.Count
                ))
                .AsNoTracking()
                .ToListAsync();
        });

// GET: Obtener UN pedido con todos sus materiales adentro
        group.MapGet("/{id}", async (int id, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            var req = await dbContext.Requerimientos
                .Include(r => r.Trabajador)
                .Include(r => r.Detalles)
                // ¡NUEVO! Buscamos por ID y validamos que sea de su proyecto
                .FirstOrDefaultAsync(r => r.Id == id && r.ProyectoId == proyectoId); 

            if (req is null) return Results.NotFound();

            var detallesDto = req.Detalles.Select(d => new DetalleLecturaDto(
                d.Id, d.Name, d.Unit, d.Quantity, d.Brand)).ToList();

            return Results.Ok(new RequerimientoDetailsDto(
                req.Id, req.FechaSolicitud, req.Estado, req.Trabajador!.NombreCompleto, detallesDto
            ));
        });

// POST: Crear un nuevo pedido
        group.MapPost("/", async (CreateRequerimientoDto dto, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            var nuevoRequerimiento = new Requerimiento
            {
                TrabajadorId = dto.TrabajadorId,
                FechaSolicitud = DateTime.UtcNow,
                Estado = "Pendiente",
                ProyectoId = proyectoId // <-- Vinculamos el pedido a la obra
            };

            foreach (var det in dto.Detalles)
            {
                nuevoRequerimiento.Detalles.Add(new DetalleRequerimiento
                {
                    Name = det.Name,
                    Unit = det.Unit,
                    Quantity = det.Quantity,
                    Brand = det.Brand
                });
            }

            dbContext.Requerimientos.Add(nuevoRequerimiento);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { Mensaje = "Requerimiento guardado", Id = nuevoRequerimiento.Id });
        });

// PUT: Cambiar estado (Aprobado/Rechazado)
        group.MapPut("/{id}/estado", async (int id, CambiarEstadoRequerimientoDto dto, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            // ¡NUEVO! En lugar de FindAsync, usamos FirstOrDefaultAsync para meter las dos condiciones
            var req = await dbContext.Requerimientos
                .FirstOrDefaultAsync(r => r.Id == id && r.ProyectoId == proyectoId);

            if (req is null) return Results.NotFound();

            req.Estado = dto.Estado;
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

// PUT: Actualizar un pedido completo (Editar el "Carrito")
        group.MapPut("/{id}", async (int id, UpdateRequerimientoDto dto, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            // 1. Buscamos el pedido validando la seguridad
            var req = await dbContext.Requerimientos
                .Include(r => r.Detalles)
                // ¡NUEVO! Validamos ID de pedido y Proyecto
                .FirstOrDefaultAsync(r => r.Id == id && r.ProyectoId == proyectoId);

            if (req is null) return Results.NotFound();

            // 2. Regla de negocio: Solo editar si está Pendiente
            if (req.Estado != "Pendiente") 
                return Results.BadRequest(new { Message = "Solo se pueden editar pedidos en estado Pendiente." });

            // 3. Obtenemos los IDs de los materiales que React nos está enviando
            var incomingIds = dto.Detalles.Where(d => d.Id.HasValue).Select(d => d.Id.Value).ToList();

            // 4. BORRAR: Si un material estaba en la BD, pero ya no viene de React, lo borramos
            var detallesAEliminar = req.Detalles.Where(d => !incomingIds.Contains(d.Id)).ToList();
            dbContext.DetallesRequerimiento.RemoveRange(detallesAEliminar);

            // 5. ACTUALIZAR o AGREGAR
            foreach (var det in dto.Detalles)
            {
                if (det.Id.HasValue && det.Id.Value > 0)
                {
                    var existente = req.Detalles.FirstOrDefault(d => d.Id == det.Id.Value);
                    if (existente != null)
                    {
                        existente.Name = det.Name;
                        existente.Unit = det.Unit;
                        existente.Quantity = det.Quantity;
                        existente.Brand = det.Brand;
                    }
                }
                else
                {
                    req.Detalles.Add(new DetalleRequerimiento
                    {
                        Name = det.Name,
                        Unit = det.Unit,
                        Quantity = det.Quantity,
                        Brand = det.Brand
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });


// 5.5 GET: Reporte de suma de materiales por rango de fechas
        group.MapGet("/reporte", async (string material, DateTime inicio, DateTime fin, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
            var fechaFinAjustada = fin.Date.AddDays(1);

            var reporte = await dbContext.DetallesRequerimiento
                .Include(d => d.Requerimiento)
                .Where(d => 
                    d.Requerimiento!.ProyectoId == proyectoId && // <-- ¡NUEVO FILTRO MAESTRO!
                    d.Requerimiento.FechaSolicitud >= inicio.Date &&
                    d.Requerimiento.FechaSolicitud < fechaFinAjustada &&
                    d.Name.ToLower().Contains(material.ToLower()) &&
                    d.Requerimiento.Estado != "Rechazado" 
                )
                .GroupBy(d => new { d.Name, d.Unit }) 
                .Select(g => new {
                    Material = g.Key.Name,
                    Unidad = g.Key.Unit,
                    TotalSolicitado = g.Sum(x => x.Quantity)
                })
                .ToListAsync();

            return Results.Ok(reporte);
        });

// DELETE: Borrar un pedido completo (y sus materiales por cascada)
        group.MapDelete("/{id}", async (int id, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            int proyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);

            // ¡NUEVO! Ejecutamos el Delete validando el ProyectoId para que nadie borre lo ajeno
            var borrados = await dbContext.Requerimientos
                .Where(r => r.Id == id && r.ProyectoId == proyectoId)
                .ExecuteDeleteAsync();

            if (borrados == 0) return Results.NotFound();

            return Results.NoContent();
        });
    }
}