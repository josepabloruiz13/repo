namespace SwiftPay.Onboarding.Domain;

/// <summary>Días hábiles de Costa Rica: lunes a viernes, sin feriados.</summary>
public sealed class BusinessCalendar
{
    private readonly HashSet<DateOnly> _feriados;

    public BusinessCalendar(IEnumerable<DateOnly> feriados) => _feriados = feriados.ToHashSet();

    public bool EsFeriado(DateOnly d) => _feriados.Contains(d);

    public bool EsHabil(DateOnly d) =>
        d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_feriados.Contains(d);

    /// <summary>
    /// N = días hábiles transcurridos desde el día 0 hasta hoy. El día 0 no cuenta;
    /// el primer día hábil siguiente es N = 1.
    /// </summary>
    public int DiasHabilesDesde(DateOnly dia0, DateOnly hoy)
    {
        int n = 0;
        for (var d = dia0.AddDays(1); d <= hoy; d = d.AddDays(1))
            if (EsHabil(d)) n++;
        return n;
    }
}

public static class CostaRicaTime
{
    private static readonly TimeZoneInfo Zona = Resolver();

    private static TimeZoneInfo Resolver()
    {
        foreach (var id in new[] { "America/Costa_Rica", "Central America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        // Costa Rica no usa horario de verano: UTC-6 fijo.
        return TimeZoneInfo.CreateCustomTimeZone("CR", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");
    }

    public static DateTime Ahora() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    public static DateOnly Hoy() => DateOnly.FromDateTime(Ahora());

    public static DateTime DesdeUnixMs(long ms) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime, Zona);

    private static readonly string[] Meses =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    /// <summary>Ej.: "15 de noviembre".</summary>
    public static string FechaLarga(DateOnly d) => $"{d.Day} de {Meses[d.Month - 1]}";
}
