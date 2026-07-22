namespace proj_daw_2026_backend.DTOs
{
    // DTO para enviar la petición de creación de una reserva
    public class CreateReservaDto
    {
        public int CanchaId { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeSpan HoraEntrada { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public List<CreateReservaArticuloDto>? Articulos { get; set; }
    }

    public class CreateReservaArticuloDto
    {
        public int ArticuloId { get; set; }
        public int Cantidad { get; set; }
    }

    // DTO para la respuesta de la reserva
    public class ReservaReadDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string? NombreUsuario { get; set; }
        public int CanchaId { get; set; }
        public string? NombreCancha { get; set; }
        public DateOnly Fecha { get; set; }
        public TimeSpan HoraEntrada { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public string CodigoReserva { get; set; } = string.Empty;
        public string EstadoReserva { get; set; } = string.Empty;
        public bool EstadoPago { get; set; }
        public decimal PrecioAplicado { get; set; }
        public decimal Total { get; set; }
        public List<ReservaArticuloReadDto> Articulos { get; set; } = new();
    }

    public class ReservaArticuloReadDto
    {
        public int ArticuloId { get; set; }
        public string? NombreArticulo { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}