using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Obras.Api.Data;
using Obras.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirReact", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
        policy.WithOrigins(frontendUrl) 
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ==========================================
// 2. CONFIGURACIÓN DE JWT (NUEVO)
// ==========================================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization(); // Habilita la autorización

builder.Services.AddValidation();
builder.AddMatDb();

var app = builder.Build();

app.UseCors("PermitirReact");

// ==========================================
// 3. ACTIVAR SEGURIDAD (El orden importa)
// ==========================================
app.UseAuthentication(); // Primero verifica quién eres
app.UseAuthorization();  // Luego verifica qué puedes hacer

// Mapeo de Endpoints
app.MapRequerimientosEndpoints();
app.MapUsersEndpoints();
app.MapAlmacenEndpoints();

app.MigrateDb();

app.Run();