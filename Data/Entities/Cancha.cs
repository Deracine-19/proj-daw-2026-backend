namespace proj_daw_2026_backend.Data.Entities;

public class Cancha
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal PrecioHora { get; set; }
    public bool Estado { get; set; }
    public int CantidadJugadores { get; set; }
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
