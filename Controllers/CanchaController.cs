using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.Data;

namespace SistemaReservaciones.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protege todos los endpoints con JWT
    public class CanchaController : ControllerBase
    {
        private readonly AppDBContext _context;

        public CanchaController(AppDBContext context)
        {
            _context = context;
        }

        // GET: api/Cancha (Cualquier usuario autenticado puede ver las canchas)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cancha>>> GetCanchas()
        {
            return await _context.Canchas.ToListAsync();
        }

        // GET: api/Cancha/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Cancha>> GetCancha(int id)
        {
            var cancha = await _context.Canchas.FindAsync(id);

            if (cancha == null)
            {
                return NotFound("La cancha solicitada no existe.");
            }

            return cancha;
        }

        // POST: api/Cancha (Solo usuarios con Rol 'Admin' deberían crear canchas)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Cancha>> CreateCancha([FromBody] Cancha cancha)
        {
            _context.Canchas.Add(cancha);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCancha), new { id = cancha.Id }, cancha);
        }
    }
}