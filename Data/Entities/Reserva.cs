namespace proj_daw_2026_backend.Data.Entities;

public class Reserva
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int CanchaId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeSpan HoraEntrada { get; set; }
    public TimeSpan HoraSalida { get; set; }
    public string CodigoReserva { get; set; } = string.Empty;
    public string EstadoReserva { get; set; } = string.Empty;
    public bool EstadoPago { get; set; }
    public decimal PrecioAplicado { get; set; } 
    public decimal Total { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow; 
    public DateTime? LastEditedDate { get; set; } 

    public Usuario Usuario { get; set; } = null!;
    public Cancha Cancha { get; set; } = null!;
    public ICollection<ReservaArticulo> ReservaArticulos { get; set; } = new List<ReservaArticulo>();
}
