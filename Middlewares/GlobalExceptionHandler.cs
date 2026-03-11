using Microsoft.AspNetCore.Diagnostics;

namespace Obras.Api.Middlewares; // Ajusta el namespace si usaste otra carpeta

// Usamos inyección de dependencias para traer el Logger nativo de .NET
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        // 1. Registramos el error real en la consola del servidor para que tú puedas depurarlo
        logger.LogError(exception, "🔥 Ocurrió un error crítico no controlado en la API.");

        // 2. Configuramos la respuesta HTTP que se enviará a React
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        // 3. Creamos un objeto JSON estandarizado. 
        // Nota: Usamos "Message" para que coincida con lo que espera tu frontend en api.js
        var errorResponse = new
        {
            Message = "Ocurrió un error interno en el servidor. Por favor, inténtalo de nuevo más tarde."
        };

        // 4. Escribimos la respuesta y se la enviamos al cliente
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        // 5. Retornamos 'true' para decirle a .NET: "Tranquilo, yo me hice cargo de este error, no detengas la app".
        return true; 
    }
}