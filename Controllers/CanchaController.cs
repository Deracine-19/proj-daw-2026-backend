using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;

namespace proj_daw_2026_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protege por defecto con JWT
    public class CanchaController : ControllerBase
    {
        private readonly CanchaService _canchaService;

        public CanchaController(CanchaService canchaService)
        {
            _canchaService = canchaService;
        }

        // GET: api/Cancha (Cualquier usuario autenticado)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cancha>>> GetCanchas()
        {
            var canchas = await _canchaService.GetAllCanchasAsync();
            return Ok(canchas);
        }

        // GET: api/Cancha/1 (Cualquier usuario autenticado)
        [HttpGet("{id}")]
        public async Task<ActionResult<Cancha>> GetCanchaById(int id)
        {
            var cancha = await _canchaService.GetCanchaByIdAsync(id);

            if (cancha == null)
            {
                return NotFound(new { mensaje = "La cancha solicitada no existe." });
            }

            return Ok(cancha);
        }

        // POST: api/Cancha (Solo Administrador)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Cancha>> CreateCancha([FromBody] CanchaDto dto)
        {
            try
            {
                var nuevaCancha = await _canchaService.CreateCanchaAsync(dto);
                return CreatedAtAction(nameof(GetCanchaById), new { id = nuevaCancha.Id }, nuevaCancha);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // PUT: api/Cancha/1 (Solo Administrador)
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateCancha(int id, [FromBody] CanchaDto dto)
        {
            try
            {
                var canchaActualizada = await _canchaService.UpdateCanchaAsync(id, dto);

                if (canchaActualizada == null)
                {
                    return NotFound(new { mensaje = "La cancha a actualizar no existe." });
                }

                return Ok(canchaActualizada);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // PATCH: api/Cancha/1/status (Solo Administrador)
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ChangeCanchaStatus(int id)
        {
            var canchaActualizada = await _canchaService.ChangeCanchaStatusAsync(id);

            if (canchaActualizada == null)
            {
                return NotFound(new { mensaje = "La cancha no existe." });
            }

            return Ok(canchaActualizada);
        }
    }
}