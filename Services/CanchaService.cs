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

        // GET: Obtener todas las canchas
        public async Task<List<Cancha>> GetAllCanchasAsync()
        {
            return await _context.Canchas.ToListAsync();
        }

        // GET: Obtener cancha por ID
        public async Task<Cancha?> GetCanchaByIdAsync(int id)
        {
            return await _context.Canchas.FindAsync(id);
        }

        // POST: Crear cancha
        public async Task<Cancha> CreateCanchaAsync(CanchaDto dto)
        {
            ValidarCancha(dto);

            var cancha = new Cancha
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
                PrecioHora = dto.PrecioHora,
                Estado = dto.Estado,
                CantidadJugadores = dto.CantidadJugadores
            };

            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();
            return cancha;
        }

        // PUT: Actualizar cancha
        public async Task<Cancha?> UpdateCanchaAsync(int id, CanchaDto dto)
        {
            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha == null) return null;

            ValidarCancha(dto);

            cancha.Nombre = dto.Nombre.Trim();
            cancha.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
            cancha.PrecioHora = dto.PrecioHora;
            cancha.Estado = dto.Estado;
            cancha.CantidadJugadores = dto.CantidadJugadores;

            await _context.SaveChangesAsync();
            return cancha;
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
        }

        // PATCH: Cambiar estado (Activa / Inactiva)
        public async Task<Cancha?> ChangeCanchaStatusAsync(int id)
        {
            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha == null) return null;

            cancha.Estado = !cancha.Estado;
            await _context.SaveChangesAsync();
            return cancha;
        }
    }
}

