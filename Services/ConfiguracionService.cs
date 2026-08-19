using Microsoft.EntityFrameworkCore;
using proj_daw_2026_backend.Data.Entities;
using proj_daw_2026_backend.DTOs;

namespace proj_daw_2026_backend.Services
{
    public interface IConfiguracionService
    {
        Task<ConfiguracionReadDto> GetConfiguracionAsync();
        Task<ConfiguracionReadDto> UpdateConfiguracionAsync(ConfiguracionDto dto);
    }

    public class ConfiguracionService : IConfiguracionService
    {
        private readonly AppDBContext _context;

        public ConfiguracionService(AppDBContext context)
        {
            _context = context;
        }

        // GET: Devuelve la fila única de configuración (sembrada por la migración, Id = 1).
        public async Task<ConfiguracionReadDto> GetConfiguracionAsync()
        {
            var config = await ObtenerFilaUnicaAsync();
            return MapToReadDto(config);
        }

        // PUT: Actualiza la fila única de configuración.
        public async Task<ConfiguracionReadDto> UpdateConfiguracionAsync(ConfiguracionDto dto)
        {
            Validar(dto);

            var config = await ObtenerFilaUnicaAsync();
            config.NombreNegocio = dto.NombreNegocio.Trim();
            config.HoraApertura = dto.HoraApertura;
            config.HoraCierre = dto.HoraCierre;
            config.LastEditedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToReadDto(config);
        }

        // Siempre hay exactamente una fila (sembrada vía HasData en AppDBContext) — no hay
        // creación/eliminación de configuración desde la app, solo edición de esa fila.
        private async Task<Configuracion> ObtenerFilaUnicaAsync()
        {
            return await _context.Configuraciones.FirstAsync();
        }

        private static void Validar(ConfiguracionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreNegocio))
                throw new InvalidOperationException("El nombre del negocio es obligatorio.");

            if (dto.NombreNegocio.Trim().Length > 60)
                throw new InvalidOperationException("El nombre del negocio no puede superar los 60 caracteres.");

            if (dto.HoraApertura < TimeSpan.Zero || dto.HoraCierre > TimeSpan.FromHours(24))
                throw new InvalidOperationException("El horario debe estar dentro de un día válido.");

            if (dto.HoraApertura >= dto.HoraCierre)
                throw new InvalidOperationException("La hora de apertura debe ser anterior a la hora de cierre.");
        }

        private static ConfiguracionReadDto MapToReadDto(Configuracion c) => new()
        {
            NombreNegocio = c.NombreNegocio,
            HoraApertura = c.HoraApertura,
            HoraCierre = c.HoraCierre,
            LastEditedDate = c.LastEditedDate
        };
    }
}
