namespace proj_daw_2026_backend.DTOs
{
    public class CanchaDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioHora { get; set; }
        public bool Estado { get; set; }
        public int CantidadJugadores { get; set; }
    }
}