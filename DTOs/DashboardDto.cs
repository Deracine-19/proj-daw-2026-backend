namespace proj_daw_2026_backend.DTOs
{
    public class DashboardDto
    {
        public int ReservasHoy { get; set; }

        public decimal IngresosHoy { get; set; }

        public int UsuariosActivos { get; set; }

        public int CanchasActivas { get; set; }

        public int ReservasPendientesPago { get; set; }

        public int NoShowsMes { get; set; }

        public string? CanchaMasReservada { get; set; }

        public List<DashboardReservaDto> ReservasRecientes { get; set; }
            = new();
    }

    public class DashboardReservaDto
    {
        public int Id { get; set; }

        public string CodigoReserva { get; set; } = string.Empty;

        public string Cliente { get; set; } = string.Empty;

        public string Cancha { get; set; } = string.Empty;

        public DateOnly Fecha { get; set; }

        public TimeSpan HoraEntrada { get; set; }

        public decimal Total { get; set; }

        public string EstadoReserva { get; set; } = string.Empty;

        public bool EstadoPago { get; set; }
    }
}