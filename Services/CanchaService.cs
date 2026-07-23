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
            var cancha = new Cancha
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
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

            cancha.Nombre = dto.Nombre;
            cancha.Descripcion = dto.Descripcion;
            cancha.PrecioHora = dto.PrecioHora;
            cancha.Estado = dto.Estado;
            cancha.CantidadJugadores = dto.CantidadJugadores;

            await _context.SaveChangesAsync();
            return cancha;
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

