namespace SwiftPay.Onboarding.Config;

/// <summary>Configuración leída de appsettings.json y variables de entorno (prefijo SWIFTPAY_).</summary>
public sealed class AppSettings
{
    /// <summary>Si es true no envía correos, no cambia etiquetas, no crea borradores, no llama al API ni escribe en SQL: solo informa qué haría.</summary>
    public bool DryRun { get; set; } = true;

    public GmailSettings Gmail { get; set; } = new();
    public SqlSettings Sql { get; set; } = new();
    public SwiftPortalsSettings SwiftPortals { get; set; } = new();
    public ClaudeSettings Claude { get; set; } = new();
    public ResumenSettings Resumen { get; set; } = new();
}

public sealed class GmailSettings
{
    /// <summary>Ruta al JSON de la cuenta de servicio de Google (con delegación de dominio).</summary>
    public string ServiceAccountJsonPath { get; set; } = "";

    /// <summary>Buzón que se gestiona (se suplanta con la delegación de dominio).</summary>
    public string Buzon { get; set; } = "afiliacion@swiftpay.one";

    /// <summary>Remitente del formulario automático de afiliación.</summary>
    public string RemitenteFormulario { get; set; } = "noreply@swiftpay.one";

    /// <summary>Dominio del banco. Nunca debe aparecer en correos al comercio.</summary>
    public string DominioBanco { get; set; } = "lafise.com";

    /// <summary>Dominio interno de SwiftPay.</summary>
    public string DominioInterno { get; set; } = "swiftpay.one";

    public int DiasBusqueda { get; set; } = 60;
}

public sealed class SqlSettings
{
    public string ConnectionString { get; set; } = "";
}

public sealed class SwiftPortalsSettings
{
    public string ActivationUrl { get; set; } = "https://swiftportals.com/api/external/merchants/payfac/activation";

    /// <summary>Llave del API. Configúrala SOLO por variable de entorno SWIFTPAY_SwiftPortals__ApiKey; nunca en el repositorio.</summary>
    public string ApiKey { get; set; } = "";

    public int TimeoutSegundos { get; set; } = 30;
}

public sealed class ClaudeSettings
{
    /// <summary>Si está vacía, el programa funciona solo con reglas y reporta los casos que no pudo clasificar.</summary>
    public string ApiKey { get; set; } = "";

    public string Model { get; set; } = "claude-opus-5-5";
}

public sealed class ResumenSettings
{
    /// <summary>A quién se envía el resumen de cada ejecución. Vacío = no se envía por correo (solo consola y SQL).</summary>
    public string Para { get; set; } = "afiliacion@swiftpay.one";

    /// <summary>Si es false, no envía el correo de resumen cuando no hubo onboardings activos ni cambios.</summary>
    public bool EnviarSiNoHayCambios { get; set; } = false;
}
