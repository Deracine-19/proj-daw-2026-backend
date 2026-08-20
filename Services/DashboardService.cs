using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public class DashboardService
    {
        private readonly AppDBContext _context;

        public DashboardService(AppDBContext context)
        {
            _context = context;
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var ahora = HoraNegocio.Ahora;
            var hoy = DateOnly.FromDateTime(ahora);

            var inicioMes = new DateOnly(
                ahora.Year,
                ahora.Month,
                1
            );

            var finMes = inicioMes.AddMonths(1);

            // Reservas programadas para hoy,
            // excluyendo las canceladas.
            var reservasHoy = await _context.Reservas
                .CountAsync(r =>
                    r.Fecha == hoy &&
                    r.EstadoReserva != "CANCELADA"
                );

            // Solo dinero efectivamente marcado como pagado hoy.
            var ingresosHoy = await _context.Reservas
                .Where(r =>
                    r.Fecha == hoy &&
                    r.EstadoPago
                )
                .SumAsync(r => (decimal?)r.Total) ?? 0;

            var usuariosActivos = await _context.Usuarios
                .CountAsync(u => u.Activo);

            var canchasActivas = await _context.Canchas
                .CountAsync(c => c.Estado);

            var reservasPendientesPago =
                await _context.Reservas
                    .CountAsync(r =>
                        !r.EstadoPago &&
                        r.EstadoReserva == "CONFIRMADA"
                    );

            var noShowsMes = await _context.Reservas
                .CountAsync(r =>
                    r.EstadoReserva == "NOSHOW" &&
                    r.Fecha >= inicioMes &&
                    r.Fecha < finMes
                );

            // Cancha con más reservas confirmadas/no canceladas.
            var canchaMasReservada = await _context.Reservas
                .Where(r => r.EstadoReserva != "CANCELADA")
                .GroupBy(r => new
                {
                    r.CanchaId,
                    r.Cancha.Nombre
                })
                .Select(g => new
                {
                    g.Key.Nombre,
                    Total = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .Select(x => x.Nombre)
                .FirstOrDefaultAsync();

            var reservasRecientes = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .OrderByDescending(r => r.CreatedDate)
                .Take(5)
                .Select(r => new DashboardReservaDto
                {
                    Id = r.Id,
                    CodigoReserva = r.CodigoReserva,
                    Cliente = r.Usuario.Nombre,
                    Cancha = r.Cancha.Nombre,
                    Fecha = r.Fecha,
                    HoraEntrada = r.HoraEntrada,
                    Total = r.Total,
                    EstadoReserva = r.EstadoReserva,
                    EstadoPago = r.EstadoPago
                })
                .ToListAsync();

            return new DashboardDto
            {
                ReservasHoy = reservasHoy,
                IngresosHoy = ingresosHoy,
                UsuariosActivos = usuariosActivos,
                CanchasActivas = canchasActivas,
                ReservasPendientesPago = reservasPendientesPago,
                NoShowsMes = noShowsMes,
                CanchaMasReservada =
                    canchaMasReservada ?? "Sin datos",
                ReservasRecientes = reservasRecientes
            };
        }
    }
}