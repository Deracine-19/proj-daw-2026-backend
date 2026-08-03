namespace proj_daw_2026_backend.Data;

// Horario de operación del negocio. Cambiar acá afecta la validación de
// CreateReservaAsync — es la fuente de verdad, no el frontend.
public static class HorarioNegocioConstantes
{
    public static readonly TimeSpan HoraApertura = new(8, 0, 0);
    public static readonly TimeSpan HoraCierre = new(22, 0, 0);
}
