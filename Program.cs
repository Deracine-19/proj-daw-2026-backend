using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using proj_daw_2026_backend.Services;
using proj_daw_2026_backend.Data.Entities;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Consistencia para las rutas (todas en minuscula)
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// 1. Agregar los Controladores
builder.Services.AddControllers();

// 2. Configurar Swagger para las pruebas
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Conexión a PostgreSQL
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. Inyección de Dependencias
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// 5. ---> CONFIGURACIÓN DE AUTENTICACIÓN JWT ACTUALIZADA <---
// Ahora lee los nombres exactos que tienes en tu appsettings.json
var jwtKey = builder.Configuration["JwtSettings:Key"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

var key = Encoding.UTF8.GetBytes(jwtKey!);

builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(config =>
{
    config.RequireHttpsMetadata = false;
    config.SaveToken = true;
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        // Habilitamos la validación porque ya los tienes en tu JSON
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,

        ValidateAudience = true,
        ValidAudience = jwtAudience
    };
});

var app = builder.Build();

// 6. Configurar el entorno HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 7. ---> MIDDLEWARES DE SEGURIDAD <---
app.UseAuthentication();
app.UseAuthorization();

// 8. Mapear las rutas a los controladores
app.MapControllers();

app.Run();