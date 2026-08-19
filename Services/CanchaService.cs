using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public class CanchaService
    {
        private readonly AppDBContext _context;

        public CanchaService(AppDBContext context)
        {
            _context = context;
        }

        // GET: Obtener canchas paginadas, con búsqueda y orden opcionales
        public async Task<PagedResultDto<CanchaReadDto>> GetAllCanchasAsync(
            int page, int pageSize, string? busqueda, string? ordenarPor, string? ordenDireccion)
        {
            (page, pageSize) = PaginacionHelper.Normalizar(page, pageSize);

            var query = ConstruirConsultaCanchas(busqueda, ordenarPor, ordenDireccion);

            var totalCount = await query.CountAsync();
            var canchas = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<CanchaReadDto>
            {
                Items = canchas.Select(MapToReadDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // GET: Obtener cancha por ID
        public async Task<CanchaReadDto?> GetCanchaByIdAsync(int id)
        {
            var cancha = await _context.Canchas.FindAsync(id);
            return cancha == null ? null : MapToReadDto(cancha);
        }

        // POST: Crear cancha
        public async Task<CanchaReadDto> CreateCanchaAsync(CanchaDto dto)
        {
            ValidarCancha(dto);

            var cancha = new Cancha
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
                PrecioHora = dto.PrecioHora,
                Estado = dto.Estado,
                CantidadJugadores = dto.CantidadJugadores,
                ImagenBase64 = dto.ImagenBase64
            };

            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();
            return MapToReadDto(cancha);
        }

        // PUT: Actualizar cancha
        public async Task<CanchaReadDto?> UpdateCanchaAsync(int id, CanchaDto dto)
        {
            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha == null) return null;

            ValidarCancha(dto);

            cancha.Nombre = dto.Nombre.Trim();
            cancha.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
            cancha.PrecioHora = dto.PrecioHora;
            cancha.Estado = dto.Estado;
            cancha.CantidadJugadores = dto.CantidadJugadores;
            cancha.ImagenBase64 = dto.ImagenBase64;
            cancha.LastEditedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToReadDto(cancha);
        }

        // Validaciones de negocio compartidas entre creación y edición
        private static void ValidarCancha(CanchaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new InvalidOperationException("El nombre de la cancha es obligatorio.");

            if (dto.Nombre.Trim().Length > 100)
                throw new InvalidOperationException("El nombre de la cancha no puede superar los 100 caracteres.");

            if (dto.Descripcion != null && dto.Descripcion.Length > 500)
                throw new InvalidOperationException("La descripción no puede superar los 500 caracteres.");

            if (dto.PrecioHora <= 0)
                throw new InvalidOperationException("El precio por hora debe ser mayor a 0.");

            if (dto.CantidadJugadores <= 0)
                throw new InvalidOperationException("La cantidad de jugadores debe ser mayor a 0.");

            ImagenValidator.Validar(dto.ImagenBase64);
        }

        // PATCH: Cambiar estado (Activa / Inactiva)
        public async Task<CanchaReadDto?> ChangeCanchaStatusAsync(int id)
        {
            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha == null) return null;

            cancha.Estado = !cancha.Estado;
            cancha.LastEditedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToReadDto(cancha);
        }

        // Construye la consulta filtrada (búsqueda/orden) SIN paginar — la comparten el listado
        // paginado y el reporte de exportación para que ambos apliquen los mismos filtros.
        private IQueryable<Cancha> ConstruirConsultaCanchas(string? busqueda, string? ordenarPor, string? ordenDireccion)
        {
            var query = _context.Canchas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim().ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(termino) ||
                    c.Descripcion.ToLower().Contains(termino));
            }

            bool desc = string.Equals(ordenDireccion, "desc", StringComparison.OrdinalIgnoreCase);
            return ordenarPor?.ToLower() switch
            {
                "precio" or "preciohora" => desc ? query.OrderByDescending(c => c.PrecioHora) : query.OrderBy(c => c.PrecioHora),
                "jugadores" or "cantidadjugadores" => desc ? query.OrderByDescending(c => c.CantidadJugadores) : query.OrderBy(c => c.CantidadJugadores),
                "estado" => desc ? query.OrderByDescending(c => c.Estado) : query.OrderBy(c => c.Estado),
                _ => desc ? query.OrderByDescending(c => c.Nombre) : query.OrderBy(c => c.Nombre),
            };
        }

        // GET: Canchas para el reporte CSV — mismos filtros que la tabla del panel, sin paginar.
        public async Task<List<Cancha>> GetCanchasParaExportarAsync(string? busqueda, string? ordenarPor, string? ordenDireccion)
        {
            return await ConstruirConsultaCanchas(busqueda, ordenarPor, ordenDireccion).ToListAsync();
        }

        private static CanchaReadDto MapToReadDto(Cancha c) => new()
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            PrecioHora = c.PrecioHora,
            Estado = c.Estado,
            CantidadJugadores = c.CantidadJugadores,
            ImagenBase64 = c.ImagenBase64
        };
    }
}
