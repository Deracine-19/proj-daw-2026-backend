using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;

namespace proj_daw_2026_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todos los endpoints
    public class ReservaController : ControllerBase
    {
        private readonly ReservaService _reservaService;

        public ReservaController(ReservaService reservaService)
        {
            _reservaService = reservaService;
        }

        // GET: api/reserva (Solo Admin)
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetAll()
        {
            var reservas = await _reservaService.GetAllReservasAsync();
            return Ok(reservas);
        }

        // GET: api/reserva/mis-reservas (Cliente / Admin)
        [HttpGet("mis-reservas")]
        public async Task<IActionResult> GetMisReservas()
        {
            int usuarioId = GetUserIdFromToken();
            var reservas = await _reservaService.GetReservasByUsuarioIdAsync(usuarioId);
            return Ok(reservas);
        }

        // GET: api/reserva/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var reserva = await _reservaService.GetReservaByIdAsync(id);
            if (reserva == null) return NotFound("Reserva no encontrada.");

            int usuarioId = GetUserIdFromToken();
            bool esAdmin = User.IsInRole("Administrador");

            // Si no es Admin y la reserva no le pertenece, denegar acceso
            if (!esAdmin && reserva.UsuarioId != usuarioId)
            {
                return Forbid();
            }

            return Ok(reserva);
        }

        // POST: api/reserva
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservaDto dto)
        {
            try
            {
                int usuarioId = GetUserIdFromToken();
                var reserva = await _reservaService.CreateReservaAsync(usuarioId, dto);
                return CreatedAtAction(nameof(GetById), new { id = reserva.Id }, reserva);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PATCH: api/reserva/{id}/cancelar
        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                int usuarioId = GetUserIdFromToken();
                bool esAdmin = User.IsInRole("Administrador");

                bool cancelada = await _reservaService.CancelarReservaAsync(id, usuarioId, esAdmin);
                if (!cancelada) return NotFound("Reserva no encontrada.");

                return Ok(new { mensaje = "Reserva cancelada exitosamente." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
        }

        // Helper para extraer el ID del usuario autenticado mediante el Token JWT
        private int GetUserIdFromToken()
        {
            var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(nameIdentifierClaim, out int usuarioId))
            {
                return usuarioId;
            }
            throw new UnauthorizedAccessException("Usuario no válido en el Token.");
        }
    }
}