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
    }
}
