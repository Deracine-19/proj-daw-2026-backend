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

            await _context.SaveChangesAsync();

            return usuario.Adapt<UsuarioDto>();
        }
    }
}