namespace proj_daw_2026_backend.DTOs
{
    public class ArticuloCreateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }

        // Data URI ("data:image/png;base64,...") generado por el frontend a partir del archivo elegido.
        public string? ImagenBase64 { get; set; }
    }

    public class ArticuloUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
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
