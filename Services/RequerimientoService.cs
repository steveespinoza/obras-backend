using Microsoft.EntityFrameworkCore;
using Obras.Api.Constants;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Services;

public class RequerimientoService(MaterialContext dbContext) : IRequerimientoService
{
    public async Task<List<RequerimientoSummaryDto>> ObtenerTodosAsync(int proyectoId)
    {
        return await dbContext.Requerimientos
            .Include(r => r.Trabajador)
                .ThenInclude(t => t.UsuarioAcceso)
            .Include(r => r.Detalles)
            .Where(r => r.ProyectoId == proyectoId)
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
    }

    public async Task<RequerimientoDetailsDto?> ObtenerPorIdAsync(int id, int proyectoId)
    {
        var req = await dbContext.Requerimientos
            .Include(r => r.Trabajador)
            .Include(r => r.Detalles)
            .AsNoTracking() // Lo hacemos más rápido porque es solo lectura
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
        var req = await dbContext.Requerimientos
            .Include(r => r.Detalles)
            .FirstOrDefaultAsync(r => r.Id == id && r.ProyectoId == proyectoId);

        if (req is null) return (false, "No encontrado");

        // Regla de negocio
        if (req.Estado != RequerimientoEstados.Pendiente)
            return (false, "Solo se pueden editar pedidos en estado Pendiente.");

        var incomingIds = dto.Detalles.Where(d => d.Id.HasValue).Select(d => d.Id.Value).ToList();
        var detallesAEliminar = req.Detalles.Where(d => !incomingIds.Contains(d.Id)).ToList();
        dbContext.DetallesRequerimiento.RemoveRange(detallesAEliminar);

        foreach (var det in dto.Detalles)
        {
            if (det.Id.HasValue && det.Id.Value > 0)
            {
                var existente = req.Detalles.FirstOrDefault(d => d.Id == det.Id.Value);
                if (existente != null)
                {
                    existente.Name = det.Name; existente.Unit = det.Unit;
                    existente.Quantity = det.Quantity; existente.Brand = det.Brand;
                }
            }
            else
            {
                req.Detalles.Add(new DetalleRequerimiento
                {
                    Name = det.Name, Unit = det.Unit, Quantity = det.Quantity, Brand = det.Brand
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<object> ObtenerReporteAsync(string material, DateTime inicio, DateTime fin, int proyectoId)
    {
        var fechaFinAjustada = fin.Date.AddDays(1);
        return await dbContext.DetallesRequerimiento
            .Include(d => d.Requerimiento)
            .Where(d => 
                d.Requerimiento!.ProyectoId == proyectoId &&
                d.Requerimiento.FechaSolicitud >= inicio.Date &&
                d.Requerimiento.FechaSolicitud < fechaFinAjustada &&
                d.Name.ToLower().Contains(material.ToLower()) &&
                d.Requerimiento.Estado != RequerimientoEstados.Rechazado // <-- Constante
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