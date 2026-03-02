using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Obras.Api.Data;
using Obras.Api.Dtos;

namespace Obras.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/users");

        group.MapGet("/", async(MaterialContext dbContext) =>
            await dbContext.Trabajadores
                .Select(t => new { t.Id, t.NombreCompleto })
                .AsNoTracking()
                .ToListAsync()
        );

        // Agregamos IConfiguration para leer la llave secreta
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
    }
}