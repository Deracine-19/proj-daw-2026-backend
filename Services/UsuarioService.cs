using Mapster;
using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public interface IUsuarioService
    {
        Task<PagedResultDto<UsuarioDto>> GetAll(int page, int pageSize, string? busqueda, string? ordenarPor, string? ordenDireccion, string? rol, bool? activo);
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

        public async Task<PagedResultDto<UsuarioDto>> GetAll(
            int page, int pageSize, string? busqueda, string? ordenarPor, string? ordenDireccion, string? rol, bool? activo)
        {
            (page, pageSize) = PaginacionHelper.Normalizar(page, pageSize);

            var query = _context.Usuarios.Include(u => u.Rol).AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim().ToLower();
                query = query.Where(u => u.Nombre.ToLower().Contains(termino) || u.Email.ToLower().Contains(termino));
            }

            if (!string.IsNullOrWhiteSpace(rol))
            {
                query = query.Where(u => u.Rol.Nombre == rol);
            }

            if (activo.HasValue)
            {
                query = query.Where(u => u.Activo == activo.Value);
            }

            bool desc = string.Equals(ordenDireccion, "desc", StringComparison.OrdinalIgnoreCase);
            query = ordenarPor?.ToLower() switch
            {
                "email" => desc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "rol" or "rolnombre" => desc ? query.OrderByDescending(u => u.Rol.Nombre) : query.OrderBy(u => u.Rol.Nombre),
                "activo" or "estado" => desc ? query.OrderByDescending(u => u.Activo) : query.OrderBy(u => u.Activo),
                _ => desc ? query.OrderByDescending(u => u.Nombre) : query.OrderBy(u => u.Nombre),
            };

            var totalCount = await query.CountAsync();
            var usuarios = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectToType<UsuarioDto>()
                .ToListAsync();

            return new PagedResultDto<UsuarioDto>
            {
                Items = usuarios,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
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