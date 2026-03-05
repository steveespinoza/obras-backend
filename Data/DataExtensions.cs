using Microsoft.EntityFrameworkCore;
using Obras.Api.Models;
using System.Collections.Generic;
using System.Linq;

namespace Obras.Api.Data;

public static class DataExtensions
{
    public static void MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MaterialContext>();
        dbContext.Database.Migrate();
    }
    
    public static void AddMatDb(this WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("Material");
        builder.Services.AddSqlite<MaterialContext>(connString,
        optionsAction: options => options.UseSeeding((context, _) =>
        {
            // ==========================================
            // 1. SEMBRAR AL SUPERUSUARIO (JEFE)
            // ==========================================
            if (!context.Set<Jefe>().Any())
            {
                var jefePassword = BCrypt.Net.BCrypt.HashPassword("jefe123"); 
                var superJefe = new Jefe 
                { 
                    Nombre = "Gerencia", 
                    Apellido = "General", 
                    Username = "superjefe", 
                    Password = jefePassword 
                };
                context.Set<Jefe>().Add(superJefe);
                context.SaveChanges();
            }

            // ==========================================
            // 2. SEMBRAR PROYECTOS Y TRABAJADORES
            // ==========================================
            if (!context.Set<Proyecto>().Any())
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("123");

                // --- A. CREAR 2 PROYECTOS (Sin Admin Asignado) ---
                var proyecto1 = new Proyecto { Nombre = "Residencial Las Palmas", Ubicacion = "Av. Javier Prado 1234", AdminId = null };
                var proyecto2 = new Proyecto { Nombre = "Edificio El Bosque", Ubicacion = "Surco, Lima", AdminId = null };
                
                context.Set<Proyecto>().AddRange(proyecto1, proyecto2);
                context.SaveChanges();

                // --- B. CREAR 2 ADMINISTRADORES (Sin Proyectos) ---
                var adminsAcceso = new List<UsuarioAcceso>
                {
                    new UsuarioAcceso { Nombre = "Steve", Apellido = "Jobs", Username = "admin_steve", Password = passwordHash, Especialidad = "Administración", Telefono = "999000111" },
                    new UsuarioAcceso { Nombre = "Bill", Apellido = "Gates", Username = "admin_bill", Password = passwordHash, Especialidad = "Administración", Telefono = "999000222" }
                };
                context.Set<UsuarioAcceso>().AddRange(adminsAcceso);
                context.SaveChanges();

                // Convertirlos a Trabajador sin agregarles obras a su lista de Proyectos
                var adminsTrabajador = adminsAcceso.Select(a => new Trabajador
                {
                    NombreCompleto = $"{a.Nombre} {a.Apellido}",
                    UsuarioAccesoId = a.Id
                }).ToList();
                context.Set<Trabajador>().AddRange(adminsTrabajador);
                context.SaveChanges();

                // --- C. CREAR 10 TRABAJADORES DE CONSTRUCCIÓN ---
                var nombres = new[] { "Carlos", "Luis", "Ana", "Jorge", "Marta", "Pedro", "Sofía", "Diego", "Lucía", "Raúl" };
                var apellidos = new[] { "Pérez", "Gómez", "Ruiz", "Díaz", "Vega", "Soto", "Luna", "Cruz", "Mora", "Ríos" };
                
                var obrerosAcceso = new List<UsuarioAcceso>();
                for (int i = 0; i < 10; i++)
                {
                    obrerosAcceso.Add(new UsuarioAcceso 
                    { 
                        Nombre = nombres[i], 
                        Apellido = apellidos[i], 
                        Username = $"obrero{i+1}", 
                        Password = passwordHash, 
                        Especialidad = "Construcción", 
                        Telefono = $"8880000{i:D2}" 
                    });
                }
                context.Set<UsuarioAcceso>().AddRange(obrerosAcceso);
                context.SaveChanges();

                // Convertirlos a Trabajador y asignar 5 al Proyecto 1, y 5 al Proyecto 2
                var obrerosTrabajador = new List<Trabajador>();
                for (int i = 0; i < 10; i++)
                {
                    var trabajador = new Trabajador
                    {
                        NombreCompleto = $"{obrerosAcceso[i].Nombre} {obrerosAcceso[i].Apellido}",
                        UsuarioAccesoId = obrerosAcceso[i].Id,
                        Proyectos = { i < 5 ? proyecto1 : proyecto2 }
                    };
                    obrerosTrabajador.Add(trabajador);
                }
                context.Set<Trabajador>().AddRange(obrerosTrabajador);
                context.SaveChanges();

                // --- D. SEMBRAR MATERIALES BÁSICOS ---
                var materialesBase = new List<MaterialAlmacen>
                {
                    new MaterialAlmacen { Name = "Cemento Sol", ProyectoId = proyecto1.Id },
                    new MaterialAlmacen { Name = "Arena Fina", ProyectoId = proyecto1.Id },
                    new MaterialAlmacen { Name = "Acero Corrugado 1/2\"", ProyectoId = proyecto1.Id },
                    new MaterialAlmacen { Name = "Ladrillo King Kong", ProyectoId = proyecto2.Id },
                    new MaterialAlmacen { Name = "Pintura Blanca 4L", ProyectoId = proyecto2.Id }
                };
                context.Set<MaterialAlmacen>().AddRange(materialesBase);
                context.SaveChanges();
            }
        }));    
    }
}