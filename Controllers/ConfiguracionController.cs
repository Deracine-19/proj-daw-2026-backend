using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;

namespace proj_daw_2026_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Protege por defecto; GET se abre explícitamente con [AllowAnonymous] abajo.
    public class ConfiguracionController : ControllerBase
    {
        private readonly IConfiguracionService _configuracionService;

        public ConfiguracionController(IConfiguracionService configuracionService)
        {
            _configuracionService = configuracionService;
        }

        // GET: api/configuracion (público — el nombre del negocio se muestra en el login,
        // antes de que exista un token; el horario también lo necesita cualquier usuario
        // autenticado o no para saber cuándo puede reservar)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ConfiguracionReadDto>> GetConfiguracion()
        {
            return Ok(await _configuracionService.GetConfiguracionAsync());
        }

        // PUT: api/configuracion (Solo Administrador)
        [HttpPut]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ConfiguracionReadDto>> UpdateConfiguracion([FromBody] ConfiguracionDto dto)
        {
            try
            {
                var actualizada = await _configuracionService.UpdateConfiguracionAsync(dto);
                return Ok(actualizada);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }
    }
}
