using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.Data.Entities;
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

        // GET: api/articulo?page=1&pageSize=20&busqueda=&ordenarPor=&ordenDireccion= (cualquier autenticado)
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ArticuloReadDto>>> GetAllArticulos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? busqueda = null,
            [FromQuery] string? ordenarPor = null,
            [FromQuery] string? ordenDireccion = null)
        {
            var articulos = await _articuloService.GetAllArticulos(page, pageSize, busqueda, ordenarPor, ordenDireccion);
            return Ok(articulos);
        }

        // GET: api/articulo/5 (cualquier autenticado)
        [HttpGet("{id}")]
        public async Task<ActionResult<Articulo>> GetArticuloById(int id)
        {
            var articulo = await _articuloService.GetArticuloById(id);
            if (articulo == null)
                return NotFound(new { mensaje = "Artículo no encontrado." });

            return Ok(articulo);
        }

        // POST: api/articulo (Solo Administrador)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Articulo>> CreateArticulo([FromBody] ArticuloCreateDto dto)
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
        public async Task<ActionResult<Articulo>> UpdateArticulo(int id, [FromBody] ArticuloUpdateDto dto)
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
        public async Task<ActionResult<Articulo>> ChangeArticuloStatus(int id)
        {
            var articuloActualizado = await _articuloService.ChangeArticuloStatus(id);
            if (articuloActualizado == null)
                return NotFound(new { mensaje = "Artículo no encontrado." });

            return Ok(articuloActualizado);
        }
    }
}