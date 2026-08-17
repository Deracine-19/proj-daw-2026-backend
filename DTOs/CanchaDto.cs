using System.ComponentModel.DataAnnotations;

namespace proj_daw_2026_backend.DTOs
{
    public class CanchaDto
    {
        [Required(ErrorMessage = "El nombre de la cancha es obligatorio.")]
        [StringLength(80, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 80 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La descripción no puede exceder los 250 caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio por hora es obligatorio.")]
        [Range(0.01, 10000.00, ErrorMessage = "El precio por hora debe estar entre L 0.01 y L 10,000.00.")]
        public decimal PrecioHora { get; set; }

        [Required(ErrorMessage = "El estado de la cancha es obligatorio.")]
        public bool Estado { get; set; }

        [Required(ErrorMessage = "La cantidad de jugadores es obligatoria.")]
        [Range(1, 50, ErrorMessage = "La cantidad de jugadores debe ser entre 1 y 50.")]
        public int CantidadJugadores { get; set; }

        public string? ImagenBase64 { get; set; }
    }

    public class CanchaReadDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioHora { get; set; }
        public bool Estado { get; set; }
        public int CantidadJugadores { get; set; }
        public string? ImagenBase64 { get; set; }
    }
}