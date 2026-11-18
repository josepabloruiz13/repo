namespace SwiftPay.Onboarding.Domain;

public sealed record Correo(string Asunto, string Cuerpo);

/// <summary>Plantillas aprobadas. Solo se adapta el nombre del contacto, del comercio, fechas y N.</summary>
public static class Templates
{
    public const string Firma = """
        Saludos cordiales,

        Afiliación
        SwiftPay
        afiliacion@swiftpay.one
        www.swiftpay.one
        """;

    public const string SaludoSinContacto = "[NOMBRE DEL CONTACTO]";

    public static string AsuntoInicio(string comercio) => $"Afiliación {comercio} – Proceso en curso";
    public static string AsuntoSeguimiento(string comercio) => $"Afiliación {comercio} – Seguimiento";
    public static string AsuntoSeguimientoDisculpa(string comercio) => $"Afiliación {comercio} – Actualización del proceso";
    public const string AsuntoBienvenida = "¡Bienvenido a SwiftPay! ¡Su afiliación ha sido finalizada!";
    public static string AsuntoAlertaFallida(string comercio) => $"ALERTA – Activación SwiftPortals fallida – {comercio}";
    public static string AsuntoAlertaOkSinBienvenida(string comercio) => $"ALERTA – Activación OK, falta bienvenida – {comercio}";

    public static Correo Inicio(string nombre, string comercio) => new(AsuntoInicio(comercio), $"""
        Estimado(a) {nombre},

        Espero que se encuentre muy bien.

        Le confirmamos que hemos recibido su solicitud y que la afiliación de {comercio} ya se encuentra en proceso. Nuestro equipo ya está gestionando las configuraciones y validaciones correspondientes.

        Le estaremos notificando oportunamente sobre los avances, así como cualquier información o documentación adicional que pudiera requerirse para completar el proceso.

        Agradecemos la confianza depositada en nosotros y quedamos atentos ante cualquier consulta.

        {Firma}
        """);

    public static Correo Seguimiento(string nombre, string comercio) => new(AsuntoSeguimiento(comercio), $"""
        Estimado(a) {nombre},

        Espero que se encuentre muy bien.

        Le escribimos para darle seguimiento a la afiliación de {comercio}. Le informamos que el proceso continúa avanzando y nuestro equipo sigue trabajando en las configuraciones y validaciones necesarias para habilitar su servicio.

        Apenas tengamos la confirmación final, le estaremos notificando por este medio.

        Quedamos atentos ante cualquier consulta.

        {Firma}
        """);

    public static Correo SeguimientoDisculpa(string nombre, string comercio) => new(AsuntoSeguimientoDisculpa(comercio), $"""
        Estimado(a) {nombre},

        Espero que se encuentre muy bien.

        Queremos ofrecerle una disculpa porque el proceso de afiliación de {comercio} está tomando más tiempo del previsto. Las validaciones internas requeridas para habilitar su servicio aún se encuentran en curso y nuestro equipo les está dando seguimiento prioritario.

        Le estaremos notificando apenas el servicio quede habilitado. Agradecemos mucho su paciencia y comprensión.

        Quedamos atentos ante cualquier consulta.

        {Firma}
        """);

    public static Correo Bienvenida(string nombre, string comercio) => new(AsuntoBienvenida, $"""
        Estimado(a) {nombre},

        Espero que se encuentre muy bien.

        Nos complace informarle que la afiliación de {comercio} a SwiftPay ha sido finalizada con éxito y ya puede comenzar a utilizar nuestra plataforma.

        En un correo aparte recibirá su usuario y contraseña de acceso al sistema. Por favor, revise su bandeja de entrada y, si no lo encuentra, también la carpeta de correo no deseado.

        Quedamos atentos ante cualquier duda o asistencia que requiera.

        {Firma}
        """);

    /// <summary>Recordatorio de vencimiento al banco (N = 3). Va como respuesta en el hilo con el banco.</summary>
    public static string RecordatorioVencimientoBanco(string comercio, DateOnly dia0, bool bancoConfirmoAceptacion, string? numeroSolicitud)
    {
        var fecha = CostaRicaTime.FechaLarga(dia0);
        string parrafo;
        if (bancoConfirmoAceptacion)
        {
            var consulta = string.IsNullOrWhiteSpace(numeroSolicitud)
                ? $"Quisiéramos consultarles cómo va la creación del comercio, que enviamos el {fecha}."
                : $"Quisiéramos consultarles cómo va la solicitud de creación #{numeroSolicitud.Trim().TrimStart('#')}, que enviamos el {fecha}.";
            parrafo = $"Les agradecemos mucho la confirmación de que {comercio} fue aceptado por Análisis de Riesgo. {consulta}";
        }
        else
        {
            parrafo = $"Quisiéramos consultarles cómo va la solicitud de onboarding de {comercio}, que enviamos el {fecha}.";
        }

        return $"""
            Buenos días, estimados:

            Espero que se encuentren muy bien.

            {parrafo}

            Sabemos que manejan muchas gestiones a la vez. Si fuera posible contar con los datos de creación (Merchant y Terminal) en el transcurso del día, nos ayudaría mucho para darle una pronta respuesta al cliente. Si necesitan cualquier información adicional de nuestra parte, con gusto se la hacemos llegar.

            Muchas gracias de antemano por su apoyo.

            {Firma}
            """;
    }

    /// <summary>Seguimiento al banco (N = 4, 6, 8…). Va como respuesta en el hilo con el banco.</summary>
    public static string SeguimientoBanco(string comercio, DateOnly dia0, int n) => $"""
        Buenos días,

        Espero que se encuentren muy bien.

        ¿Podrían indicarnos cuál es el estado actual de la solicitud de onboarding de {comercio}? Fue enviada el {CostaRicaTime.FechaLarga(dia0)} y a la fecha han transcurrido {n} días hábiles. Agradecemos su apoyo para poder atender oportunamente al cliente.

        Quedamos a la espera de su respuesta.

        {Firma}
        """;
}
