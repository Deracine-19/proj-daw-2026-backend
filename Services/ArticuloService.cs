using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public interface IArticuloService
    {
        Task<PagedResultDto<ArticuloReadDto>> GetAllArticulos(int page, int pageSize, string? busqueda, string? ordenarPor, string? ordenDireccion);
        Task<ArticuloReadDto?> GetArticuloById(int id);
        Task<ArticuloReadDto> CreateArticulo(ArticuloCreateDto dto);
        Task<ArticuloReadDto?> UpdateArticulo(int id, ArticuloUpdateDto dto);
        Task<ArticuloReadDto?> ChangeArticuloStatus(int id);
        Task<List<Articulo>> GetArticulosParaExportarAsync(string? busqueda, string? ordenarPor, string? ordenDireccion);
    }

    public class ArticuloService : IArticuloService
    {
        private readonly AppDBContext _context;

        public ArticuloService(AppDBContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<ArticuloReadDto>> GetAllArticulos(
            int page, int pageSize, string? busqueda, string? ordenarPor, string? ordenDireccion)
        {
            (page, pageSize) = PaginacionHelper.Normalizar(page, pageSize);

            var query = ConstruirConsultaArticulos(busqueda, ordenarPor, ordenDireccion);

            var totalCount = await query.CountAsync();
            var articulos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<ArticuloReadDto>
            {
                Items = articulos.Select(MapToReadDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ArticuloReadDto?> GetArticuloById(int id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            return articulo == null ? null : MapToReadDto(articulo);
        }

        public async Task<ArticuloReadDto> CreateArticulo(ArticuloCreateDto dto)
        {
            ValidarArticulo(dto.Nombre, dto.Descripcion, dto.Precio, dto.ImagenBase64);

            var nuevoArticulo = new Articulo
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
                Precio = dto.Precio,
                Estado = true,
                ImagenBase64 = dto.ImagenBase64
            };

            _context.Articulos.Add(nuevoArticulo);
            await _context.SaveChangesAsync();
            return MapToReadDto(nuevoArticulo);
        }

        public async Task<ArticuloReadDto?> UpdateArticulo(int id, ArticuloUpdateDto dto)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null) return null;

            ValidarArticulo(dto.Nombre, dto.Descripcion, dto.Precio, dto.ImagenBase64);

            articulo.Nombre = dto.Nombre.Trim();
            articulo.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
            articulo.Precio = dto.Precio;
            articulo.Estado = dto.Estado;
            articulo.ImagenBase64 = dto.ImagenBase64;
            articulo.LastEditedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToReadDto(articulo);
        }

        public async Task<ArticuloReadDto?> ChangeArticuloStatus(int id)
        {
            var articulo = await _context.Articulos.FindAsync(id);
            if (articulo == null) return null;

            articulo.Estado = !articulo.Estado;
            articulo.LastEditedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToReadDto(articulo);
        }

        // Validaciones de negocio compartidas entre creación y edición
        private static void ValidarArticulo(string nombre, string? descripcion, decimal precio, string? imagenBase64)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException("El nombre del artículo es obligatorio.");

            if (nombre.Trim().Length > 100)
                throw new InvalidOperationException("El nombre del artículo no puede superar los 100 caracteres.");

            if (descripcion != null && descripcion.Length > 500)
                throw new InvalidOperationException("La descripción no puede superar los 500 caracteres.");

            if (precio <= 0)
                throw new InvalidOperationException("El precio debe ser mayor a 0.");

            ImagenValidator.Validar(imagenBase64);
        }

        // Construye la consulta filtrada (búsqueda/orden) SIN paginar — la comparten el listado
        // paginado y el reporte de exportación para que ambos apliquen los mismos filtros.
        private IQueryable<Articulo> ConstruirConsultaArticulos(string? busqueda, string? ordenarPor, string? ordenDireccion)
        {
            var query = _context.Articulos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim().ToLower();
                query = query.Where(a =>
                    a.Nombre.ToLower().Contains(termino) ||
                    a.Descripcion.ToLower().Contains(termino));
            }

            bool desc = string.Equals(ordenDireccion, "desc", StringComparison.OrdinalIgnoreCase);
            return ordenarPor?.ToLower() switch
            {
                "precio" => desc ? query.OrderByDescending(a => a.Precio) : query.OrderBy(a => a.Precio),
                "estado" => desc ? query.OrderByDescending(a => a.Estado) : query.OrderBy(a => a.Estado),
                _ => desc ? query.OrderByDescending(a => a.Nombre) : query.OrderBy(a => a.Nombre),
            };
        }

        // GET: Artículos para el reporte CSV — mismos filtros que la tabla del panel, sin paginar.
        public async Task<List<Articulo>> GetArticulosParaExportarAsync(string? busqueda, string? ordenarPor, string? ordenDireccion)
        {
            return await ConstruirConsultaArticulos(busqueda, ordenarPor, ordenDireccion).ToListAsync();
        }

        private static ArticuloReadDto MapToReadDto(Articulo a) => new()
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Descripcion = a.Descripcion,
            Precio = a.Precio,
            Estado = a.Estado,
            ImagenBase64 = a.ImagenBase64
        };
    }
}