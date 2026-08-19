using System.ComponentModel.DataAnnotations;

namespace proj_daw_2026_backend.DTOs
{
    public class ArticuloCreateDto
    {
        [Required(ErrorMessage = "El nombre del artículo es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La descripción no puede exceder los 250 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, 10000.00, ErrorMessage = "El precio debe ser mayor a L 0.00 y menor a L 10,000.00.")]
        public decimal Precio { get; set; }

        // Data URI ("data:image/png;base64,...") generado por el frontend
        public string? ImagenBase64 { get; set; }
    }

    public class ArticuloUpdateDto
    {
        [Required(ErrorMessage = "El nombre del artículo es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La descripción no puede exceder los 250 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, 10000.00, ErrorMessage = "El precio debe ser mayor a L 0.00 y menor a L 10,000.00.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El estado del artículo es obligatorio.")]
        public bool Estado { get; set; }

        public string? ImagenBase64 { get; set; }
    }

    public class ArticuloReadDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public bool Estado { get; set; }
        public string? ImagenBase64 { get; set; }
    }
}