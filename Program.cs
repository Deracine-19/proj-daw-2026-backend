using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // <-- 1. Importante para la configuración de Swagger JWT
using proj_daw_2026_backend.Services;
using proj_daw_2026_backend.Data.Entities;
using System.Text;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Railway (y la mayoría de PaaS) inyectan el puerto a escuchar vía la variable PORT en
// tiempo de ejecución, no en un archivo de config. Solo forzamos el binding cuando esa
// variable existe (dentro del contenedor) — en local dev NO tocamos nada, así Kestrel sigue
// usando lo que ya dice Properties/launchSettings.json (http://0.0.0.0:5248), que es el
// puerto que el resto del equipo, Postman y el .env del frontend ya esperan.
var puerto = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(puerto))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");
}

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
    options.UseNpgsql(ObtenerCadenaConexion(builder.Configuration)));

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
        policy.WithOrigins(ObtenerOrigenesPermitidos(builder.Configuration))
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Aplica migraciones pendientes al arrancar — así cada deploy en Railway deja la base de
// datos al día sola, sin un paso manual de "dotnet ef database update" contra producción.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
    db.Database.Migrate();
}

app.UseCors("FrontendPolicy");

// 6. Configurar el entorno HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Railway (y la mayoría de PaaS) terminan TLS en su proxy y le reenvían al contenedor
// tráfico en HTTP plano — si se fuerza HttpsRedirection ahí, Kestrel ve siempre HTTP y
// redirige en bucle. En producción el proxy ya garantiza HTTPS de cara al usuario.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 7. MIDDLEWARES DE SEGURIDAD
app.UseAuthentication();
app.UseAuthorization();

// 8. Mapear las rutas a los controladores
app.MapControllers();

app.Run();

// Railway inyecta Postgres como DATABASE_URL en formato URI ("postgres://user:pass@host:port/db"),
// pero Npgsql espera el formato "Host=...;Port=...;Database=...;Username=...;Password=..." — si
// existe esa variable se convierte acá; en local se sigue usando ConnectionStrings:DefaultConnection
// tal cual (de appsettings.Development.json).
static string ObtenerCadenaConexion(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrEmpty(databaseUrl))
    {
        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection y no hay DATABASE_URL.");
    }

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    }.ConnectionString;
}

// Orígenes permitidos por CORS: configurables vía "Cors:AllowedOrigins" en appsettings
// (o la variable de entorno Cors__AllowedOrigins en Railway), separados por comas — para poder
// apuntar al dominio real del frontend una vez que Railway se lo asigne. localhost:5173 siempre
// queda permitido para desarrollo local.
static string[] ObtenerOrigenesPermitidos(IConfiguration configuration)
{
    var configurados = configuration["Cors:AllowedOrigins"]
        ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? Array.Empty<string>();

    return configurados.Append("http://localhost:5173").Distinct().ToArray();
}