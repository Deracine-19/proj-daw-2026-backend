using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace proj_daw_2026_backend.Services
{
    // Envío por la API HTTP de Mailtrap (antes era SMTP). No usamos el SDK oficial porque solo
    // se publica en GitHub Packages y exigiría un token de GitHub en cada build (incluido Docker);
    // el endpoint es un simple POST con JSON, así que basta con HttpClient.
    public class EmailService
    {
        private const string SendUrl = "https://send.api.mailtrap.io/api/send";
        private const string SandboxUrl = "https://sandbox.api.mailtrap.io/api/send";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public EmailService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            var apiToken = _config["MailtrapSettings:ApiToken"];
            var sandboxId = _config["MailtrapSettings:SandboxId"];
            var senderEmail = _config["MailtrapSettings:SenderEmail"];
            var senderName = _config["MailtrapSettings:SenderName"];

            // Con SandboxId los correos caen en la bandeja de pruebas de Mailtrap (no llegan al
            // destinatario real); sin él se envían de verdad por el stream transaccional.
            var url = string.IsNullOrWhiteSpace(sandboxId) ? SendUrl : $"{SandboxUrl}/{sandboxId}";

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(new
                {
                    from = new { email = senderEmail, name = senderName },
                    to = new[] { new { email = toEmail } },
                    subject,
                    html = bodyHtml
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            using var response = await _httpClient.SendAsync(request);

            // Mailtrap explica el motivo del fallo en el cuerpo (ej. {"success":false,"errors":[...]}),
            // así que lo incluimos en la excepción para que quede en el log de quien llama.
            if (!response.IsSuccessStatusCode)
            {
                var detalle = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Mailtrap respondió {(int)response.StatusCode}: {detalle}", null, response.StatusCode);
            }
        }
    }
}
