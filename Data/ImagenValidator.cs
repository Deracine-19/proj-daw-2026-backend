namespace proj_daw_2026_backend.Data;

// Validación compartida para cualquier campo ImagenBase64 (Articulo, Cancha, ...).
// Centralizado acá para que el límite de tamaño se ajuste en un solo lugar.
public static class ImagenValidator
{
    // ~3M caracteres de base64 ≈ 2.2 MB de imagen real. Cota generosa para una miniatura,
    // pero evita que alguien mande un archivo enorme y deje la tabla (y cada GET) pesadísima.
    public const int MaxLength = 3_000_000;

    public static void Validar(string? imagenBase64)
    {
        if (string.IsNullOrEmpty(imagenBase64)) return;

        if (!imagenBase64.StartsWith("data:image/"))
            throw new InvalidOperationException("La imagen debe subirse como archivo de imagen válido.");

        if (imagenBase64.Length > MaxLength)
            throw new InvalidOperationException("La imagen es demasiado grande (máximo ~2 MB).");
    }
}
