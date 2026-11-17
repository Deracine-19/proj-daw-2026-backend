using System.Globalization;
using System.Net;

namespace proj_daw_2026_backend.Services
{
    // Plantillas HTML de los correos, con la misma estética del frontend (tema oscuro, verde de
    // marca, tarjeta con filas etiqueta/valor como en ReservaConfirmadaPage). Los colores son los
    // tokens de src/index.css copiados a mano: los clientes de correo no soportan variables CSS.
    // Todo va con tablas y estilos inline porque Gmail/Outlook ignoran <style> y flexbox.
    public static class EmailTemplates
    {
        private const string ColorPage = "#09090b";
        private const string ColorSurface = "#0c0c0e";
        private const string ColorLine = "#1f1f22";
        private const string ColorInk = "#fafafa";
        private const string ColorInkMuted = "#a1a1aa";
        private const string ColorInkFaint = "#71717a";
        private const string ColorBrand = "#329e26";
        private const string ColorPositive = "#7fd970";
        private const string ColorPositiveBg = "#12210e";

        // Geist solo carga en clientes que respetan <link> (Apple Mail, iOS); el resto usa la del sistema.
        private const string FontSans = "Geist, -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif";
        private const string FontMono = "Geist Mono, ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";

        private static readonly string[] Meses = { "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic" };

        public record ArticuloLinea(string Nombre, int Cantidad, decimal Subtotal);

        public static string ReservaConfirmada(
            string nombreNegocio, string nombreUsuario, string codigoReserva, string cancha,
            DateOnly fecha, TimeSpan horaEntrada, TimeSpan horaSalida,
            IReadOnlyList<ArticuloLinea> articulos, decimal total)
        {
            var filas = Fila("Cancha", cancha)
                + Fila("Fecha", FormatoFecha(fecha))
                + Fila("Horario", $@"{horaEntrada:hh\:mm} – {horaSalida:hh\:mm}");

            foreach (var a in articulos)
                filas += Fila($"{a.Nombre} ×{a.Cantidad}", FormatoMoneda(a.Subtotal));

            var contenido = $@"
                {Icono("&#10003;")}
                {Titulo("¡Reserva confirmada!")}
                {Parrafo($"Hola <strong style='color:{ColorInk};font-weight:600;'>{H(nombreUsuario)}</strong>, presenta este código en la cancha para validar tu pago.")}
                {Codigo(codigoReserva, 24, ".2em")}
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'
                       style='background-color:{ColorPage};border:1px solid {ColorLine};border-radius:10px;'>
                    <tr><td style='padding:6px 14px;'>
                        <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
                            {filas}
                            <tr><td colspan='2' style='padding:6px 0;'><div style='height:1px;background-color:{ColorLine};line-height:1px;font-size:0;'>&nbsp;</div></td></tr>
                            <tr>
                                <td style='padding:6px 0 8px;font-size:13px;color:{ColorInkFaint};'>Total</td>
                                <td align='right' style='padding:6px 0 8px;font-size:15px;font-weight:600;color:{ColorInk};'>{FormatoMoneda(total)}</td>
                            </tr>
                        </table>
                    </td></tr>
                </table>";

            return Layout(nombreNegocio, $"Tu código de reserva es {codigoReserva}", contenido);
        }

        public static string ClaveTemporal(string nombreNegocio, string nombreUsuario, string claveTemporal)
        {
            var contenido = $@"
                {Icono("&#128273;")}
                {Titulo("Recuperación de contraseña")}
                {Parrafo($"Hola <strong style='color:{ColorInk};font-weight:600;'>{H(nombreUsuario)}</strong>, solicitaste restablecer tu contraseña. Esta es tu clave temporal de acceso:")}
                {Codigo(claveTemporal, 20, ".08em")}
                {Parrafo("Ingresa con esta clave y registra una nueva contraseña de inmediato. Si no solicitaste este cambio, puedes ignorar este correo.", 0)}";

            return Layout(nombreNegocio, "Tu clave temporal de acceso", contenido);
        }

        private static string Layout(string nombreNegocio, string preheader, string contenido) => $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <meta name='color-scheme' content='dark'>
    <meta name='supported-color-schemes' content='dark'>
    <link href='https://fonts.googleapis.com/css2?family=Geist:wght@400;500;600&family=Geist+Mono:wght@500;600&display=swap' rel='stylesheet'>
</head>
<body style='margin:0;padding:0;background-color:{ColorPage};'>
    <div style='display:none;max-height:0;overflow:hidden;opacity:0;'>{H(preheader)}</div>
    <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' bgcolor='{ColorPage}' style='background-color:{ColorPage};'>
        <tr><td align='center' style='padding:40px 16px;font-family:{FontSans};'>
            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:440px;'>
                <tr><td align='center' style='padding-bottom:20px;'>
                    <span style='display:inline-block;width:8px;height:8px;border-radius:50%;background-color:{ColorBrand};vertical-align:middle;'></span>
                    <span style='font-size:15px;font-weight:600;letter-spacing:-0.01em;color:{ColorInk};vertical-align:middle;padding-left:6px;'>{H(nombreNegocio)}</span>
                </td></tr>
                <tr><td bgcolor='{ColorSurface}' style='background-color:{ColorSurface};border:1px solid {ColorLine};border-radius:14px;padding:24px;text-align:center;'>
                    {contenido}
                </td></tr>
                <tr><td align='center' style='padding-top:20px;font-size:12px;line-height:18px;color:{ColorInkFaint};'>
                    Correo automático de {H(nombreNegocio)}, por favor no respondas a este mensaje.
                </td></tr>
            </table>
        </td></tr>
    </table>
</body>
</html>";

        private static string Icono(string entidad) => $@"
            <table role='presentation' cellpadding='0' cellspacing='0' border='0' align='center' style='margin:0 auto 12px;'>
                <tr><td width='44' height='44' align='center' valign='middle'
                        style='width:44px;height:44px;border-radius:22px;background-color:{ColorPositiveBg};color:{ColorPositive};font-size:20px;line-height:44px;'>{entidad}</td></tr>
            </table>";

        private static string Titulo(string texto) =>
            $"<h1 style='margin:0 0 8px;font-size:18px;line-height:26px;font-weight:600;color:{ColorInk};'>{texto}</h1>";

        // El contenido se pasa ya con HTML seguro (lo variable se escapa con H() antes de llegar aquí).
        private static string Parrafo(string html, int margenInferior = 16) =>
            $"<p style='margin:0 0 {margenInferior}px;font-size:14px;line-height:21px;color:{ColorInkMuted};'>{html}</p>";

        private static string Codigo(string codigo, int tamano, string espaciado) => $@"
            <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:16px;'>
                <tr><td align='center' style='padding:16px 8px;background-color:{ColorPositiveBg};border:1px solid {ColorBrand};border-radius:10px;
                        font-family:{FontMono};font-size:{tamano}px;font-weight:600;letter-spacing:{espaciado};color:{ColorPositive};'>{H(codigo)}</td></tr>
            </table>";

        private static string Fila(string etiqueta, string valor) => $@"
            <tr>
                <td style='padding:6px 12px 6px 0;font-size:13px;color:{ColorInkFaint};text-align:left;'>{H(etiqueta)}</td>
                <td align='right' style='padding:6px 0;font-size:13px;font-weight:500;color:{ColorInk};'>{H(valor)}</td>
            </tr>";

        // Mismo formato que el frontend: "16 nov 2026" y "L 1,250".
        private static string FormatoFecha(DateOnly fecha) => $"{fecha.Day} {Meses[fecha.Month - 1]} {fecha.Year}";

        private static string FormatoMoneda(decimal monto) => "L " + monto.ToString("#,0.##", CultureInfo.InvariantCulture);

        // Nombre del usuario, cancha, artículos, etc. vienen de la BD (los escribe el propio usuario
        // o un admin) — sin escapar, alguien podría registrarse con un nombre que inyecte HTML.
        private static string H(string? texto) => WebUtility.HtmlEncode(texto ?? string.Empty);
    }
}
