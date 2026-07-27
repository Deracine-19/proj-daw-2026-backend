using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.DTOs;
using proj_daw_2026_backend.Services;

namespace proj_daw_2026_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticuloController : ControllerBase
    {
        private readonly IArticuloService _articuloService;

        public ArticuloController(IArticuloService articuloService)
        {
            _articuloService = articuloService;
        }

        // GET: api/articulo
        [HttpGet]
        public async Task<IActionResult> GetAllArticulos()
        {
            var articulos = await _articuloService.GetAllArticulos();
            return Ok(articulos);
        }

        // GET: api/articulo/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetArticuloById(int id)
        {
            var articulo = await _articuloService.GetArticuloById(id);
            if (articulo == null)
                return NotFound(new { message = "Artículo no encontrado." });

            return Ok(articulo);
        }

        // POST: api/articulo
        [HttpPost]
        public async Task<IActionResult> CreateArticulo([FromBody] ArticuloCreateDto dto)
        {
            var nuevoArticulo = await _articuloService.CreateArticulo(dto);
            return CreatedAtAction(nameof(GetArticuloById), new { id = nuevoArticulo.Id }, nuevoArticulo);
        }

        // PUT: api/articulo/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticulo(int id, [FromBody] ArticuloUpdateDto dto)
        {
            var articuloActualizado = await _articuloService.UpdateArticulo(id, dto);
            if (articuloActualizado == null)
                return NotFound(new { message = "Artículo no encontrado." });

            return Ok(articuloActualizado);
        }

        // PATCH: api/articulo/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeArticuloStatus(int id)
        {
            await _articuloService.ChangeArticuloStatus(id);
            return NoContent(); // 204 No Content es el estándar para un patch exitoso sin devolver datos
        }
    }
}