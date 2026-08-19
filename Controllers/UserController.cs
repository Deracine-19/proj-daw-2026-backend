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

        // GET: api/usuarios/5 (Solo Administrador — para ver/editar los datos de OTRO usuario.
        // El propio usuario ve su perfil vía GET api/usuarios/perfil, más abajo.)
        [HttpGet("{id}")]
        [Authorize(Roles = RolesConstantes.Administrador)]
        public async Task<ActionResult<UsuarioDto>> GetById(int id)
        {
            var usuario = await _usuarioService.GetById(id);
            return usuario != null ? Ok(usuario) : NotFound();
        }

        // PUT: api/usuarios/5 (Solo Administrador — nombre/email/rol/foto de cualquier usuario)
        [HttpPut("{id}")]
        [Authorize(Roles = RolesConstantes.Administrador)]
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // GET: api/usuarios/perfil (Cualquier usuario autenticado — su propio perfil. Existe porque
        // el JWT solo trae email/rol, no nombre ni foto.)
        [HttpGet("perfil")]
        public async Task<ActionResult<UsuarioDto>> GetPerfil()
        {
            int usuarioId = GetUserIdFromToken();
            var usuario = await _usuarioService.GetById(usuarioId);
            return usuario != null ? Ok(usuario) : NotFound();
        }

        // PATCH: api/usuarios/perfil/foto (Cualquier usuario autenticado — su propia foto de perfil)
        [HttpPatch("perfil/foto")]
        public async Task<ActionResult<UsuarioDto>> ActualizarFotoPropia([FromBody] ActualizarFotoDto dto)
        {
            try
            {
                int usuarioId = GetUserIdFromToken();
                return Ok(await _usuarioService.ActualizarFotoPropiaAsync(usuarioId, dto.ImagenBase64));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
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