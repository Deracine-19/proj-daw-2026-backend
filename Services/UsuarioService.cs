using Mapster;
using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public interface IUsuarioService
    {
        Task<List<UsuarioDto>> GetAll();
        Task<UsuarioDto?> GetById(int id);
        Task<UsuarioDto> Update(int id, UsuarioUpdateDto dto);
        Task<UsuarioDto> CreateUser(UsuarioCreateDto dto);
        Task<UsuarioDto> ChangeUserStatus(int id, int currentUserId);
    }

    public class UsuarioService : IUsuarioService
    {
        private readonly AppDBContext _context;

        public UsuarioService(AppDBContext context)
        {
            _context = context;
        }

        public async Task<List<UsuarioDto>> GetAll()
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .ProjectToType<UsuarioDto>()
                .ToListAsync();
        }

        public async Task<UsuarioDto?> GetById(int id)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Id == id)
                .ProjectToType<UsuarioDto>()
                .FirstOrDefaultAsync();
        }

        public async Task<UsuarioDto> Update(int id, UsuarioUpdateDto dto)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new KeyNotFoundException("Usuario no encontrado");

            usuario.Nombre = dto.Nombre;
            usuario.Email = dto.Email;
            usuario.RolId = dto.RolId;

            await _context.SaveChangesAsync();

            // ============== Algo que dijo Claude ================
            // El Rol que ya tenías cargado en memoria sigue siendo el VIEJO después de cambiar RolId,
            // porque EF Core no releé la relación de navegación sola cuando solo cambias el FK.
            // Sin esta línea, RolNombre en la respuesta mostraría el rol anterior, no el nuevo.
            await _context.Entry(usuario).Reference(u => u.Rol).LoadAsync();

            return usuario.Adapt<UsuarioDto>();
        }

        public async Task<UsuarioDto> CreateUser(UsuarioCreateDto dto)
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RolId = dto.RolId,
                Activo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            await _context.Entry(usuario).Reference(u => u.Rol).LoadAsync();

            return usuario.Adapt<UsuarioDto>();
        }

        public async Task<UsuarioDto> ChangeUserStatus(int id, int currentUserId)
        {
            if (id == currentUserId)
                throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id)
                ?? throw new KeyNotFoundException("Usuario no encontrado");

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();

            return usuario.Adapt<UsuarioDto>();
        }
    }
}