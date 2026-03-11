using Microsoft.EntityFrameworkCore;
using Obras.Api.Constants;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Services;

// DESPUÉS
public class RequerimientoService(MaterialContext dbContext, ILogger<RequerimientoService> logger) : IRequerimientoService
{
    public async Task<PaginacionDto<RequerimientoSummaryDto>> ObtenerTodosAsync(int proyectoId, int pagina, int cantidad, string rol, int trabajadorId)
    {
        // 1. Filtramos por la obra
        var queryBase = dbContext.Requerimientos
            .Where(r => r.ProyectoId == proyectoId);

        // 2. ¡EL FILTRO DE SEGURIDAD! Si es operario, solo ve sus propios pedidos
        if (rol == AppRoles.User)
        {
            queryBase = queryBase.Where(r => r.TrabajadorId == trabajadorId);
        }

        int totalItems = await queryBase.CountAsync();
        int totalPaginas = totalItems > 0 ? (int)Math.Ceiling(totalItems / (double)cantidad) : 1;

        var items = await queryBase
            .OrderByDescending(r => r.FechaSolicitud)
            .Skip((pagina - 1) * cantidad)
            .Take(cantidad)
            .Include(r => r.Trabajador)
                .ThenInclude(t => t.UsuarioAcceso)
            .Include(r => r.Detalles)
            .Select(r => new RequerimientoSummaryDto(
                r.Id,
                r.FechaSolicitud,
                r.Estado,
                r.Trabajador!.NombreCompleto,
                r.Trabajador.UsuarioAcceso!.Especialidad,
                r.Detalles.Count
            ))
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        return new PaginacionDto<RequerimientoSummaryDto>(items, totalItems, pagina, totalPaginas);
    }

    public async Task<RequerimientoDetailsDto?> ObtenerPorIdAsync(int id, int proyectoId)
    {
        var req = await dbContext.Requerimientos
            .Include(r => r.Trabajador)
            .Include(r => r.Detalles)
            .AsNoTracking() // Lo hacemos más rápido porque es solo lectura
            .AsSplitQuery() // <--- 🚀 NUEVO: Evita la explosión cartesiana 
            .FirstOrDefaultAsync(r => r.Id == id && r.ProyectoId == proyectoId);

        if (req is null) return null;

        var detallesDto = req.Detalles.Select(d => new DetalleLecturaDto(
            d.Id, d.Name, d.Unit, d.Quantity, d.Brand)).ToList();

        return new RequerimientoDetailsDto(
            req.Id, req.FechaSolicitud, req.Estado, req.Trabajador!.NombreCompleto, detallesDto
        );
    }

    public async Task<int> CrearAsync(CreateRequerimientoDto dto, int proyectoId)
    {
        var nuevoRequerimiento = new Requerimiento
        {
            TrabajadorId = dto.TrabajadorId,
            FechaSolicitud = DateTime.UtcNow,
            Estado = RequerimientoEstados.Pendiente, // <-- Usamos la constante
            ProyectoId = proyectoId
        };

        foreach (var det in dto.Detalles)
        {
            nuevoRequerimiento.Detalles.Add(new DetalleRequerimiento
            {
                Name = det.Name, Unit = det.Unit, Quantity = det.Quantity, Brand = det.Brand
            });
        }

        dbContext.Requerimientos.Add(nuevoRequerimiento);
        await dbContext.SaveChangesAsync();
        return nuevoRequerimiento.Id;
    }

    public async Task<bool> CambiarEstadoAsync(int id, string estado, int proyectoId)
    {
        var req = await dbContext.Requerimientos
            .FirstOrDefaultAsync(r => r.Id == id && r.ProyectoId == proyectoId);

        if (req is null) return false;

        req.Estado = estado;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Exito, string MensajeError)> ActualizarCompletoAsync(int id, UpdateRequerimientoDto dto, int proyectoId)
    {
        // Buscamos el requerimiento
        var req = await dbContext.Requerimientos
            .Include(r => r.Detalles)
            .FirstOrDefaultAsync(r => r.Id == id && r.ProyectoId == proyectoId);

        if (req is null) return (false, "No encontrado");

        // Regla de negocio
        if (req.Estado != RequerimientoEstados.Pendiente)
            return (false, "Solo se pueden editar pedidos en estado Pendiente.");

        // INICIAMOS LA TRANSACCIÓN
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // 1. Identificar y eliminar detalles que ya no vienen desde React
            var incomingIds = dto.Detalles.Where(d => d.Id.HasValue).Select(d => d.Id.Value).ToList();
            var detallesAEliminar = req.Detalles.Where(d => !incomingIds.Contains(d.Id)).ToList();
            
            if (detallesAEliminar.Any())
            {
                dbContext.DetallesRequerimiento.RemoveRange(detallesAEliminar);
            }

            // 2. Actualizar existentes o agregar nuevos
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

            // 3. Guardar los cambios en la base de datos
            await dbContext.SaveChangesAsync();

            // 4. SI TODO SALIÓ BIEN, CONFIRMAMOS LA TRANSACCIÓN
            await transaction.CommitAsync();
            
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            // SI ALGO FALLA, REVERTIMOS TODO
            await transaction.RollbackAsync();
            
            // Registramos el error de forma estructurada para facilitar la depuración
            logger.LogError(ex, "🔥 Error crítico al actualizar el requerimiento con ID: {RequerimientoId} en el proyecto: {ProyectoId}", id, proyectoId);
            
            return (false, "Ocurrió un error inesperado al actualizar el requerimiento. Los cambios han sido descartados.");
        }
    }

    public async Task<object> ObtenerReporteAsync(string material, DateTime inicio, DateTime fin, int proyectoId)
    {
        // 1. SOLUCIÓN POSTGRESQL: Forzamos a que .NET trate las fechas entrantes como UTC
        var inicioUtc = DateTime.SpecifyKind(inicio.Date, DateTimeKind.Utc);
        var finUtc = DateTime.SpecifyKind(fin.Date, DateTimeKind.Utc).AddDays(1); // Sumamos 1 día para incluir todo el último día

        // 2. Pasamos el material a minúsculas en memoria para que la consulta SQL sea más limpia
        var materialBuscado = material.ToLower();

        return await dbContext.DetallesRequerimiento
            .Include(d => d.Requerimiento)
            .Where(d => 
                d.Requerimiento!.ProyectoId == proyectoId &&
                d.Requerimiento.FechaSolicitud >= inicioUtc && // Comparamos UTC contra UTC
                d.Requerimiento.FechaSolicitud < finUtc &&
                d.Name.ToLower().Contains(materialBuscado) &&
                d.Requerimiento.Estado != RequerimientoEstados.Rechazado 
            )
            .GroupBy(d => new { d.Name, d.Unit }) 
            .Select(g => new {
                Material = g.Key.Name,
                Unidad = g.Key.Unit,
                TotalSolicitado = g.Sum(x => x.Quantity)
            })
            .ToListAsync();
    }

    public async Task<bool> EliminarAsync(int id, int proyectoId)
    {
        var borrados = await dbContext.Requerimientos
            .Where(r => r.Id == id && r.ProyectoId == proyectoId)
            .ExecuteDeleteAsync();
        return borrados > 0;
    }
}