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

        /// <summary>
        /// Exporta el listado detallado de reservas a CSV compatible con Excel.
        /// </summary>
        [HttpGet("reservas-csv")]
        public async Task<IActionResult> ExportarReservasCsv([FromQuery] DateOnly fechaInicio, [FromQuery] DateOnly fechaFin)
        {
            if (fechaFin < fechaInicio)
            {
                return BadRequest(new { mensaje = "La fecha final no puede ser anterior a la fecha inicial." });
            }

            var reservas = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Cancha)
                .Include(r => r.ReservaArticulos)
                    .ThenInclude(ra => ra.Articulo)
                .Where(r => r.Fecha >= fechaInicio && r.Fecha <= fechaFin)
                .OrderByDescending(r => r.Fecha)
                .ThenBy(r => r.HoraEntrada)
                .ToListAsync();

            var csvBuilder = new StringBuilder();

            // Cabecera separada por punto y coma (Estándar de Excel en Español)
            csvBuilder.AppendLine("CodigoReserva;Fecha;HoraEntrada;HoraSalida;Cancha;Usuario;Email;Articulos;PrecioCancha;Total;EstadoReserva;EstadoPago");

            // Filas
            foreach (var r in reservas)
            {
                var articulosTexto = r.ReservaArticulos != null && r.ReservaArticulos.Any()
                    ? string.Join(" | ", r.ReservaArticulos.Select(ra => $"{ra.Articulo.Nombre} (x{ra.Cantidad})"))
                    : "Ninguno";

                csvBuilder.AppendLine(string.Join(";", new[]
                {
                    EscapeCsv(r.CodigoReserva),
                    r.Fecha.ToString("yyyy-MM-dd"),
                    r.HoraEntrada.ToString(@"hh\:mm"),
                    r.HoraSalida.ToString(@"hh\:mm"),
                    EscapeCsv(r.Cancha?.Nombre ?? ""),
                    EscapeCsv(r.Usuario?.Nombre ?? ""),
                    EscapeCsv(r.Usuario?.Email ?? ""),
                    EscapeCsv(articulosTexto),
                    r.PrecioAplicado.ToString("F2", CultureInfo.InvariantCulture),
                    r.Total.ToString("F2", CultureInfo.InvariantCulture),
                    EscapeCsv(r.EstadoReserva),
                    r.EstadoPago ? "Pagado" : "Pendiente"
                }));
            }

            // UTF-8 con BOM (Excel lo reconoce como UTF-8 inmediatamente)
            var utf8Encoding = new UTF8Encoding(true);
            var fileBytes = utf8Encoding.GetPreamble().Concat(utf8Encoding.GetBytes(csvBuilder.ToString())).ToArray();
            var fileName = $"Reporte_Reservas_{fechaInicio:yyyyMMdd}_al_{fechaFin:yyyyMMdd}.csv";

            // Usamos application/octet-stream para EVITAR que Swagger corrompa el archivo
            return File(fileBytes, "application/octet-stream", fileName);
        }

        /// <summary>
        /// Exporta un resumen consolidado de ingresos por cancha a CSV.
        /// </summary>
        [HttpGet("ingresos-canchas-csv")]
        public async Task<IActionResult> ExportarIngresosCanchasCsv([FromQuery] DateOnly fechaInicio, [FromQuery] DateOnly fechaFin)
        {
            if (fechaFin < fechaInicio)
            {
                return BadRequest(new { mensaje = "La fecha final no puede ser anterior a la fecha inicial." });
            }

            var resumenCanchas = await _context.Reservas
                .Where(r => r.Fecha >= fechaInicio && r.Fecha <= fechaFin && r.EstadoReserva != "Cancelada")
                .GroupBy(r => new { r.CanchaId, NombreCancha = r.Cancha.Nombre })
                .Select(g => new
                {
                    CanchaId = g.Key.CanchaId,
                    NombreCancha = g.Key.NombreCancha,
                    TotalReservas = g.Count(),
                    TotalIngresos = g.Sum(r => r.Total)
                })
                .OrderByDescending(g => g.TotalIngresos)
                .ToListAsync();

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("CanchaId;NombreCancha;TotalReservas;TotalIngresosLempiras");

            foreach (var item in resumenCanchas)
            {
                csvBuilder.AppendLine(string.Join(";", new[]
                {
                    item.CanchaId.ToString(),
                    EscapeCsv(item.NombreCancha),
                    item.TotalReservas.ToString(),
                    item.TotalIngresos.ToString("F2", CultureInfo.InvariantCulture)
                }));
            }

            var utf8Encoding = new UTF8Encoding(true);
            var fileBytes = utf8Encoding.GetPreamble().Concat(utf8Encoding.GetBytes(csvBuilder.ToString())).ToArray();
            var fileName = $"Reporte_Ingresos_Canchas_{fechaInicio:yyyyMMdd}_al_{fechaFin:yyyyMMdd}.csv";

            return File(fileBytes, "application/octet-stream", fileName);
        }

        /// <summary>
        /// Escapa punto y coma, comillas dobles y saltos de línea.
        /// </summary>
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