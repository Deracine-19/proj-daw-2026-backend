namespace proj_daw_2026_backend.Data.Entities
{
    public class Articulo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public bool Estado { get; set; }
        public ICollection<ReservaArticulo> ReservaArticulos { get; set; } = new List<ReservaArticulo>();

        // Data URI completo ("data:image/png;base64,....") — se guarda directo en la BD
        // para no depender de un bucket/almacenamiento externo.
        public string? ImagenBase64 { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastEditedDate { get; set; }
    }
}
