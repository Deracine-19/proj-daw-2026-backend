using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // <-- 1. Importante para la configuración de Swagger JWT
using proj_daw_2026_backend.Services;
using proj_daw_2026_backend.Data.Entities;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Consistencia para las rutas (todas en minúscula)
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// 1. Agregar los Controladores
builder.Services.AddControllers();

// 2. Configurar Swagger con soporte para JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "proj_daw_2026_backend", Version = "v1" });

    // Definición del esquema de seguridad para Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT obtenido en el login."
    });

    // Aplicar el esquema de seguridad globalmente a la interfaz de Swagger
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 3. Conexión a PostgreSQL
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. Inyección de Dependencias
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<CanchaService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<IArticuloService, ArticuloService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<EmailService>();

// 5. CONFIGURACIÓN DE AUTENTICACIÓN JWT
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
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("FrontendPolicy");

// 6. Configurar el entorno HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 7. MIDDLEWARES DE SEGURIDAD
app.UseAuthentication();
app.UseAuthorization();

// 8. Mapear las rutas a los controladores
app.MapControllers();

app.Run();