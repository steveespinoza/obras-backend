using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Obras.Api.Constants; // <-- Nuestras constantes limpias
using Obras.Api.Data;
using Obras.Api.Dtos;
using Obras.Api.Models;

namespace Obras.Api.Services;

public class UserService(MaterialContext dbContext, IConfiguration config) : IUserService
{
    public async Task<List<UserSummaryDto>> ObtenerTodosAsync(string rol, int miProyectoId)
    {
        var query = dbContext.UsuariosAcceso
            .Include(u => u.Trabajador) 
                .ThenInclude(t => t.Proyectos)
            .AsSplitQuery() // <--- 🚀 NUEVO: Previene la sobrecarga de datos cruzados
            .AsQueryable();

        // Lógica de seguridad: El Admin solo ve a su gente
        if (rol == AppRoles.Admin)
        {
            query = query.Where(u => u.Trabajador != null && u.Trabajador.Proyectos.Any(p => p.Id == miProyectoId));
        }

// Dentro de ObtenerTodosAsync, cambia el select por esto:
        return await query.Select(u => new UserSummaryDto(
                u.Id,
                u.Nombre,
                u.Apellido,
                u.Trabajador != null ? u.Trabajador.NombreCompleto : $"{u.Nombre} {u.Apellido}",
                u.Username,
                u.Especialidad,
                u.Telefono,
                u.Trabajador != null ? (int?)u.Trabajador.Proyectos.FirstOrDefault()!.Id : null,
                (u.Trabajador != null && u.Trabajador.Proyectos.Any()) 
                    ? string.Join(", ", u.Trabajador.Proyectos.Select(p => p.Nombre))
                    : "Sin proyecto"
            ))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginInfo)
    {
        // PUERTA 1: Jefe
        var jefe = await dbContext.Jefes.FirstOrDefaultAsync(j => j.Username == loginInfo.Username);
        if (jefe != null && BCrypt.Net.BCrypt.Verify(loginInfo.Password, jefe.Password))
        {
            var claimsJefe = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, $"J-{jefe.Id}"),
                new Claim(ClaimTypes.Name, $"{jefe.Nombre} {jefe.Apellido}"),
                new Claim(ClaimTypes.Role, AppRoles.Jefe)
            };
            return new LoginResponseDto(jefe.Id, $"{jefe.Nombre} {jefe.Apellido}", AppRoles.Jefe, null, GenerarToken(claimsJefe));
        }

        // PUERTA 2: Trabajadores y Admins
        var acceso = await dbContext.UsuariosAcceso
            .Include(u => u.Trabajador)
                .ThenInclude(t => t.Proyectos)
            .FirstOrDefaultAsync(u => u.Username == loginInfo.Username);

        if (acceso is null || acceso.Trabajador is null || !BCrypt.Net.BCrypt.Verify(loginInfo.Password, acceso.Password))
            return null; // Retornamos null para que el endpoint lance Unauthorized

        var isAdmin = acceso.Especialidad == Especialidades.Administracion;
        var proyectoActivoId = acceso.Trabajador.Proyectos.FirstOrDefault()?.Id;

        var claimsUsuario = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, acceso.Trabajador.Id.ToString()),
            new Claim(ClaimTypes.Name, acceso.Trabajador.NombreCompleto),
            new Claim(ClaimTypes.Role, isAdmin ? AppRoles.Admin : AppRoles.User),
            new Claim("ProyectoId", proyectoActivoId?.ToString() ?? "0") 
        };

        return new LoginResponseDto(acceso.Trabajador.Id, acceso.Trabajador.NombreCompleto, isAdmin ? AppRoles.Admin : AppRoles.User, proyectoActivoId, GenerarToken(claimsUsuario));
    }

    public async Task<(bool Exito, string Mensaje)> RegistrarAsync(CreateUserDto dto, string rolCreador, int adminProyectoId)
    {
        // Reglas de negocio
        if (rolCreador == AppRoles.Admin && dto.Especialidad == Especialidades.Administracion)
            return (false, "Acceso denegado: Un Administrador de obra no puede crear a otro Administrador.");

        if (rolCreador == AppRoles.Jefe && dto.Especialidad != Especialidades.Administracion)
            return (false, "Acceso denegado: El Jefe supremo solo debe registrar perfiles de Administración.");

        if (await dbContext.UsuariosAcceso.AnyAsync(u => u.Username == dto.Username))
            return (false, "El nombre de usuario ya está en uso.");
        // ¡NUEVA REGLA DE NEGOCIO!
        if (rolCreador == AppRoles.Admin && adminProyectoId <= 0)
            return (false, "No puedes registrar personal porque no tienes ninguna obra asignada actualmente.");

        var nuevoAcceso = new UsuarioAcceso { 
            Nombre = dto.Nombre, Apellido = dto.Apellido, Username = dto.Username, 
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password), 
            Especialidad = dto.Especialidad, Telefono = dto.Telefono 
        };
        var nuevoTrabajador = new Trabajador { NombreCompleto = $"{dto.Nombre} {dto.Apellido}", UsuarioAcceso = nuevoAcceso };

        // Lógica de Asignación Inteligente
        if (rolCreador == AppRoles.Admin)
        {
            var proyectoDelAdmin = await dbContext.Proyectos.FindAsync(adminProyectoId);
            if (proyectoDelAdmin != null) nuevoTrabajador.Proyectos.Add(proyectoDelAdmin);
        }
        else if (rolCreador == AppRoles.Jefe && dto.ProyectoId.HasValue && dto.ProyectoId > 0)
        {
            var proyectoSeleccionado = await dbContext.Proyectos.FindAsync(dto.ProyectoId.Value);
            if (proyectoSeleccionado != null) nuevoTrabajador.Proyectos.Add(proyectoSeleccionado);
        }

        dbContext.Set<UsuarioAcceso>().Add(nuevoAcceso);
        dbContext.Set<Trabajador>().Add(nuevoTrabajador);
        await dbContext.SaveChangesAsync();

        // Coronar como líder si el Jefe lo registró
// Coronar como líder si el Jefe lo registró
        if (rolCreador == AppRoles.Jefe && dto.Especialidad == Especialidades.Administracion && dto.ProyectoId.HasValue)
        {
            var proyectoVinculado = await dbContext.Proyectos.FindAsync(dto.ProyectoId.Value);
            if (proyectoVinculado != null)
            {
                // 1. ¡NUEVO! Detectamos si ya había un admin antes y le quitamos la obra de su historial
                var viejoAdminId = proyectoVinculado.AdminId;
                if (viejoAdminId.HasValue)
                {
                    var viejoTrabajador = await dbContext.Trabajadores
                        .Include(t => t.Proyectos)
                        .FirstOrDefaultAsync(t => t.UsuarioAccesoId == viejoAdminId.Value);
                        
                    if (viejoTrabajador != null) 
                    {
                        viejoTrabajador.Proyectos.Remove(proyectoVinculado);
                    }
                }

                // 2. Asignamos al nuevo usuario como el admin definitivo
                proyectoVinculado.AdminId = nuevoAcceso.Id;
                
                await dbContext.SaveChangesAsync();
            }
        }

        return (true, "Usuario registrado.");
    }

    public async Task<(bool Exito, string Mensaje, bool NoEncontrado)> ActualizarAsync(int id, UpdateUserDto dto)
    {
        var usuario = await dbContext.UsuariosAcceso
            .Include(u => u.Trabajador)
                .ThenInclude(t => t.Proyectos) // <-- IMPORTANTE: Incluir proyectos
            .FirstOrDefaultAsync(u => u.Id == id);
            
        if (usuario == null) return (false, "Usuario no encontrado", true);

        if (await dbContext.UsuariosAcceso.AnyAsync(u => u.Username == dto.Username && u.Id != id))
            return (false, "Ese nombre de usuario ya está ocupado.", false);

        usuario.Nombre = dto.Nombre;
        usuario.Apellido = dto.Apellido;
        usuario.Username = dto.Username;
        usuario.Telefono = dto.Telefono ?? "";

        if (!string.IsNullOrEmpty(dto.Password))
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        if (usuario.Trabajador != null)
        {
            usuario.Trabajador.NombreCompleto = $"{dto.Nombre} {dto.Apellido}";

// LÓGICA DE REASIGNACIÓN DE OBRAS
            var proyectoActual = usuario.Trabajador.Proyectos.FirstOrDefault();

            if (proyectoActual?.Id != dto.ProyectoId)
            {
                // 1. Limpieza total y segura: Le quitamos la obra antigua
                if (usuario.Trabajador.Proyectos.Any())
                {
                    // Liberamos el AdminId de la obra en la base de datos
                    var obraVieja = await dbContext.Proyectos.FindAsync(proyectoActual!.Id);
                    if (obraVieja != null && obraVieja.AdminId == usuario.Id)
                    {
                        obraVieja.AdminId = null; 
                    }
                    
                    // Vaciamos la lista completa para forzar a EF Core a borrar la relación
                    usuario.Trabajador.Proyectos.Clear();
                }

                // 2. Asignarlo al nuevo proyecto (si seleccionaron uno)
                if (dto.ProyectoId.HasValue && dto.ProyectoId.Value > 0)
                {
                    var obraNueva = await dbContext.Proyectos.FindAsync(dto.ProyectoId.Value);
                    if (obraNueva != null)
                    {
                        // Si la obra nueva ya tenía a OTRO admin, se lo quitamos a la fuerza
                        if (obraNueva.AdminId.HasValue && obraNueva.AdminId != usuario.Id)
                        {
                            var adminAnterior = await dbContext.Trabajadores
                                .Include(t => t.Proyectos)
                                .FirstOrDefaultAsync(t => t.UsuarioAccesoId == obraNueva.AdminId.Value);
                            if (adminAnterior != null) adminAnterior.Proyectos.Clear(); // También usamos Clear aquí
                        }

                        usuario.Trabajador.Proyectos.Add(obraNueva);
                        obraNueva.AdminId = usuario.Id;
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync();
        return (true, "Datos y asignación del administrador actualizados.", false);
    }

    private string GenerarToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"], claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}