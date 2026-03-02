using Microsoft.EntityFrameworkCore;
using Obras.Api.Models;

namespace Obras.Api.Data;

public class MaterialContext(DbContextOptions<MaterialContext> options) : DbContext(options)
{
    public DbSet<Material> Mats => Set<Material>();
    public DbSet<MaterialAlmacen> MaterialesAlmacen => Set<MaterialAlmacen>();
    
    // NUEVAS TABLAS
    public DbSet<UsuarioAcceso> UsuariosAcceso => Set<UsuarioAcceso>();
    public DbSet<Trabajador> Trabajadores => Set<Trabajador>(); 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuramos explícitamente la relación 1 a 1
        modelBuilder.Entity<Trabajador>()
            .HasOne(t => t.UsuarioAcceso)
            .WithOne(u => u.Trabajador)
            .HasForeignKey<Trabajador>(t => t.UsuarioAccesoId);

        base.OnModelCreating(modelBuilder);
    }
}