namespace proj_daw_2026_backend.Data.Entities;

// Fila única (sembrada con Id = 1) con la configuración global del negocio: nombre y horario
// de operación. No existe UI para crear/eliminar filas — el service siempre lee/edita esa
// única fila. Reemplaza a HorarioNegocioConstantes, que tenía el horario hardcodeado en código.
public class Configuracion
{
    public int Id { get; set; }
    public string NombreNegocio { get; set; } = string.Empty;
    public TimeSpan HoraApertura { get; set; }
    public TimeSpan HoraCierre { get; set; }
    public DateTime? LastEditedDate { get; set; }
}
