using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/users");

        // GET: Obtener lista COMPLETA de todos los usuarios registrados (Para la tabla del Admin)
        group.MapGet("/", async (MaterialContext dbContext) =>
        {
            var usuarios = await dbContext.UsuariosAcceso
                .Include(u => u.Trabajador) // Traemos la info cruzada del trabajador
                .Select(u => new {
                    u.Id,
                    NombreCompleto = u.Trabajador != null ? u.Trabajador.NombreCompleto : $"{u.Nombre} {u.Apellido}",
                    u.Username,
                    u.Especialidad,
                    u.Telefono
                })
                .AsNoTracking()
                .ToListAsync();
                
            return Results.Ok(usuarios);
        });

        // POST: Login
        group.MapPost("/login", async (LoginDto loginInfo, MaterialContext dbContext, IConfiguration config) =>
        {
            var acceso = await dbContext.UsuariosAcceso
                .Include(u => u.Trabajador)
                .FirstOrDefaultAsync(u => u.Username == loginInfo.Username && u.Password == loginInfo.Password);

            if (acceso is null || acceso.Trabajador is null)
            {
                return Results.Unauthorized();
            }

            // 1. Crear los "Claims" (Datos dentro del token)
            var isAdmin = acceso.Especialidad == "Administración";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, acceso.Trabajador.Id.ToString()),
                new Claim(ClaimTypes.Name, acceso.Trabajador.NombreCompleto),
                new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User") // Rol para permisos futuros
            };

            // 2. Configurar la firma del token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Crear el token (Válido por 8 horas)
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // 4. Devolver los datos + EL TOKEN
            return Results.Ok(new 
            { 
                Id = acceso.Trabajador.Id, 
                Name = acceso.Trabajador.NombreCompleto,
                IsAdmin = isAdmin,
                Token = tokenString // <-- ¡Aquí enviamos el pase VIP!
            });
        });

        // POST: Registro de nuevos usuarios
        group.MapPost("/registro", async (CreateUserDto dto, MaterialContext dbContext) =>
        {
            // 1. Verificamos que el username no esté repetido
            if (await dbContext.UsuariosAcceso.AnyAsync(u => u.Username == dto.Username))
            {
                return Results.BadRequest(new { Message = "El nombre de usuario ya está en uso. Elige otro." });
            }

            // 2. Creamos los datos de acceso
            var nuevoAcceso = new UsuarioAcceso 
            { 
                Nombre = dto.Nombre, 
                Apellido = dto.Apellido, 
                Username = dto.Username, 
                Password = dto.Password, // Nota: En un entorno real avanzado, esto se encriptaría.
                Especialidad = dto.Especialidad, 
                Telefono = dto.Telefono 
            };

            // 3. Creamos su perfil de trabajador asociado
            var nuevoTrabajador = new Trabajador 
            { 
                NombreCompleto = $"{dto.Nombre} {dto.Apellido}", 
                UsuarioAcceso = nuevoAcceso 
            };

            // 4. Guardamos en la BD
            dbContext.Set<UsuarioAcceso>().Add(nuevoAcceso);
            dbContext.Set<Trabajador>().Add(nuevoTrabajador);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { Message = "Usuario y Trabajador creados con éxito." });
        });
    }
}