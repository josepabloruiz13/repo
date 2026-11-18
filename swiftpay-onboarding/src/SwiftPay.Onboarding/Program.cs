using Microsoft.Extensions.Configuration;
using SwiftPay.Onboarding.Config;
using SwiftPay.Onboarding.Domain;
using SwiftPay.Onboarding.Infra;

// Uso:
//   SwiftPay.Onboarding.exe            -> usa DryRun de appsettings.json
//   SwiftPay.Onboarding.exe --simular  -> fuerza modo simulación (no envía ni cambia nada)
//   SwiftPay.Onboarding.exe --real     -> fuerza modo real
// Código de salida 0 = OK, 1 = error (el job de SQL Agent lo marca como fallido).

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables(prefix: "SWIFTPAY_")
    .Build();

var cfg = config.Get<AppSettings>() ?? new AppSettings();
if (args.Contains("--simular")) cfg.DryRun = true;
if (args.Contains("--real")) cfg.DryRun = false;

var hoy = CostaRicaTime.Hoy();
var resumen = new RunSummary { DryRun = cfg.DryRun };
Db db;
long ejecucionId = 0;
string? errorFatal = null;
GmailClient? gmail = null;

try
{
    db = new Db(cfg.Sql.ConnectionString);
    if (!cfg.DryRun) ejecucionId = await db.IniciarEjecucionAsync(cfg.DryRun);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"No se pudo conectar a SQL Server: {ex.Message}");
    return 1;
}

try
{
    var calendario = new BusinessCalendar(await db.FeriadosAsync());

    if (calendario.EsFeriado(hoy))
    {
        resumen.LineaUnica = $"Hoy ({hoy:dd/MM/yyyy}) es feriado en Costa Rica; no se procesó nada.";
    }
    else if (!calendario.EsHabil(hoy))
    {
        resumen.LineaUnica = $"Hoy ({hoy:dd/MM/yyyy}) no es día hábil; no se procesó nada.";
    }
    else
    {
        if (await db.FeriadosVerificadosDelAnioAsync(hoy.Year) == 0)
            resumen.Avisos.Add($"No hay feriados VERIFICADOS de {hoy.Year} en onb.Feriado. Cárgalos desde el calendario oficial del MTSS (ver sql/02_feriados.sql); mientras tanto los días hábiles pueden estar mal contados.");

        gmail = new GmailClient(cfg.Gmail);
        var claude = new ClaudeClassifier(cfg.Claude);
        SwiftPortalsClient? portals = string.IsNullOrWhiteSpace(cfg.SwiftPortals.ApiKey) ? null : new SwiftPortalsClient(cfg.SwiftPortals);

        var proc = new OnboardingProcessor(cfg, gmail, db, claude, portals, calendario, resumen, hoy);
        await proc.EjecutarAsync();
    }
}
catch (Exception ex)
{
    errorFatal = $"{ex.GetType().Name}: {ex.Message}";
    resumen.Dudas.Add($"La ejecución se detuvo por un error: {errorFatal}");
}

var texto = resumen.Texto();
Console.WriteLine(texto);

// Correo de resumen (interno).
if (gmail != null && !string.IsNullOrWhiteSpace(cfg.Resumen.Para) && !cfg.DryRun &&
    (errorFatal != null || !resumen.SinActividad || cfg.Resumen.EnviarSiNoHayCambios))
{
    try
    {
        await gmail.EnviarAsync([cfg.Resumen.Para], [], $"Resumen onboarding SwiftPay – {hoy:dd/MM/yyyy}", texto);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"No se pudo enviar el resumen por correo: {ex.Message}");
    }
}

if (!cfg.DryRun)
{
    try { await db.TerminarEjecucionAsync(ejecucionId, texto, errorFatal); }
    catch (Exception ex) { Console.Error.WriteLine($"No se pudo guardar la ejecución en SQL: {ex.Message}"); }
}

return errorFatal is null ? 0 : 1;
