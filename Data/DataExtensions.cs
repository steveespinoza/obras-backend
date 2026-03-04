using Microsoft.EntityFrameworkCore;
using Obras.Api.Models;

namespace Obras.Api.Data;

public static class DataExtensions
{
    public static void MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider
                    .GetRequiredService<MaterialContext>();
            dbContext.Database.Migrate();
    }
    
    public static void AddMatDb(this WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("Material");
        builder.Services.AddSqlite<MaterialContext>(connString,
        optionsAction: options => options.UseSeeding((context, _) =>
        {
            // 1. Sembrar Usuarios (Esto ya lo tenías)
            // 1. Sembrar Usuarios
            if (!context.Set<UsuarioAcceso>().Any())
            {
                // Generamos el hash para "123" que usarán ambos
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("123");

                var adminAcceso = new UsuarioAcceso { Nombre = "Steve", Apellido = "Jobs", Username = "steveAdmin", Password = passwordHash, Especialidad = "Administración", Telefono = "999999999" };
                var adminTrabajador = new Trabajador { NombreCompleto = $"{adminAcceso.Nombre} {adminAcceso.Apellido}", UsuarioAcceso = adminAcceso };

                var userAcceso = new UsuarioAcceso { Nombre = "Pedro", Apellido = "Perez", Username = "pedroObras", Password = passwordHash, Especialidad = "Construcción", Telefono = "888888888" };
                var userTrabajador = new Trabajador { NombreCompleto = $"{userAcceso.Nombre} {userAcceso.Apellido}", UsuarioAcceso = userAcceso };

                context.Set<Trabajador>().AddRange(adminTrabajador, userTrabajador);
                context.SaveChanges();
            }

            // 2. Sembrar Catálogo de Almacén (NUEVO)
            if (!context.Set<MaterialAlmacen>().Any())
            {
                var materialesBase = new List<MaterialAlmacen>
                {
                    new MaterialAlmacen { Name = "Cemento Sol" },
                    new MaterialAlmacen { Name = "Cemento Andino" },
                    new MaterialAlmacen { Name = "Arena Fina" },
                    new MaterialAlmacen { Name = "Arena Gruesa" },
                    new MaterialAlmacen { Name = "Piedra Chancada 1/2" },
                    new MaterialAlmacen { Name = "Ladrillo King Kong 18 huecos" },
                    new MaterialAlmacen { Name = "Ladrillo Pandereta" },
                    new MaterialAlmacen { Name = "Acero Corrugado 1/2\"" },
                    new MaterialAlmacen { Name = "Acero Corrugado 3/8\"" },
                    new MaterialAlmacen { Name = "Alambre Negro #16" },
                    new MaterialAlmacen { Name = "Alambre Negro #8" },
                    new MaterialAlmacen { Name = "Clavos para madera 2.5\"" },
                    new MaterialAlmacen { Name = "Clavos para madera 3\"" },
                    new MaterialAlmacen { Name = "Yeso de construcción" },
                    new MaterialAlmacen { Name = "Pegamento Chema" },
                    new MaterialAlmacen { Name = "Tubo PVC 2\"" }
                };

                context.Set<MaterialAlmacen>().AddRange(materialesBase);
                context.SaveChanges();
            }
        }));    
    }
}