using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;

namespace proj_daw_2026_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponseDto>> Register([FromBody] RegisterDto dto)
        {
            var usuario = await _authService.Register(dto);

            var respuesta = new RegisterResponseDto
            {
                Id = usuario.Id,
                Email = usuario.Email
            };

            return Created("", respuesta);
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authService.Login(dto);
                return Ok(new LoginResponseDto { Token = token });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Credenciales inválidas");
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { mensaje = ex.Message });
            }
        }

        // POST: api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPassword(dto);

            // Por seguridad (estándar OWASP), siempre respondemos OK aunque el email no exista,
            // para evitar que atacantes descubran qué correos están registrados.
            return Ok(new { mensaje = "Si el correo existe en nuestro sistema, hemos enviado una contraseña temporal." });
        }

        // POST: api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var (exito, mensaje) = await _authService.ResetPassword(dto);

            if (!exito)
            {
                return BadRequest(new { mensaje });
            }

            return Ok(new { mensaje });
        }
    }

    // DTOs auxiliares para documentar las respuestas en Swagger
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public class RegisterResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}