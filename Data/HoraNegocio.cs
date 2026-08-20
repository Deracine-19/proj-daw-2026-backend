namespace proj_daw_2026_backend.Data;

// Honduras usa UTC-6 todo el año (no observa horario de verano), así que alcanza con un
// offset fijo en vez de depender de la zona horaria configurada en el sistema operativo
// donde corra el backend. Esto importa porque DateTime.Now/DateTime.Today devuelven la hora
// LOCAL de esa máquina — la VM de desarrollo, un contenedor de Railway, etc. — que casi nunca
// coincide con la hora real de Honduras (típicamente corren en UTC). Si el reloj del servidor
// va adelantado, la lógica de "esta hora ya pasó" se dispara de más y bloquea horarios que en
// la realidad todavía no han ocurrido — eso es lo que se estaba viendo como canchas que
// "no se pueden reservar aunque no estén ocupadas".
//
// DateTime.UtcNow SÍ es confiable sin importar el entorno (siempre es UTC real), así que todo
// el código de negocio que necesite "la fecha/hora de ahora" debe pasar por acá — nunca usar
// DateTime.Now/DateTime.Today directamente.
public static class HoraNegocio
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-6);

    public static DateTime Ahora => DateTime.UtcNow + Offset;
}
