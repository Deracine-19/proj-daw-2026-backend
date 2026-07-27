using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public interface IArticuloService
    {
        Task<IEnumerable<Articulo>> GetAllArticulos();
        Task<Articulo?> GetArticuloById(int id);
        Task<Articulo> CreateArticulo(ArticuloCreateDto dto);
        Task<Articulo?> UpdateArticulo(int id, ArticuloUpdateDto dto);
        Task ChangeArticuloStatus(int id);
    }

    
    public class ArticuloService : IArticuloService
    {
        private readonly AppDBContext _context;

        public ArticuloService(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Articulo>> GetAllArticulos()
        {
            
            return await _context.Articulos.ToListAsync();
        }

        public async Task<Articulo?> GetArticuloById(int id)
        {
            
            return await _context.Articulos.FindAsync(id);
        }

        public async Task<Articulo> CreateArticulo(ArticuloCreateDto dto)
        {
            var nuevoArticulo = new Articulo
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                Estado = true 
            };

            _context.Articulos.Add(nuevoArticulo);
            await _context.SaveChangesAsync();
            return nuevoArticulo;
        }

        public async Task<Articulo?> UpdateArticulo(int id, ArticuloUpdateDto dto)
        {
            var articulo = await _context.Articulos.FindAsync(id);

            if (articulo == null)
                return null;

            
            articulo.Nombre = dto.Nombre;
            articulo.Descripcion = dto.Descripcion;
            articulo.Precio = dto.Precio;
            articulo.Estado = dto.Estado;

            await _context.SaveChangesAsync();
            return articulo;
        }

        public async Task ChangeArticuloStatus(int id)
        {
            var articulo = await _context.Articulos.FindAsync(id);

            if (articulo != null)
            {
                
                articulo.Estado = !articulo.Estado;
                await _context.SaveChangesAsync();
            }
        }
    }
}