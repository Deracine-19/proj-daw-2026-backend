using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public class ReservaService
    {
        private readonly AppDBContext _context;

        public ReservaService(AppDBContext context)
        {
            _context = context;
        }

        // GET: Obtener todas las reservas
        public async Task<List<ReservaReadDto>> GetAllReservasAsync()
        {
            var reservas = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .Include(r => r.ReservaArticulos)
                    .ThenInclude(ra => ra.Articulo)
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.HoraEntrada)
                .ToListAsync();

            return reservas.Select(MapToReadDto).ToList();
        }

        // GET: Obtener reserva por ID
        public async Task<ReservaReadDto?> GetReservaByIdAsync(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .Include(r => r.ReservaArticulos)
                    .ThenInclude(ra => ra.Articulo)
                .FirstOrDefaultAsync(r => r.Id == id);

            return reserva == null ? null : MapToReadDto(reserva);
        }

        // GET: Obtener reservas de un usuario específico
        public async Task<List<ReservaReadDto>> GetReservasByUsuarioIdAsync(int usuarioId)
        {
            var reservas = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .Include(r => r.ReservaArticulos)
                    .ThenInclude(ra => ra.Articulo)
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();

            return reservas.Select(MapToReadDto).ToList();
        }

        // POST: Crear Reserva
        public async Task<ReservaReadDto> CreateReservaAsync(int usuarioId, CreateReservaDto dto)
        {
            // 1. Validar horarios
            if (dto.HoraSalida <= dto.HoraEntrada)
            {
                throw new InvalidOperationException("La hora de salida debe ser posterior a la hora de entrada.");
            }

            // 2. Validar existencia de la cancha
            var cancha = await _context.Canchas.FindAsync(dto.CanchaId);
            if (cancha == null)
            {
                throw new KeyNotFoundException("La cancha especificada no existe.");
            }

            // 3. Validar traslape/solapamiento de horarios en la misma cancha
            bool yaReservado = await _context.Reservas.AnyAsync(r =>
                r.CanchaId == dto.CanchaId &&
                r.Fecha == dto.Fecha &&
                r.EstadoReserva != "CANCELADA" &&
                ((dto.HoraEntrada >= r.HoraEntrada && dto.HoraEntrada < r.HoraSalida) ||
                 (dto.HoraSalida > r.HoraEntrada && dto.HoraSalida <= r.HoraSalida) ||
                 (dto.HoraEntrada <= r.HoraEntrada && dto.HoraSalida >= r.HoraSalida))
            );

            if (yaReservado)
            {
                throw new InvalidOperationException("La cancha ya se encuentra reservada en el horario seleccionado.");
            }

            // 4. Calcular precio de la cancha según horas (usando PrecioHora)
            double horas = (dto.HoraSalida - dto.HoraEntrada).TotalHours;
            decimal totalCancha = cancha.PrecioHora * (decimal)horas;
            decimal totalArticulos = 0;

            var reservaArticulos = new List<ReservaArticulo>();

            // 5. Procesar artículos si vienen en el DTO (usando Precio)
            if (dto.Articulos != null && dto.Articulos.Any())
            {
                foreach (var item in dto.Articulos)
                {
                    var articulo = await _context.Articulos.FindAsync(item.ArticuloId);
                    if (articulo == null)
                    {
                        throw new KeyNotFoundException($"El artículo con ID {item.ArticuloId} no existe.");
                    }

                    decimal subtotalArticulo = articulo.Precio * item.Cantidad;
                    totalArticulos += subtotalArticulo;

                    reservaArticulos.Add(new ReservaArticulo
                    {
                        ArticuloId = item.ArticuloId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = articulo.Precio
                    });
                }
            }

            // 6. Crear la entidad Reserva
            var reserva = new Reserva
            {
                UsuarioId = usuarioId,
                CanchaId = dto.CanchaId,
                Fecha = dto.Fecha,
                HoraEntrada = dto.HoraEntrada,
                HoraSalida = dto.HoraSalida,
                CodigoReserva = await GenerarCodigoReservaAsync(),
                EstadoReserva = "CONFIRMADA",
                EstadoPago = false,
                PrecioAplicado = cancha.PrecioHora,
                Total = totalCancha + totalArticulos,
                ReservaArticulos = reservaArticulos
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return (await GetReservaByIdAsync(reserva.Id))!;
        }

        // PATCH/PUT: Cancelar Reserva
        public async Task<bool> CancelarReservaAsync(int id, int usuarioId, bool esAdmin)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return false;

            // Un cliente solo puede cancelar sus propias reservas
            if (!esAdmin && reserva.UsuarioId != usuarioId)
            {
                throw new UnauthorizedAccessException("No tienes permiso para cancelar esta reserva.");
            }

            reserva.EstadoReserva = "CANCELADA";
            await _context.SaveChangesAsync();
            return true;
        }

        // Helper para Mapeo
        private static ReservaReadDto MapToReadDto(Reserva r)
        {
            return new ReservaReadDto
            {
                Id = r.Id,
                UsuarioId = r.UsuarioId,
                NombreUsuario = r.Usuario?.Nombre,
                CanchaId = r.CanchaId,
                NombreCancha = r.Cancha?.Nombre,
                Fecha = r.Fecha,
                HoraEntrada = r.HoraEntrada,
                HoraSalida = r.HoraSalida,
                CodigoReserva = r.CodigoReserva,
                EstadoReserva = r.EstadoReserva,
                EstadoPago = r.EstadoPago,
                PrecioAplicado = r.PrecioAplicado,
                Total = r.Total,
                Articulos = r.ReservaArticulos?.Select(ra => new ReservaArticuloReadDto
                {
                    ArticuloId = ra.ArticuloId,
                    NombreArticulo = ra.Articulo?.Nombre,
                    Cantidad = ra.Cantidad,
                    PrecioUnitario = ra.PrecioUnitario
                }).ToList() ?? new()
            };
        }

        private const string CaracteresCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private async Task<string> GenerarCodigoReservaAsync()
        {
            string codigo;
            bool existe;

            do
            {
                codigo = new string(Enumerable.Range(0, 5)
                    .Select(_ => CaracteresCodigo[Random.Shared.Next(CaracteresCodigo.Length)])
                    .ToArray());

                existe = await _context.Reservas.AnyAsync(r => r.CodigoReserva == codigo);
            } while (existe);

            return codigo;
        }

        // PATCH: Marcar como pagada (check-in del operador/administrador)
        public async Task<ReservaReadDto?> MarcarComoPagadaAsync(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return null;

            if (reserva.EstadoReserva == "CANCELADA")
                throw new InvalidOperationException("No se puede marcar como pagada una reserva cancelada.");

            if (reserva.EstadoReserva == "NOSHOW")
                throw new InvalidOperationException("No se puede marcar como pagada una reserva marcada como No-Show.");

            if (reserva.EstadoPago)
                throw new InvalidOperationException("Esta reserva ya está marcada como pagada.");

            reserva.EstadoPago = true;
            await _context.SaveChangesAsync();

            return await GetReservaByIdAsync(id);
        }

        // PATCH: Marcar como No-Show (el cliente no se presentó)
        public async Task<ReservaReadDto?> MarcarComoNoShowAsync(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return null;

            if (reserva.EstadoReserva == "CANCELADA")
                throw new InvalidOperationException("No se puede marcar como No-Show una reserva cancelada.");

            if (reserva.EstadoPago)
                throw new InvalidOperationException("No se puede marcar como No-Show una reserva ya pagada.");

            var fechaHoraReserva = reserva.Fecha.ToDateTime(TimeOnly.FromTimeSpan(reserva.HoraEntrada));
            if (fechaHoraReserva > DateTime.Now)
                throw new InvalidOperationException("No se puede marcar como No-Show una reserva que todavía no ha ocurrido.");

            reserva.EstadoReserva = "NOSHOW";
            await _context.SaveChangesAsync();

            return await GetReservaByIdAsync(id);
        }
    }
}