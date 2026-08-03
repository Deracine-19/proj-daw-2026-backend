using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;

namespace proj_daw_2026_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere autenticación JWT para todos los endpoints
    public class ArticuloController : ControllerBase
    {
        private readonly IArticuloService _articuloService;

        public ArticuloController(IArticuloService articuloService)
        {
            _articuloService = articuloService;
        }

        // GET: api/articulo (cualquier autenticado)
        [HttpGet]
        public async Task<IActionResult> GetAllArticulos()
        {
            var articulos = await _articuloService.GetAllArticulos();
            return Ok(articulos);
        }

        // GET: api/articulo/5 (cualquier autenticado)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetArticuloById(int id)
        {
            var articulo = await _articuloService.GetArticuloById(id);
            if (articulo == null)
                return NotFound(new { mensaje = "Artículo no encontrado." });

            return Ok(articulo);
        }

        // POST: api/articulo (Solo Administrador)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateArticulo([FromBody] ArticuloCreateDto dto)
        {
            try
            {
                var nuevoArticulo = await _articuloService.CreateArticulo(dto);
                return CreatedAtAction(nameof(GetArticuloById), new { id = nuevoArticulo.Id }, nuevoArticulo);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // PUT: api/articulo/5 (Solo Administrador)
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateArticulo(int id, [FromBody] ArticuloUpdateDto dto)
        {
            try
            {
                var articuloActualizado = await _articuloService.UpdateArticulo(id, dto);
                if (articuloActualizado == null)
                    return NotFound(new { mensaje = "Artículo no encontrado." });

                return Ok(articuloActualizado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        // PATCH: api/articulo/5/status (Solo Administrador)
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ChangeArticuloStatus(int id)
        {
            var articuloActualizado = await _articuloService.ChangeArticuloStatus(id);
            if (articuloActualizado == null)
                return NotFound(new { mensaje = "Artículo no encontrado." });

            return Ok(articuloActualizado);
        }
    }
}
