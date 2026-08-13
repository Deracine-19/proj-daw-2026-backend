using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace proj_daw_2026_backend.Services
{
    public interface IAuthService
    {
        Task<Usuario> Register(RegisterDto dto);
        Task<string> Login(LoginDto dto);
        Task<bool> ForgotPassword(ForgotPasswordDto dto);
        Task<(bool Exito, string Mensaje)> ResetPassword(ResetPasswordDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService; // Inyección para enviar correos
        private readonly ILogger<AuthService> _logger;
        private readonly IHostEnvironment _env;

        public AuthService(AppDBContext context, IConfiguration config, EmailService emailService, ILogger<AuthService> logger, IHostEnvironment env)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
            _logger = logger;
            _env = env;
        }

        public async Task<Usuario> Register(RegisterDto dto)
        {
            var hashPassword = HashPassword(dto.Password);

            var nuevoUsuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                PasswordHash = hashPassword,
                RolId = 1 // Clientes por defecto
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();
            return nuevoUsuario;
        }

        public async Task<string> Login(LoginDto dto)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (usuario == null || !VerifyPassword(dto.Password, usuario.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales incorrectas");

            if (!usuario.Activo)
                throw new InvalidOperationException("Tu cuenta está desactivada. Contacta a un administrador.");

            if (usuario.Rol == null)
            {
                usuario.Rol = await _context.Roles.FindAsync(usuario.RolId);
            }

            return GenerateToken(usuario);
        }

        // 1. Solicitud de Contraseña Temporal (Olvidé mi Contraseña)
        public async Task<bool> ForgotPassword(ForgotPasswordDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (usuario == null) return false;

            // Guardar la contraseña anterior antes de sobrescribirla (solo si no se ha respaldado antes)
            if (string.IsNullOrEmpty(usuario.PasswordAnteriorHash))
            {
                usuario.PasswordAnteriorHash = usuario.PasswordHash;
            }

            // Generar clave temporal de 12 caracteres. Ojo: antes tenía un sufijo fijo "#2026"
            // pegado a los 8 caracteres aleatorios — cualquiera que viera el patrón se ahorraba
            // adivinar 5 de los 13 caracteres, y además quedaba desactualizado cada año.
            string tempPassword = Guid.NewGuid().ToString("N").Substring(0, 12);

            // SOLO EN DEVELOPMENT: imprime la clave en la consola del backend para poder probar
            // el flujo sin depender de que el SMTP esté configurado/accesible. Nunca se ejecuta
            // en Production porque queda condicionado a IsDevelopment().
            if (_env.IsDevelopment())
            {
                _logger.LogWarning("[SOLO DEV] Clave temporal para {Email}: {TempPassword}", usuario.Email, tempPassword);
            }

            // Guardar clave temporal e indicar que requiere cambio
            usuario.PasswordHash = HashPassword(tempPassword);
            usuario.RequiereCambioPassword = true;

            await _context.SaveChangesAsync();

            // Enviar correo electrónico con la clave temporal
            string mensajeHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #2c3e50; text-align: center;'>Recuperación de Contraseña 🔑</h2>
                    <p>Hola <strong>{usuario.Nombre}</strong>,</p>
                    <p>Has solicitado restablecer tu contraseña. Tu clave temporal de acceso es:</p>
                    <div style='text-align: center; margin: 20px 0;'>
                        <span style='background-color: #f8f9fa; border: 1px dashed #2980b9; padding: 10px 20px; font-size: 22px; font-weight: bold; color: #2980b9;'>{tempPassword}</span>
                    </div>
                    <p style='color: #e74c3c;'><strong>Nota:</strong> Deberás ingresar esta clave e inmediatamente registrar una nueva contraseña.</p>
                </div>";

            // Igual que en ReservaService: sin este try/catch, un fallo de SMTP desaparece en
            // silencio dentro del Task.Run desatendido y nunca se sabría por qué no llegó el correo.
            var destinatario = usuario.Email;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(destinatario, "Clave Temporal de Acceso", mensajeHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo enviar el correo de recuperación de contraseña a {Email}", destinatario);
                }
            });

            return true;
        }

        // 2. Restablecer Contraseña (Validando no repetir la anterior)
        public async Task<(bool Exito, string Mensaje)> ResetPassword(ResetPasswordDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (usuario == null)
                return (false, "El usuario no existe.");

            // Validar clave temporal
            if (!VerifyPassword(dto.TempPassword, usuario.PasswordHash))
                return (false, "La contraseña temporal ingresada es incorrecta.");

            // Validar que la NUEVA clave no coincida con la ANTERIOR
            if (!string.IsNullOrEmpty(usuario.PasswordAnteriorHash))
            {
                if (VerifyPassword(dto.NewPassword, usuario.PasswordAnteriorHash))
                {
                    return (false, "No puedes reutilizar tu contraseña anterior. Por favor, elige una diferente.");
                }
            }

            // Actualizar a la nueva contraseña
            usuario.PasswordHash = HashPassword(dto.NewPassword);
            usuario.RequiereCambioPassword = false;
            usuario.PasswordAnteriorHash = null; // Limpiar el historial tras actualizar con éxito

            await _context.SaveChangesAsync();

            return (true, "Contraseña actualizada exitosamente.");
        }

        private string GenerateToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["JwtSettings:Key"]!);

            string nombreRol = usuario.Rol?.Nombre ?? "Cliente";

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, nombreRol),
                    new Claim("requiereCambioPassword", usuario.RequiereCambioPassword.ToString().ToLower()) // Indicador para el frontend
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _config["JwtSettings:Issuer"],
                Audience = _config["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}