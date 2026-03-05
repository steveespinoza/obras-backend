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

        // GET: Obtener lista de usuarios (Jefe ve todos, Admin ve solo los de su obra)
        group.MapGet("/", async (MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            var rol = user.FindFirst(ClaimTypes.Role)?.Value;
            var miProyectoId = int.Parse(user.FindFirst("ProyectoId")?.Value ?? "0");

            var query = dbContext.UsuariosAcceso
                .Include(u => u.Trabajador) 
                    .ThenInclude(t => t.Proyectos) // Traemos los proyectos
                .AsQueryable();

            // Lógica de seguridad: El Admin solo ve a su gente
            if (rol == "Admin")
            {
                query = query.Where(u => u.Trabajador != null && u.Trabajador.Proyectos.Any(p => p.Id == miProyectoId));
            }

            var usuarios = await query.Select(u => new {
                    u.Id,
                    NombreCompleto = u.Trabajador != null ? u.Trabajador.NombreCompleto : $"{u.Nombre} {u.Apellido}",
                    u.Username,
                    u.Especialidad,
                    u.Telefono,
                    // Mostramos todos los proyectos en los que está separado por comas
                    ProyectoNombre = (u.Trabajador != null && u.Trabajador.Proyectos.Any()) 
                                        ? string.Join(", ", u.Trabajador.Proyectos.Select(p => p.Nombre))
                                        : "Sin proyecto"
                })
                .AsNoTracking()
                .ToListAsync();
                
            return Results.Ok(usuarios);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Jefe"));

        // POST: Login 
        group.MapPost("/login", async (LoginDto loginInfo, MaterialContext dbContext, IConfiguration config) =>
        {
            // PUERTA 1: Jefe
            var jefe = await dbContext.Jefes.FirstOrDefaultAsync(j => j.Username == loginInfo.Username);
            if (jefe != null && BCrypt.Net.BCrypt.Verify(loginInfo.Password, jefe.Password))
            {
                var claimsJefe = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, $"J-{jefe.Id}"),
                    new Claim(ClaimTypes.Name, $"{jefe.Nombre} {jefe.Apellido}"),
                    new Claim(ClaimTypes.Role, "Jefe")
                };
                return Results.Ok(new { Id = jefe.Id, Name = $"{jefe.Nombre} {jefe.Apellido}", Role = "Jefe", ProyectoId = (int?)null, Token = GenerarToken(claimsJefe, config) });
            }

            // PUERTA 2: Trabajadores y Admins
            var acceso = await dbContext.UsuariosAcceso
                .Include(u => u.Trabajador)
                    .ThenInclude(t => t.Proyectos) // ¡CLAVE! Traemos sus proyectos
                .FirstOrDefaultAsync(u => u.Username == loginInfo.Username);

            if (acceso is null || acceso.Trabajador is null || !BCrypt.Net.BCrypt.Verify(loginInfo.Password, acceso.Password))
                return Results.Unauthorized(); 

            var isAdmin = acceso.Especialidad == "Administración";
            
            // Si está en varios, tomamos el primero por defecto para que inicie sesión
            var proyectoActivoId = acceso.Trabajador.Proyectos.FirstOrDefault()?.Id;

            var claimsUsuario = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, acceso.Trabajador.Id.ToString()),
                new Claim(ClaimTypes.Name, acceso.Trabajador.NombreCompleto),
                new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User"),
                new Claim("ProyectoId", proyectoActivoId?.ToString() ?? "0") 
            };

            return Results.Ok(new { Id = acceso.Trabajador.Id, Name = acceso.Trabajador.NombreCompleto, Role = isAdmin ? "Admin" : "User", ProyectoId = proyectoActivoId, Token = GenerarToken(claimsUsuario, config) });
        });

// POST: Registro 
        group.MapPost("/registro", async (CreateUserDto dto, MaterialContext dbContext, ClaimsPrincipal user) =>
        {
            var rolCreador = user.FindFirst(ClaimTypes.Role)?.Value;

            // REGLA 1: Un Admin no crea Admins
            if (rolCreador == "Admin" && dto.Especialidad == "Administración")
            {
                return Results.BadRequest(new { Message = "Acceso denegado: Un Administrador de obra no puede crear a otro Administrador." });
            }

            // REGLA 2: ¡NUEVA! El Jefe SOLO crea Admins
            if (rolCreador == "Jefe" && dto.Especialidad != "Administración")
            {
                return Results.BadRequest(new { Message = "Acceso denegado: El Jefe supremo solo debe registrar perfiles de Administración." });
            }

            // ... (resto del código)

            // 2. Verificamos que el usuario no exista ya
            if (await dbContext.UsuariosAcceso.AnyAsync(u => u.Username == dto.Username))
                return Results.BadRequest(new { Message = "El nombre de usuario ya está en uso." });

            var nuevoAcceso = new UsuarioAcceso { Nombre = dto.Nombre, Apellido = dto.Apellido, Username = dto.Username, Password = BCrypt.Net.BCrypt.HashPassword(dto.Password), Especialidad = dto.Especialidad, Telefono = dto.Telefono };
            var nuevoTrabajador = new Trabajador { NombreCompleto = $"{dto.Nombre} {dto.Apellido}", UsuarioAcceso = nuevoAcceso };

            // === LÓGICA DE ASIGNACIÓN INTELIGENTE ===

            if (rolCreador == "Admin")
            {
                // El Admin asigna automáticamente a su propia obra
                int adminProyectoId = int.Parse(user.FindFirst("ProyectoId")!.Value);
                var proyectoDelAdmin = await dbContext.Proyectos.FindAsync(adminProyectoId);
                if (proyectoDelAdmin != null) nuevoTrabajador.Proyectos.Add(proyectoDelAdmin);
            }
            else if (rolCreador == "Jefe")
            {
                // El Jefe usa el selector
                if (dto.ProyectoId.HasValue && dto.ProyectoId > 0)
                {
                    var proyectoSeleccionado = await dbContext.Proyectos.FindAsync(dto.ProyectoId.Value);
                    if (proyectoSeleccionado != null) nuevoTrabajador.Proyectos.Add(proyectoSeleccionado);
                }
            }

            dbContext.Set<UsuarioAcceso>().Add(nuevoAcceso);
            dbContext.Set<Trabajador>().Add(nuevoTrabajador);
            await dbContext.SaveChangesAsync();

            // Coronar como líder si el Jefe lo registró como Admin
            if (rolCreador == "Jefe" && dto.Especialidad == "Administración" && dto.ProyectoId.HasValue)
            {
                var proyectoVinculado = await dbContext.Proyectos.FindAsync(dto.ProyectoId.Value);
                if (proyectoVinculado != null)
                {
                    proyectoVinculado.AdminId = nuevoAcceso.Id;
                    await dbContext.SaveChangesAsync();
                }
            }

            return Results.Ok(new { Message = "Usuario registrado." });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Jefe")); // Ambos pueden registrar

        // PUT: Actualizar datos de un usuario
        group.MapPut("/{id}", async (int id, UpdateUserDto dto, MaterialContext dbContext) =>
        {
            var usuario = await dbContext.UsuariosAcceso
                .Include(u => u.Trabajador)
                .FirstOrDefaultAsync(u => u.Id == id);
                
            if (usuario == null) return Results.NotFound(new { Message = "Usuario no encontrado" });

            // Verificamos que el nuevo username no lo tenga otra persona
            if (await dbContext.UsuariosAcceso.AnyAsync(u => u.Username == dto.Username && u.Id != id))
                return Results.BadRequest(new { Message = "Ese nombre de usuario ya está ocupado." });

            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
            usuario.Username = dto.Username;
            usuario.Telefono = dto.Telefono ?? "";

            // Solo cambiamos la contraseña si el Jefe escribió una nueva
            if (!string.IsNullOrEmpty(dto.Password))
            {
                usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            if (usuario.Trabajador != null)
            {
                usuario.Trabajador.NombreCompleto = $"{dto.Nombre} {dto.Apellido}";
            }

            await dbContext.SaveChangesAsync();
            return Results.Ok(new { Message = "Datos del usuario actualizados" });
            
        }).RequireAuthorization(policy => policy.RequireRole("Jefe", "Admin"));
    }

    

    private static string GenerarToken(List<Claim> claims, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"], claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}