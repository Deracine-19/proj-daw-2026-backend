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
    }

    public class AuthService : IAuthService
    {
        private readonly AppDBContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<Usuario> Register(RegisterDto dto)
        {
            var hashPassword = HashPassword(dto.Password);

            var nuevoUsuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                Password = hashPassword,
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

            if (usuario == null || !VerifyPassword(dto.Password, usuario.Password))
                throw new UnauthorizedAccessException("Credenciales incorrectas");

            // Si la propiedad de navegación no cargó el Rol, lo buscamos explícitamente en _context.Roles
            if (usuario.Rol == null)
            {
                usuario.Rol = await _context.Roles.FindAsync(usuario.RolId);
            }

            return GenerateToken(usuario);
        }

        private string GenerateToken(Usuario usuario)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["JwtSettings:Key"]!);

            // Nos aseguramos de obtener el nombre real del rol
            string nombreRol = usuario.Rol?.Nombre ?? "Cliente";

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, nombreRol)
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