using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace proj_daw_2026_backend.Data.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    public bool Activo { get; set; } = true;

    // Nuevos campos para la recuperación de contraseña
    public bool RequiereCambioPassword { get; set; } = false;
    public string? PasswordAnteriorHash { get; set; }

    // Data URI completo ("data:image/png;base64,....") — mismo esquema que Cancha/Articulo.ImagenBase64.
    public string? ImagenBase64 { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastEditedDate { get; set; }
}