namespace proj_daw_2026_backend.DTOs
{
    // Formato de horas: "HH:mm:ss" (TimeSpan), igual que el resto del proyecto — "9:00" falla la
    // deserialización, debe ser "09:00:00".
    public class ConfiguracionDto
    {
        public string NombreNegocio { get; set; } = string.Empty;
        public TimeSpan HoraApertura { get; set; }
        public TimeSpan HoraCierre { get; set; }
    }

    public class ConfiguracionReadDto : ConfiguracionDto
    {
        public DateTime? LastEditedDate { get; set; }
    }
}
