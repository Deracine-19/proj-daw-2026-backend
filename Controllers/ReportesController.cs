using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.Data.Entities;
using System.Globalization;
using System.Text;

namespace proj_daw_2026_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDBContext _context;

        public ReportesController(AppDBContext context)
        {
            _context = context;
        }

        // =========================================================================================
        // 1. EXPORTAR RESERVAS (Individual)
        // =========================================================================================
        [HttpGet("exportar/reservas")]
        public async Task<IActionResult> ExportarReservasCsv([FromQuery] DateOnly? fechaInicio, [FromQuery] DateOnly? fechaFin)
        {
            var query = _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .Include(r => r.ReservaArticulos)
                    .ThenInclude(ra => ra.Articulo)
                .AsQueryable();

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                query = query.Where(r => r.Fecha >= fechaInicio.Value && r.Fecha <= fechaFin.Value);
            }

            var reservas = await query
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.HoraEntrada)
                .ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id;CodigoReserva;Fecha;HoraEntrada;HoraSalida;Cancha;Usuario;Articulos;EstadoReserva;EstadoPago;PrecioAplicado;Total;CreadoEl");

            foreach (var r in reservas)
            {
                var articulosTexto = r.ReservaArticulos != null && r.ReservaArticulos.Any()
                    ? string.Join(" | ", r.ReservaArticulos.Select(ra => $"{ra.Articulo.Nombre} (x{ra.Cantidad})"))
                    : "Ninguno";

                csvBuilder.AppendLine(string.Join(";", new[]
                {
                    r.Id.ToString(),
                    EscapeCsv(r.CodigoReserva),
                    r.Fecha.ToString("yyyy-MM-dd"),
                    r.HoraEntrada.ToString(@"hh\:mm"),
                    r.HoraSalida.ToString(@"hh\:mm"),
                    EscapeCsv(r.Cancha?.Nombre ?? ""),
                    EscapeCsv(r.Usuario?.Nombre ?? ""),
                    EscapeCsv(articulosTexto),
                    EscapeCsv(r.EstadoReserva),
                    r.EstadoPago ? "Pagado" : "Pendiente",
                    r.PrecioAplicado.ToString("F2", CultureInfo.InvariantCulture),
                    r.Total.ToString("F2", CultureInfo.InvariantCulture),
                    r.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                }));
            }

            return GenerarArchivoCsv(csvBuilder.ToString(), $"Reservas_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // =========================================================================================
        // 2. EXPORTAR CANCHAS (Individual)
        // =========================================================================================
        [HttpGet("exportar/canchas")]
        public async Task<IActionResult> ExportarCanchasCsv()
        {
            var canchas = await _context.Canchas.ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id;Nombre;Descripcion;PrecioHora;Estado;CantidadJugadores");

            foreach (var c in canchas)
            {
                csvBuilder.AppendLine(string.Join(";", new[]
                {
                    c.Id.ToString(),
                    EscapeCsv(c.Nombre),
                    EscapeCsv(c.Descripcion),
                    c.PrecioHora.ToString("F2", CultureInfo.InvariantCulture),
                    c.Estado ? "Activa" : "Inactiva",
                    c.CantidadJugadores.ToString()
                }));
            }

            return GenerarArchivoCsv(csvBuilder.ToString(), $"Canchas_{DateTime.Now:yyyyMMdd}.csv");
        }

        // =========================================================================================
        // 3. EXPORTAR USUARIOS (Individual)
        // =========================================================================================
        [HttpGet("exportar/usuarios")]
        public async Task<IActionResult> ExportarUsuariosCsv()
        {
            var usuarios = await _context.Usuarios.ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id;Nombre;Email;RolId;Activo;RequiereCambioPassword");

            foreach (var u in usuarios)
            {
                csvBuilder.AppendLine(string.Join(";", new[]
                {
                    u.Id.ToString(),
                    EscapeCsv(u.Nombre),
                    EscapeCsv(u.Email),
                    u.RolId.ToString(),
                    u.Activo ? "Si" : "No",
                    u.RequiereCambioPassword ? "Si" : "No"
                }));
            }

            return GenerarArchivoCsv(csvBuilder.ToString(), $"Usuarios_{DateTime.Now:yyyyMMdd}.csv");
        }

        // =========================================================================================
        // 4. EXPORTAR ARTICULOS (Individual)
        // =========================================================================================
        [HttpGet("exportar/articulos")]
        public async Task<IActionResult> ExportarArticulosCsv()
        {
            var articulos = await _context.Articulos.ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Id;Nombre;Descripcion;Precio;Estado");

            foreach (var a in articulos)
            {
                csvBuilder.AppendLine(string.Join(";", new[]
                {
                    a.Id.ToString(),
                    EscapeCsv(a.Nombre),
                    EscapeCsv(a.Descripcion),
                    a.Precio.ToString("F2", CultureInfo.InvariantCulture),
                    a.Estado ? "Activo" : "Inactivo"
                }));
            }

            return GenerarArchivoCsv(csvBuilder.ToString(), $"Articulos_{DateTime.Now:yyyyMMdd}.csv");
        }


        // =========================================================================================
        // MÉTODOS AUXILIARES
        // =========================================================================================

        private IActionResult GenerarArchivoCsv(string contenidoCsv, string nombreArchivo)
        {
            var utf8Encoding = new UTF8Encoding(true);
            var fileBytes = utf8Encoding.GetPreamble().Concat(utf8Encoding.GetBytes(contenidoCsv)).ToArray();

            return File(fileBytes, "application/octet-stream", nombreArchivo);
        }

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";

            if (field.Contains(";") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return $"\"{field}\"";
        }
    }
}