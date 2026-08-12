using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public interface IArticuloService
    {
        Task<List<ArticuloReadDto>> GetAllArticulos();
        Task<ArticuloReadDto?> GetArticuloById(int id);
        Task<ArticuloReadDto> CreateArticulo(ArticuloCreateDto dto);
        Task<ArticuloReadDto?> UpdateArticulo(int id, ArticuloUpdateDto dto);
        Task<ArticuloReadDto?> ChangeArticuloStatus(int id);
    }

    public class ArticuloService : IArticuloService
    {
        private readonly AppDBContext _context;

        public ArticuloService(AppDBContext context)
        {
            _context = context;
        }

        public async Task<List<ArticuloReadDto>> GetAllArticulos()
        {
            var articulos = await _context.Articulos.ToListAsync();
            return articulos.Select(MapToReadDto).ToList();
        }

        public async Task<ArticuloReadDto?> GetArticuloById(int id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            return articulo == null ? null : MapToReadDto(articulo);
        }

        public async Task<ArticuloReadDto> CreateArticulo(ArticuloCreateDto dto)
        {
            ValidarArticulo(dto.Nombre, dto.Descripcion, dto.Precio);

            var nuevoArticulo = new Articulo
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
                Precio = dto.Precio,
                Estado = true,
                ImagenUrl = dto.ImagenUrl?.Trim() 
            };

            _context.Articulos.Add(nuevoArticulo);
            await _context.SaveChangesAsync();
            return MapToReadDto(nuevoArticulo);
        }

        public async Task<ArticuloReadDto?> UpdateArticulo(int id, ArticuloUpdateDto dto)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null) return null;

            ValidarArticulo(dto.Nombre, dto.Descripcion, dto.Precio);

            articulo.Nombre = dto.Nombre.Trim();
            articulo.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
            articulo.Precio = dto.Precio;
            articulo.Estado = dto.Estado;
            articulo.ImagenUrl = dto.ImagenUrl?.Trim(); // Agregado

            await _context.SaveChangesAsync();
            return MapToReadDto(articulo);
        }

        public async Task<ArticuloReadDto?> ChangeArticuloStatus(int id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null) return null;

            articulo.Estado = !articulo.Estado;
            await _context.SaveChangesAsync();
            return MapToReadDto(articulo);
        }

        // Validaciones de negocio compartidas entre creación y edición
        private static void ValidarArticulo(string nombre, string? descripcion, decimal precio)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException("El nombre del artículo es obligatorio.");

            if (nombre.Trim().Length > 100)
                throw new InvalidOperationException("El nombre del artículo no puede superar los 100 caracteres.");

            if (descripcion != null && descripcion.Length > 500)
                throw new InvalidOperationException("La descripción no puede superar los 500 caracteres.");

            if (precio <= 0)
                throw new InvalidOperationException("El precio debe ser mayor a 0.");
        }

        private static ArticuloReadDto MapToReadDto(Articulo a) => new()
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Descripcion = a.Descripcion,
            Precio = a.Precio,
            Estado = a.Estado,
            ImagenUrl = a.ImagenUrl 
        };
    }
}