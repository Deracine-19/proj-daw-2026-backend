using System.ComponentModel.DataAnnotations;

namespace proj_daw_2026_backend.DTOs;

public class UsuarioUpdateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol válido.")]
    public int RolId { get; set; }

    // Editable solo por Administrador, vía este mismo DTO. La foto propia del usuario
    // (cualquier rol) se cambia por separado — ver ActualizarFotoDto / endpoint "perfil/foto".
    public string? ImagenBase64 { get; set; }
}