using Microsoft.EntityFrameworkCore;
using Obras.Api.Models;

namespace Obras.Api.Data;

public class MaterialContext(DbContextOptions<MaterialContext> options) : DbContext(options)
{
    // Reemplazamos public DbSet<Material> Mats por:
    public DbSet<Requerimiento> Requerimientos => Set<Requerimiento>();
    public DbSet<DetalleRequerimiento> DetallesRequerimiento => Set<DetalleRequerimiento>();
    
    public DbSet<MaterialAlmacen> MaterialesAlmacen => Set<MaterialAlmacen>();
    public DbSet<UsuarioAcceso> UsuariosAcceso => Set<UsuarioAcceso>();
    public DbSet<Trabajador> Trabajadores => Set<Trabajador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trabajador>()
            .HasOne(t => t.UsuarioAcceso)
            .WithOne(u => u.Trabajador)
            .HasForeignKey<Trabajador>(t => t.UsuarioAccesoId);

        // Configurar relación 1 a Muchos y borrado en cascada
        modelBuilder.Entity<Requerimiento>()
            .HasMany(r => r.Detalles)
            .WithOne(d => d.Requerimiento)
            .HasForeignKey(d => d.RequerimientoId)
            .OnDelete(DeleteBehavior.Cascade); // Si borras el pedido, se borra el detalle

        base.OnModelCreating(modelBuilder);
    }
}