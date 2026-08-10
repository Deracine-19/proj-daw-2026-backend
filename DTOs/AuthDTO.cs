using System.ComponentModel.DataAnnotations;

namespace proj_daw_2026_backend.DTOs;

public class RegisterDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string TempPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}