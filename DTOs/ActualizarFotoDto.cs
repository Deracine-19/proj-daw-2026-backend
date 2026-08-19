namespace proj_daw_2026_backend.DTOs;

// Usado por PATCH api/usuarios/perfil/foto — el usuario autenticado cambia su propia foto,
// sin poder tocar nombre/email/rol (eso queda reservado al Administrador vía UsuarioUpdateDto).
public class ActualizarFotoDto
{
    public string? ImagenBase64 { get; set; }
}
