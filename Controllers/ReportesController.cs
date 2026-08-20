using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proj_daw_2026_backend.Data;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.Services;

namespace proj_daw_2026_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // La autorización específica de cada reporte se decide por acción, no acá.
    public class ReportesController : ControllerBase
    {
        private readonly CanchaService _canchaService;
        private readonly IUsuarioService _usuarioService;
        private readonly IArticuloService _articuloService;
        private readonly ReservaService _reservaService;

        public ReportesController(
            CanchaService canchaService,
            IUsuarioService usuarioService,
            IArticuloService articuloService,
            ReservaService reservaService)
        {
            _canchaService = canchaService;
            _usuarioService = usuarioService;
            _articuloService = articuloService;
            _reservaService = reservaService;
        }

        // =========================================================================================
        // 1. EXPORTAR RESERVAS — panel de administrador (Admin + Operador, igual que la tabla)
        // =========================================================================================
        [HttpGet("exportar/reservas")]
        [Authorize(Roles = "Administrador,Operador")]
        public async Task<IActionResult> ExportarReservasCsv(
            [FromQuery] string? busqueda,
            [FromQuery] string? ordenarPor,
            [FromQuery] string? ordenDireccion,
            [FromQuery] DateOnly? fechaInicio,
            [FromQuery] DateOnly? fechaFin,
            [FromQuery] string? estado)
        {
            var reservas = await _reservaService.GetReservasParaExportarAsync(
                busqueda, ordenarPor, ordenDireccion, fechaInicio, fechaFin, estado);

            return GenerarArchivoCsv(ConstruirCsvReservas(reservas), NombreArchivo("reservas"));
        }

        // =========================================================================================
        // 2. EXPORTAR MIS RESERVAS — cualquier usuario autenticado, solo sus propias reservas
        // =========================================================================================
        [HttpGet("exportar/mis-reservas")]
        public async Task<IActionResult> ExportarMisReservasCsv()
        {
            int usuarioId = GetUserIdFromToken();
            var reservas = await _reservaService.GetReservasDeUsuarioParaExportarAsync(usuarioId);

            return GenerarArchivoCsv(ConstruirCsvReservas(reservas), NombreArchivo("mis-reservas"));
        }

        // =========================================================================================
        // 3. EXPORTAR CANCHAS (Solo Administrador)
        // =========================================================================================
        [HttpGet("exportar/canchas")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ExportarCanchasCsv(
            [FromQuery] string? busqueda, [FromQuery] string? ordenarPor, [FromQuery] string? ordenDireccion)
        {
            var canchas = await _canchaService.GetCanchasParaExportarAsync(busqueda, ordenarPor, ordenDireccion);

            var csv = new StringBuilder();
            csv.AppendLine("Id;Nombre;Descripcion;PrecioHora;Estado;CantidadJugadores;CreadoEl;EditadoEl");

            foreach (var c in canchas)
            {
                csv.AppendLine(string.Join(";", new[]
                {
                    c.Id.ToString(),
                    EscapeCsv(c.Nombre),
                    EscapeCsv(c.Descripcion),
                    c.PrecioHora.ToString("F2", CultureInfo.InvariantCulture),
                    c.Estado ? "Activa" : "Inactiva",
                    c.CantidadJugadores.ToString(),
                    FormatoFecha(c.CreatedDate),
                    FormatoFecha(c.LastEditedDate)
                }));
            }

            return GenerarArchivoCsv(csv.ToString(), NombreArchivo("canchas"));
        }

        // =========================================================================================
        // 4. EXPORTAR USUARIOS (Solo Administrador) — nunca incluye PasswordHash/PasswordAnteriorHash
        // =========================================================================================
        [HttpGet("exportar/usuarios")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ExportarUsuariosCsv(
            [FromQuery] string? busqueda, [FromQuery] string? ordenarPor, [FromQuery] string? ordenDireccion,
            [FromQuery] string? rol, [FromQuery] bool? activo)
        {
            var usuarios = await _usuarioService.GetUsuariosParaExportarAsync(busqueda, ordenarPor, ordenDireccion, rol, activo);

            var csv = new StringBuilder();
            csv.AppendLine("Id;Nombre;Email;Rol;Activo;RequiereCambioPassword;CreadoEl;EditadoEl");

            foreach (var u in usuarios)
            {
                csv.AppendLine(string.Join(";", new[]
                {
                    u.Id.ToString(),
                    EscapeCsv(u.Nombre),
                    EscapeCsv(u.Email),
                    EscapeCsv(u.Rol?.Nombre ?? ""),
                    u.Activo ? "Si" : "No",
                    u.RequiereCambioPassword ? "Si" : "No",
                    FormatoFecha(u.CreatedDate),
                    FormatoFecha(u.LastEditedDate)
                }));
            }

            return GenerarArchivoCsv(csv.ToString(), NombreArchivo("usuarios"));
        }

        // =========================================================================================
        // 5. EXPORTAR ARTICULOS (Solo Administrador)
        // =========================================================================================
        [HttpGet("exportar/articulos")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ExportarArticulosCsv(
            [FromQuery] string? busqueda, [FromQuery] string? ordenarPor, [FromQuery] string? ordenDireccion)
        {
            var articulos = await _articuloService.GetArticulosParaExportarAsync(busqueda, ordenarPor, ordenDireccion);

            var csv = new StringBuilder();
            csv.AppendLine("Id;Nombre;Descripcion;Precio;Estado;CreadoEl;EditadoEl");

            foreach (var a in articulos)
            {
                csv.AppendLine(string.Join(";", new[]
                {
                    a.Id.ToString(),
                    EscapeCsv(a.Nombre),
                    EscapeCsv(a.Descripcion),
                    a.Precio.ToString("F2", CultureInfo.InvariantCulture),
                    a.Estado ? "Activo" : "Inactivo",
                    FormatoFecha(a.CreatedDate),
                    FormatoFecha(a.LastEditedDate)
                }));
            }

            return GenerarArchivoCsv(csv.ToString(), NombreArchivo("articulos"));
        }

        // =========================================================================================
        // MÉTODOS AUXILIARES
        // =========================================================================================

        // Reutilizado por el reporte de administración y el de "mis reservas" del cliente —
        // así ambos quedan con exactamente las mismas columnas, sin duplicar el armado del CSV.
        private static string ConstruirCsvReservas(List<Reserva> reservas)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id;CodigoReserva;Fecha;HoraEntrada;HoraSalida;Cancha;Usuario;Articulos;EstadoReserva;EstadoPago;PrecioAplicado;Total;CreadoEl;EditadoEl");

            foreach (var r in reservas)
            {
                var articulosTexto = r.ReservaArticulos != null && r.ReservaArticulos.Any()
                    ? string.Join(" | ", r.ReservaArticulos.Select(ra => $"{ra.Articulo?.Nombre ?? "Artículo"} (x{ra.Cantidad})"))
                    : "Ninguno";

                csv.AppendLine(string.Join(";", new[]
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
                    FormatoFecha(r.CreatedDate),
                    FormatoFecha(r.LastEditedDate)
                }));
            }

            return csv.ToString();
        }

        private static string NombreArchivo(string dato) => $"{dato}_{HoraNegocio.Ahora:yyyy-MM-dd}.csv";

        private static string FormatoFecha(DateTime? fecha) =>
            fecha.HasValue ? fecha.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";

        private IActionResult GenerarArchivoCsv(string contenidoCsv, string nombreArchivo)
        {
            // BOM UTF-8: sin esto, Excel puede mostrar mal los acentos/ñ al abrir el archivo directo.
            var utf8Encoding = new UTF8Encoding(true);
            var fileBytes = utf8Encoding.GetPreamble().Concat(utf8Encoding.GetBytes(contenidoCsv)).ToArray();

            return File(fileBytes, "text/csv", nombreArchivo);
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

        private int GetUserIdFromToken()
        {
            var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(nameIdentifierClaim, out int usuarioId))
            {
                return usuarioId;
            }
            throw new UnauthorizedAccessException("Usuario no válido en el Token.");
        }
    }
}
