
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
            if (!context.Set<UsuarioAcceso>().Any())
            {
                // 1. Creamos al Admin
                var adminAcceso = new UsuarioAcceso { Nombre = "Steve", Apellido = "Jobs", Username = "steveAdmin", Password = "123", Especialidad = "Administración", Telefono = "999999999" };
                var adminTrabajador = new Trabajador { NombreCompleto = $"{adminAcceso.Nombre} {adminAcceso.Apellido}", UsuarioAcceso = adminAcceso };

                // 2. Creamos a un Trabajador normal
                var userAcceso = new UsuarioAcceso { Nombre = "Pedro", Apellido = "Perez", Username = "pedroObras", Password = "123", Especialidad = "Construcción", Telefono = "888888888" };
                var userTrabajador = new Trabajador { NombreCompleto = $"{userAcceso.Nombre} {userAcceso.Apellido}", UsuarioAcceso = userAcceso };

                // Al guardar el Trabajador, EF Core guarda automáticamente el UsuarioAcceso enlazado
                context.Set<Trabajador>().AddRange(adminTrabajador, userTrabajador);
                context.SaveChanges();
            }
        }));    

    }
}
