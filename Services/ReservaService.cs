using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public class ReservaService
    {
        private readonly AppDBContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<ReservaService> _logger;
        private readonly IConfiguracionService _configuracionService;

        public ReservaService(
            AppDBContext context, EmailService emailService, ILogger<ReservaService> logger,
            IConfiguracionService configuracionService)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _configuracionService = configuracionService;
        }

        // GET: Obtener reservas paginadas
        public async Task<PagedResultDto<ReservaReadDto>> GetAllReservasAsync(
            int page, int pageSize, string? busqueda, string? ordenarPor, string? ordenDireccion,
            DateOnly? fechaInicio, DateOnly? fechaFin, string? estado)
        {
            (page, pageSize) = PaginacionHelper.Normalizar(page, pageSize);

            // A diferencia del reporte de exportación, el listado paginado sí necesita un rango
            // por defecto ("hoy") para no traer la tabla completa cuando no se pide ningún filtro.
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var desde = fechaInicio ?? hoy;
            var hasta = fechaFin ?? hoy;

            var query = ConstruirConsultaReservas(busqueda, ordenarPor, ordenDireccion, desde, hasta, estado);

            var totalCount = await query.CountAsync();
            var reservas = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<ReservaReadDto>
            {
                Items = reservas.Select(MapToReadDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
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

        // GET: Horarios ya ocupados de una cancha en una fecha (incluye horas transcurridas si es HOY)
        public async Task<List<HorarioOcupadoDto>> GetHorariosOcupadosAsync(int canchaId, DateOnly fecha)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var horario = await _configuracionService.GetConfiguracionAsync();

            // Si se consulta un día pasado, todo el día se marca como ocupado/bloqueado
            if (fecha < hoy)
            {
                return new List<HorarioOcupadoDto>
                {
                    new HorarioOcupadoDto
                    {
                        HoraEntrada = horario.HoraApertura,
                        HoraSalida = horario.HoraCierre
                    }
                };
            }

            var ocupados = await _context.Reservas
                .Where(r => r.CanchaId == canchaId && r.Fecha == fecha && r.EstadoReserva != "CANCELADA")
                .Select(r => new HorarioOcupadoDto { HoraEntrada = r.HoraEntrada, HoraSalida = r.HoraSalida })
                .ToListAsync();

            // Si es la fecha actual, bloqueamos desde la hora de apertura hasta la hora actual
            if (fecha == hoy)
            {
                var horaActual = DateTime.Now.TimeOfDay;

                if (horaActual > horario.HoraApertura)
                {
                    ocupados.Add(new HorarioOcupadoDto
                    {
                        HoraEntrada = horario.HoraApertura,
                        HoraSalida = horaActual < horario.HoraCierre ? horaActual : horario.HoraCierre
                    });
                }
            }

            return ocupados;
        }

        // POST: Crear Reserva
        public async Task<ReservaReadDto> CreateReservaAsync(int usuarioId, CreateReservaDto dto)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var horaActual = DateTime.Now.TimeOfDay;

            // 1. Validaciones de Fecha y Hora Transcurridas
            if (dto.Fecha < hoy)
            {
                throw new InvalidOperationException("No se pueden realizar reservas para fechas pasadas.");
            }

            if (dto.Fecha == hoy && dto.HoraEntrada <= horaActual)
            {
                throw new InvalidOperationException("No se pueden realizar reservas para horarios que ya transcurrieron.");
            }

            // 2. Validar orden de horarios de entrada/salida
            if (dto.HoraSalida <= dto.HoraEntrada)
            {
                throw new InvalidOperationException("La hora de salida debe ser posterior a la hora de entrada.");
            }

            var horario = await _configuracionService.GetConfiguracionAsync();
            if (dto.HoraEntrada < horario.HoraApertura || dto.HoraSalida > horario.HoraCierre)
            {
                throw new InvalidOperationException(
                    $"El horario debe estar entre las {horario.HoraApertura:hh\\:mm} y las {horario.HoraCierre:hh\\:mm}.");
            }

            // 3. Validar existencia de la cancha
            var cancha = await _context.Canchas.FindAsync(dto.CanchaId);
            if (cancha == null)
            {
                throw new KeyNotFoundException("La cancha especificada no existe.");
            }

            // 4. Validar traslape/solapamiento de horarios en la misma cancha
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

            // 5. Calcular precio de la cancha según horas
            double horas = (dto.HoraSalida - dto.HoraEntrada).TotalHours;
            decimal totalCancha = cancha.PrecioHora * (decimal)horas;
            decimal totalArticulos = 0;

            var reservaArticulos = new List<ReservaArticulo>();

            // 6. Procesar artículos
            if (dto.Articulos != null && dto.Articulos.Any())
            {
                foreach (var item in dto.Articulos)
                {
                    var articulo = await _context.Articulos.FindAsync(item.ArticuloId);
                    if (articulo == null)
                    {
                        throw new KeyNotFoundException($"El artículo con ID {item.ArticuloId} no existe.");
                    }

                    if (item.Cantidad <= 0)
                    {
                        throw new InvalidOperationException($"La cantidad para el artículo '{articulo.Nombre}' debe ser mayor a 0.");
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

            // 7. Crear la entidad Reserva
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

            // 8. Notificación automática por correo electrónico
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null && !string.IsNullOrEmpty(usuario.Email))
            {
                string articulosHtml = "";
                if (reservaArticulos.Any())
                {
                    articulosHtml = "<h3>Artículos Alquilados:</h3><ul>";
                    foreach (var item in reservaArticulos)
                    {
                        var art = await _context.Articulos.FindAsync(item.ArticuloId);
                        articulosHtml += $"<li><strong>{art?.Nombre ?? "Artículo"}</strong> x{item.Cantidad} - L {item.PrecioUnitario * item.Cantidad:N2}</li>";
                    }
                    articulosHtml += "</ul>";
                }

                string mensajeHtml = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                        <h2 style='color: #2c3e50; text-align: center;'>¡Reserva Confirmada! ⚽</h2>
                        <p>Hola <strong>{usuario.Nombre}</strong>,</p>
                        <p>Tu reserva ha sido registrada exitosamente. A continuación se muestra el detalle de tu solicitud:</p>
                        <hr style='border: none; border-top: 1px solid #eee;' />
                        <p><strong>Código de Reserva:</strong> <span style='font-size: 18px; color: #27ae60; font-weight: bold;'>{reserva.CodigoReserva}</span></p>
                        <p><strong>Cancha:</strong> {cancha.Nombre}</p>
                        <p><strong>Fecha:</strong> {reserva.Fecha:dd/MM/yyyy}</p>
                        <p><strong>Horario:</strong> {reserva.HoraEntrada:hh\:mm} - {reserva.HoraSalida:hh\:mm}</p>
                        {articulosHtml}
                        <hr style='border: none; border-top: 1px solid #eee;' />
                        <h3 style='color: #2c3e50;'>Total a Pagar: L {reserva.Total:N2}</h3>
                        <p style='font-size: 12px; color: #7f8c8d; text-align: center; margin-top: 20px;'>¡Gracias por confiar en nuestros servicios!</p>
                    </div>";

                var destinatario = usuario.Email;
                var asunto = $"Confirmación de Reserva #{reserva.CodigoReserva}";
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEmailAsync(destinatario, asunto, mensajeHtml);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo enviar el correo de confirmación de la reserva {CodigoReserva} a {Email}", reserva.CodigoReserva, destinatario);
                    }
                });
            }

            return (await GetReservaByIdAsync(reserva.Id))!;
        }

        // PATCH/PUT: Cancelar Reserva
        public async Task<bool> CancelarReservaAsync(int id, int usuarioId, bool esAdmin)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null) return false;

            if (!esAdmin && reserva.UsuarioId != usuarioId)
            {
                throw new UnauthorizedAccessException("No tienes permiso para cancelar esta reserva.");
            }

            reserva.EstadoReserva = "CANCELADA";
            reserva.LastEditedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

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
            reserva.LastEditedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetReservaByIdAsync(id);
        }

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
            reserva.LastEditedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetReservaByIdAsync(id);
        }

        // Construye la consulta filtrada (búsqueda/estado/rango de fechas/orden) SIN paginar.
        // La usan tanto GetAllReservasAsync (que le agrega Skip/Take) como el reporte de
        // exportación — así ambos caminos ven exactamente los mismos filtros, sin duplicar lógica.
        private IQueryable<Reserva> ConstruirConsultaReservas(
            string? busqueda, string? ordenarPor, string? ordenDireccion,
            DateOnly? fechaInicio, DateOnly? fechaFin, string? estado)
        {
            var query = _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .Include(r => r.ReservaArticulos)
                    .ThenInclude(ra => ra.Articulo)
                .AsQueryable();

            if (fechaInicio.HasValue || fechaFin.HasValue)
            {
                var desde = fechaInicio ?? DateOnly.MinValue;
                var hasta = fechaFin ?? DateOnly.MaxValue;
                if (hasta < desde) (desde, hasta) = (hasta, desde);
                query = query.Where(r => r.Fecha >= desde && r.Fecha <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim().ToLower();
                query = query.Where(r =>
                    r.Usuario.Nombre.ToLower().Contains(termino) ||
                    r.Cancha.Nombre.ToLower().Contains(termino) ||
                    r.CodigoReserva.ToLower().Contains(termino));
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(r => r.EstadoReserva == estado);
            }

            bool desc = string.Equals(ordenDireccion, "desc", StringComparison.OrdinalIgnoreCase);
            return ordenarPor?.ToLower() switch
            {
                "nombreusuario" or "cliente" => desc ? query.OrderByDescending(r => r.Usuario.Nombre) : query.OrderBy(r => r.Usuario.Nombre),
                "nombrecancha" or "cancha" => desc ? query.OrderByDescending(r => r.Cancha.Nombre) : query.OrderBy(r => r.Cancha.Nombre),
                "total" => desc ? query.OrderByDescending(r => r.Total) : query.OrderBy(r => r.Total),
                "estadoreserva" or "estado" => desc ? query.OrderByDescending(r => r.EstadoReserva) : query.OrderBy(r => r.EstadoReserva),
                "fecha" => desc
                    ? query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraEntrada)
                    : query.OrderBy(r => r.Fecha).ThenBy(r => r.HoraEntrada),
                _ => query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraEntrada),
            };
        }

        // GET: Reservas para el reporte CSV (Admin/Operador) — mismos filtros que la tabla del
        // panel, pero sin paginar y sin el "solo hoy" por defecto: si no mandan fechas, exporta todo.
        public async Task<List<Reserva>> GetReservasParaExportarAsync(
            string? busqueda, string? ordenarPor, string? ordenDireccion,
            DateOnly? fechaInicio, DateOnly? fechaFin, string? estado)
        {
            return await ConstruirConsultaReservas(busqueda, ordenarPor, ordenDireccion, fechaInicio, fechaFin, estado)
                .ToListAsync();
        }

        // GET: Reservas de un usuario para su propio reporte CSV (Cliente)
        public async Task<List<Reserva>> GetReservasDeUsuarioParaExportarAsync(int usuarioId)
        {
            return await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .Include(r => r.ReservaArticulos)
                    .ThenInclude(ra => ra.Articulo)
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.Fecha)
                .ToListAsync();
        }
    }
}