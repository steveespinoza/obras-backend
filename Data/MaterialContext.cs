using Microsoft.EntityFrameworkCore;
using Obras.Api.Models;

namespace Obras.Api.Data;

public class MaterialContext(DbContextOptions<MaterialContext> options) : DbContext(options)
{
    public DbSet<Requerimiento> Requerimientos => Set<Requerimiento>();
    public DbSet<DetalleRequerimiento> DetallesRequerimiento => Set<DetalleRequerimiento>();
    public DbSet<MaterialAlmacen> MaterialesAlmacen => Set<MaterialAlmacen>();
    public DbSet<UsuarioAcceso> UsuariosAcceso => Set<UsuarioAcceso>();
    public DbSet<Trabajador> Trabajadores => Set<Trabajador>();
    
    public DbSet<Jefe> Jefes => Set<Jefe>();
    // ¡NUEVO!
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Relación Trabajador - UsuarioAcceso
        modelBuilder.Entity<Trabajador>()
            .HasOne(t => t.UsuarioAcceso)
            .WithOne(u => u.Trabajador)
            .HasForeignKey<Trabajador>(t => t.UsuarioAccesoId);

        // 2. Relación Requerimiento - Detalles (Cascada)
        modelBuilder.Entity<Requerimiento>()
            .HasMany(r => r.Detalles)
            .WithOne(d => d.Requerimiento)
            .HasForeignKey(d => d.RequerimientoId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- ¡NUEVAS RELACIONES DEL PROYECTO! ---

        // Un Proyecto tiene un Admin (UsuarioAcceso)
        modelBuilder.Entity<Proyecto>()
            .HasOne(p => p.Admin)
            .WithMany() // Un admin podría tener varios proyectos, o no poner nada aquí
            .HasForeignKey(p => p.AdminId)
            .OnDelete(DeleteBehavior.Restrict); // Evita borrar al admin si el proyecto existe

// Reemplaza la configuración antigua del trabajador por esta:
        modelBuilder.Entity<Trabajador>()
            .HasMany(t => t.Proyectos)
            .WithMany(p => p.Trabajadores)
            .UsingEntity(j => j.ToTable("TrabajadorProyecto")); // EF Core creará la tabla intermedia automáticamente

        // Un Proyecto tiene su propio catálogo de Materiales
        modelBuilder.Entity<MaterialAlmacen>()
            .HasOne(m => m.Proyecto)
            .WithMany(p => p.CatalogoAlmacen)
            .HasForeignKey(m => m.ProyectoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Un Proyecto tiene muchos Requerimientos
        modelBuilder.Entity<Requerimiento>()
            .HasOne(r => r.Proyecto)
            .WithMany(p => p.Requerimientos)
            .HasForeignKey(r => r.ProyectoId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}