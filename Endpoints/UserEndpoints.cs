using Obras.Api.Constants;
using Obras.Api.Dtos;
using Obras.Api.Services; // <-- Importamos los servicios
using System.Security.Claims;

namespace Obras.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/users");

        // GET: Obtener lista de usuarios 
        group.MapGet("/", async (IUserService userService, ClaimsPrincipal user) =>
        {
            var rol = user.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var miProyectoId = int.Parse(user.FindFirst("ProyectoId")?.Value ?? "0");

            var usuarios = await userService.ObtenerTodosAsync(rol, miProyectoId);
            return Results.Ok(usuarios);
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Jefe));

        // POST: Login (Ruta PÚBLICA, no requiere autorización)
        group.MapPost("/login", async (LoginDto loginInfo, IUserService userService) =>
        {
            var response = await userService.LoginAsync(loginInfo);
            
            return response is not null 
                ? Results.Ok(response) 
                : Results.Unauthorized(); 
        });

        // POST: Registro 
        group.MapPost("/registro", async (CreateUserDto dto, IUserService userService, ClaimsPrincipal user) =>
        {
            var rolCreador = user.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var adminProyectoId = int.Parse(user.FindFirst("ProyectoId")?.Value ?? "0");

            var resultado = await userService.RegistrarAsync(dto, rolCreador, adminProyectoId);

            if (!resultado.Exito)
                return Results.BadRequest(new { Message = resultado.Mensaje });

            return Results.Ok(new { Message = resultado.Mensaje });
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Jefe)); 

        // PUT: Actualizar datos de un usuario
        group.MapPut("/{id}", async (int id, UpdateUserDto dto, IUserService userService) =>
        {
            var resultado = await userService.ActualizarAsync(id, dto);

            if (!resultado.Exito)
            {
                if (resultado.NoEncontrado) return Results.NotFound(new { Message = resultado.Mensaje });
                return Results.BadRequest(new { Message = resultado.Mensaje });
            }

            return Results.Ok(new { Message = resultado.Mensaje });
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Jefe, AppRoles.Admin));
    }
}