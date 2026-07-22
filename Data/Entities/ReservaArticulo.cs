namespace proj_daw_2026_backend.Data.Entities
{
    public class ReservaArticulo
    {
        public int Id { get; set; }
        public int ReservaId { get; set; }
        public int ArticuloId { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }

        public Reserva Reserva { get; set; } = null!;
        public Articulo Articulo { get; set; } = null!;
    }
}
