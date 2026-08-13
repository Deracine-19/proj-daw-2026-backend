namespace proj_daw_2026_backend.Data;

// Normaliza page/pageSize para todos los listados paginados del panel de administrador.
// Evita que un valor inválido o abusivo (page=0, pageSize=100000) llegue a Skip/Take.
public static class PaginacionHelper
{
    public const int PageSizePorDefecto = 20;
    public const int PageSizeMaximo = 200;

    public static (int Page, int PageSize) Normalizar(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = PageSizePorDefecto;
        if (pageSize > PageSizeMaximo) pageSize = PageSizeMaximo;
        return (page, pageSize);
    }
}
