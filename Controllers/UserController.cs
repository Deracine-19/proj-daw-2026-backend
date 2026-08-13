using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;
using System.Security.Claims;

namespace proj_daw_2026_backend.Controllers
{
    [Route("api/usuarios")]
    [ApiController]
    [Authorize]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuarioController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // GET: api/usuarios?page=1&pageSize=20&busqueda=&ordenarPor=&ordenDireccion=&rol=&activo= (Solo Administrador)
        [HttpGet]
        [Authorize(Roles = RolesConstantes.Administrador)]
        public async Task<ActionResult<PagedResultDto<UsuarioDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? busqueda = null,
            [FromQuery] string? ordenarPor = null,
            [FromQuery] string? ordenDireccion = null,
            [FromQuery] string? rol = null,
            [FromQuery] bool? activo = null)
            => Ok(await _usuarioService.GetAll(page, pageSize, busqueda, ordenarPor, ordenDireccion, rol, activo));

        // GET: api/usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioDto>> GetById(int id)
        {
            var usuario = await _usuarioService.GetById(id);
            return usuario != null ? Ok(usuario) : NotFound();
        }

        // PUT: api/usuarios/5
        [HttpPut("{id}")]
        public async Task<ActionResult<UsuarioDto>> Update(int id, [FromBody] UsuarioUpdateDto dto)
        {
            try
            {
                return Ok(await _usuarioService.Update(id, dto));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // POST: api/usuarios (Solo Administrador)
        [HttpPost]
        [Authorize(Roles = RolesConstantes.Administrador)]
        public async Task<ActionResult<UsuarioDto>> Create([FromBody] UsuarioCreateDto dto)
        {
            var usuario = await _usuarioService.CreateUser(dto);
            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
        }

        // PATCH: api/usuarios/5/estado (Solo Administrador)
        [HttpPatch("{id}/estado")]
        [Authorize(Roles = RolesConstantes.Administrador)]
        public async Task<ActionResult<UsuarioDto>> CambiarEstado(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                return Ok(await _usuarioService.ChangeUserStatus(id, currentUserId));
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
    }
}