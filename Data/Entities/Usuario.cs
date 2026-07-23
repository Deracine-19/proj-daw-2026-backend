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
}
