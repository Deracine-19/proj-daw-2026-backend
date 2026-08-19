namespace proj_daw_2026_backend.DTOs
{
    // Envoltorio genérico para cualquier listado paginado del panel de administrador.
    // Antes los endpoints devolvían la tabla completa; con muchos registros eso se vuelve
    // lento tanto en la consulta a Postgres como en el JSON que viaja al frontend.
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
