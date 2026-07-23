using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;

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

        [HttpGet]
        [Authorize(Roles = RolesConstantes.Administrador)]
        public async Task<IActionResult> GetAll() => Ok(await _usuarioService.GetAll());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var usuario = await _usuarioService.GetById(id);
            return usuario != null ? Ok(usuario) : NotFound();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDto dto)
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

        [HttpPost]
        [Authorize(Roles = RolesConstantes.Administrador)]
        public async Task<IActionResult> Create([FromBody] UsuarioCreateDto dto)
        {
            var usuario = await _usuarioService.CreateUser(dto);
            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario);
        }

        [HttpPatch("{id}/estado")]
        [Authorize(Roles = RolesConstantes.Administrador)]
        public async Task<IActionResult> ChangeUserStatus(int id)
        {
            try
            {
                return Ok(await _usuarioService.ChangeUserStatus(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}