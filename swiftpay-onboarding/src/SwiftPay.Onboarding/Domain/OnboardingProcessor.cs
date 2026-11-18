using System.Text;
using System.Text.RegularExpressions;
using SwiftPay.Onboarding.Config;
using SwiftPay.Onboarding.Infra;

namespace SwiftPay.Onboarding.Domain;

/// <summary>Reglas del proceso de onboarding (mismo comportamiento que la tarea programada original).</summary>
public sealed partial class OnboardingProcessor
{
    // Etiquetas de Gmail (ya existen en el buzón).
    public const string Et1Enviado = "Onboarding/1-Enviado al banco";
    public const string Et2PideInfo = "Onboarding/2-Banco pide información";
    public const string Et3SlaVencido = "Onboarding/3-SLA vencido";
    public const string Et4Terminal = "Onboarding/4-Terminal recibido - falta bienvenida";
    public const string EtCompletada = "Afiliaciones completadas";

    // Tipos de envío para la bitácora (onb.Envio).
    private const string TInicio = "INICIO";
    private const string TSegComercio = "SEGUIMIENTO_COMERCIO";
    private const string TRecordatorioBanco = "RECORDATORIO_BANCO";
    private const string TSegBanco = "SEGUIMIENTO_BANCO";
    private const string TBienvenida = "BIENVENIDA";
    private const string TAlertaFallida = "ALERTA_FALLIDA";
    private const string TAlertaOk = "ALERTA_OK_SIN_BIENVENIDA";

    private readonly AppSettings _cfg;
    private readonly GmailClient _gmail;
    private readonly Db _db;
    private readonly ClaudeClassifier _claude;
    private readonly SwiftPortalsClient? _portals;
    private readonly BusinessCalendar _cal;
    private readonly RunSummary _r;
    private readonly DateOnly _hoy;

    private string Banco => _cfg.Gmail.DominioBanco;
    private string Interno => _cfg.Gmail.DominioInterno;
    private string Buzon => _cfg.Gmail.Buzon;

    public OnboardingProcessor(AppSettings cfg, GmailClient gmail, Db db, ClaudeClassifier claude,
        SwiftPortalsClient? portals, BusinessCalendar cal, RunSummary resumen, DateOnly hoy)
    {
        _cfg = cfg; _gmail = gmail; _db = db; _claude = claude; _portals = portals; _cal = cal; _r = resumen; _hoy = hoy;
    }

    // =====================================================================
    // Punto de entrada
    // =====================================================================

    public async Task EjecutarAsync()
    {
        if (!_claude.Disponible)
            _r.Avisos.Add("La API de Claude no está configurada: los mensajes del banco que no traen datos de creación no se pueden clasificar y se reportan para revisión manual.");

        var query = $"subject:\"Solicitud de onboarding\" newer_than:{_cfg.Gmail.DiasBusqueda}d";
        var hilos = await _gmail.BuscarHilosAsync(query);
        var idCompletada = await _gmail.LabelIdAsync(EtCompletada);

        foreach (var threadId in hilos)
        {
            List<Mail> mensajes;
            try { mensajes = await _gmail.ObtenerHiloAsync(threadId); }
            catch (Exception ex) { _r.Dudas.Add($"No se pudo leer el hilo {threadId}: {ex.Message}"); continue; }

            if (mensajes.Any(m => m.LabelIds.Contains(idCompletada))) continue;

            // Día 0: correo enviado desde el buzón a alguna dirección del banco con asunto "Solicitud de onboarding <COMERCIO>".
            var dia0Msg = mensajes.FirstOrDefault(m =>
                m.FromEmail.Equals(Buzon, StringComparison.OrdinalIgnoreCase) &&
                m.ToEmails.Concat(m.CcEmails).Any(e => DireccionUtil.EsDeDominio(e, Banco)) &&
                ComercioDesdeAsunto(m.Subject) != null);
            if (dia0Msg is null) continue; // revisiones internas u otros hilos

            var comercio = ComercioDesdeAsunto(dia0Msg.Subject)!;
            try
            {
                await ProcesarAsync(threadId, comercio, dia0Msg, mensajes);
            }
            catch (Exception ex)
            {
                _r.Dudas.Add($"{comercio}: error inesperado, se reintentará en la próxima ejecución ({ex.GetType().Name}: {ex.Message}).");
            }
        }
    }

    private async Task ProcesarAsync(string threadId, string comercio, Mail dia0Msg, List<Mail> mensajes)
    {
        var dia0 = DateOnly.FromDateTime(dia0Msg.FechaCr);
        var n = _cal.DiasHabilesDesde(dia0, _hoy);

        // Datos de creación: el bloque válido es el del mensaje del banco MÁS RECIENTE que lo traiga.
        (Mail msg, DatosCreacion datos)? creacion = null;
        foreach (var m in mensajes.Where(m => DireccionUtil.EsDeDominio(m.FromEmail, Banco)).OrderByDescending(m => m.FechaCr))
        {
            var d = CreationDataParser.Parse(m.Cuerpo);
            if (d != null) { creacion = (m, d); break; }
        }

        var contacto = await BuscarContactoAsync(comercio);

        if (creacion is null)
            await ProcesarPendienteBancoAsync(threadId, comercio, dia0Msg, dia0, n, mensajes, contacto);
        else
            await ProcesarTerminalRecibidoAsync(threadId, comercio, dia0Msg, dia0, n, creacion.Value.msg, creacion.Value.datos, contacto);
    }

    // =====================================================================
    // A) Sin datos de creación (pendiente con el banco)
    // =====================================================================

    private async Task ProcesarPendienteBancoAsync(string threadId, string comercio, Mail dia0Msg, DateOnly dia0, int n,
        List<Mail> mensajes, Contacto? contacto)
    {
        var agregar = new List<string> { Et1Enviado };
        var quitar = new List<string> { Et4Terminal };

        // ¿El banco pide información que no se ha respondido?
        var ultimoBanco = mensajes.Where(m => m.Id != dia0Msg.Id && DireccionUtil.EsDeDominio(m.FromEmail, Banco)).MaxBy(m => m.FechaCr);
        var ultimoNuestro = mensajes.Where(m => m.FromEmail.Equals(Buzon, StringComparison.OrdinalIgnoreCase)).MaxBy(m => m.FechaCr);
        var bancoSinResponder = ultimoBanco != null && (ultimoNuestro == null || ultimoBanco.FechaCr > ultimoNuestro.FechaCr);

        AnalisisBanco? analisis = null;
        if (ultimoBanco != null && (bancoSinResponder || n == 3))
            analisis = await _claude.AnalizarHiloBancoAsync(comercio, Conversacion(mensajes));

        if (bancoSinResponder)
        {
            if (analisis is null)
            {
                _r.Dudas.Add($"{comercio}: hay un mensaje del banco del {ultimoBanco!.FechaCr:dd/MM HH:mm} sin datos de creación que no pude clasificar. Revísalo.");
            }
            else if (analisis.PideInformacion)
            {
                agregar.Add(Et2PideInfo);
                _r.PeticionesBanco.Add($"{comercio}: el banco pide información ({analisis.QuePide ?? "ver el hilo"}). Mensaje del {ultimoBanco!.FechaCr:dd/MM HH:mm}.");
            }
            else
            {
                quitar.Add(Et2PideInfo);
            }
        }
        else
        {
            quitar.Add(Et2PideInfo);
        }

        // SLA del banco: vence en N = 3; vencido desde N = 4.
        if (n >= 4) agregar.Add(Et3SlaVencido);
        await EtiquetarAsync(threadId, comercio, agregar, quitar);

        var etapa = n >= 4 ? "1-Enviado al banco (SLA vencido)" : "1-Enviado al banco";
        _r.Activos.Add($"{comercio}: {etapa}, N = {n} (día 0: {dia0:dd/MM/yyyy}).");
        if (n == 3) _r.SlaHoy.Add($"{comercio} (enviado el {CostaRicaTime.FechaLarga(dia0)}).");
        if (n >= 4) _r.SlaVencidos.Add($"{comercio}: N = {n}.");

        // Correo de INICIO al comercio (una sola vez).
        if (!await ExisteInicioAsync(threadId, comercio, contacto))
        {
            await EnviarAlComercioAsync(threadId, comercio, contacto, TInicio, "-",
                Templates.Inicio(contacto?.NombrePila ?? Templates.SaludoSinContacto, comercio));
        }

        // Correos al banco (respuesta en el hilo con el banco).
        if (n == 3)
        {
            var cuerpo = Templates.RecordatorioVencimientoBanco(comercio, dia0,
                analisis?.ConfirmoAceptacion ?? false, analisis?.ConfirmoAceptacion == true ? analisis.NumeroSolicitud : null);
            if (analisis is null && ultimoBanco != null)
                _r.Dudas.Add($"{comercio}: el recordatorio al banco se envió en la versión sin confirmación de aceptación porque no pude analizar las respuestas del banco.");
            await ResponderAlBancoAsync(threadId, comercio, dia0Msg, mensajes, TRecordatorioBanco, n, cuerpo);
        }
        else if (n >= 4 && n % 2 == 0)
        {
            await ResponderAlBancoAsync(threadId, comercio, dia0Msg, mensajes, TSegBanco, n, Templates.SeguimientoBanco(comercio, dia0, n));
        }

        // Seguimiento al comercio: N = 2 y 4 normal; N = 6, 8, 10… con disculpa.
        if (n is 2 or 4)
            await EnviarAlComercioAsync(threadId, comercio, contacto, TSegComercio, n.ToString(),
                Templates.Seguimiento(contacto?.NombrePila ?? Templates.SaludoSinContacto, comercio));
        else if (n >= 6 && n % 2 == 0)
            await EnviarAlComercioAsync(threadId, comercio, contacto, TSegComercio, n.ToString(),
                Templates.SeguimientoDisculpa(contacto?.NombrePila ?? Templates.SaludoSinContacto, comercio));

        await GuardarAsync(threadId, comercio, dia0, n, etapa, contacto, null, null, false);
    }

    // =====================================================================
    // B) Con datos de creación (terminal recibido)
    // =====================================================================

    private async Task ProcesarTerminalRecibidoAsync(string threadId, string comercio, Mail dia0Msg, DateOnly dia0, int n,
        Mail msgTerminal, DatosCreacion datos, Contacto? contacto)
    {
        await EtiquetarAsync(threadId, comercio, [Et4Terminal], [Et1Enviado, Et2PideInfo, Et3SlaVencido]);
        var terminalesTxt = string.Join(", ", datos.Terminales.Select(t => $"Terminal ({t.Tipo}): {t.Numero}"));

        // ¿Ya se envió la bienvenida después de recibir el terminal?
        if (await ExisteBienvenidaAsync(comercio, contacto, msgTerminal.FechaCr))
        {
            await EtiquetarAsync(threadId, comercio, [EtCompletada], [Et4Terminal]);
            _r.Terminales.Add($"{comercio}: ya tenía bienvenida enviada; se movió a \"{EtCompletada}\". Merchant {datos.Merchant}, {terminalesTxt}.");
            await GuardarAsync(threadId, comercio, dia0, n, "Completado", contacto, datos.Merchant, null, true);
            return;
        }

        // Si ya se activó (alerta "Activación OK" enviada), no se vuelve a llamar al API.
        if (await ExisteAlertaOkAsync(threadId, comercio, msgTerminal.FechaCr))
        {
            _r.Terminales.Add($"{comercio}: activación ya exitosa antes; falta enviar la bienvenida manualmente. Merchant {datos.Merchant}, {terminalesTxt}.");
            await GuardarAsync(threadId, comercio, dia0, n, "4-Activado, falta bienvenida", contacto, datos.Merchant, null, false);
            return;
        }

        // Cédula del Excel adjunto al correo de día 0.
        var (cedula, motivoSinCedula) = await LeerCedulaAsync(dia0Msg);

        var resultados = new List<(Terminal t, ResultadoApi res)>();
        string? motivoFallo = null;

        if (cedula is null)
        {
            motivoFallo = $"No se encontró la cédula en el Excel ({motivoSinCedula}). No se llamó al API.";
        }
        else if (_cfg.DryRun)
        {
            _r.Terminales.Add($"{comercio}: Merchant {datos.Merchant}, {terminalesTxt}, cédula {cedula}. [SIMULACIÓN] Se llamaría al API de activación una vez por terminal.");
            await GuardarAsync(threadId, comercio, dia0, n, "4-Terminal recibido", contacto, datos.Merchant, cedula, false);
            return;
        }
        else if (_portals is null)
        {
            motivoFallo = "La llave del API de SwiftPortals no está configurada en el servidor. No se llamó al API.";
        }
        else
        {
            foreach (var t in datos.Terminales)
            {
                // Si una ejecución anterior ya activó este terminal con éxito, no se vuelve a llamar.
                if (await _db.ActivacionExitosaRegistradaAsync(threadId, t.Numero))
                {
                    resultados.Add((t, new ResultadoApi(true, null, "ya activado en una ejecución anterior")));
                    continue;
                }
                var res = await _portals.ActivarAsync(cedula, t.Numero);
                resultados.Add((t, res));
                await _db.RegistrarLlamadaAsync(threadId, comercio, cedula, t.Numero, t.Tipo, res.CodigoHttp, res.Exito, res.Detalle);
            }
            if (resultados.Any(x => !x.res.Exito))
                motivoFallo = resultados.Any(x => x.res.Detalle.Contains("403") && x.res.CodigoHttp is null)
                    ? "La red del servidor bloqueó la conexión a swiftportals.com."
                    : "El API devolvió error; no se envió la bienvenida. Activar manualmente o revisar.";
        }

        var resultadoTxt = resultados.Count == 0
            ? "sin llamadas al API"
            : string.Join("; ", resultados.Select(x => $"Terminal ({x.t.Tipo}) {x.t.Numero} → {x.res.Texto}"));
        _r.Terminales.Add($"{comercio}: Merchant {datos.Merchant}, {terminalesTxt}, cédula {cedula ?? "no encontrada"}. Resultado: {resultadoTxt}.");

        if (motivoFallo != null)
        {
            await EnviarAlertaAsync(threadId, comercio, TAlertaFallida, Templates.AsuntoAlertaFallida(comercio),
                CuerpoAlerta(comercio, cedula, datos, resultados, motivoFallo));
            await GuardarAsync(threadId, comercio, dia0, n, "4-Terminal recibido (activación fallida)", contacto, datos.Merchant, cedula, false);
            return;
        }

        // Activación exitosa → bienvenida.
        bool bienvenidaEnviada;
        try
        {
            bienvenidaEnviada = await EnviarAlComercioAsync(threadId, comercio, contacto, TBienvenida, "-",
                Templates.Bienvenida(contacto?.NombrePila ?? Templates.SaludoSinContacto, comercio));
        }
        catch (Exception ex)
        {
            _r.Dudas.Add($"{comercio}: la activación fue exitosa pero falló el envío de la bienvenida ({ex.Message}).");
            await EnviarAlertaAsync(threadId, comercio, TAlertaOk, Templates.AsuntoAlertaOkSinBienvenida(comercio),
                CuerpoAlerta(comercio, cedula, datos, resultados, "Activación OK – falta bienvenida. Enviarla manualmente."));
            await GuardarAsync(threadId, comercio, dia0, n, "4-Activado, falta bienvenida", contacto, datos.Merchant, cedula, false);
            return;
        }

        if (bienvenidaEnviada)
        {
            await EtiquetarAsync(threadId, comercio, [EtCompletada], [Et4Terminal]);
            await GuardarAsync(threadId, comercio, dia0, n, "Completado", contacto, datos.Merchant, cedula, true);
        }
        else
        {
            // Quedó en borrador sin destinatario: se deja la etiqueta 4.
            await GuardarAsync(threadId, comercio, dia0, n, "4-Activado, bienvenida en borrador", contacto, datos.Merchant, cedula, false);
        }
    }

    // =====================================================================
    // Envíos
    // =====================================================================

    /// <summary>
    /// Envía un correo al comercio, verificando confidencialidad y duplicados.
    /// Devuelve true si se envió; false si quedó en borrador o no se envió por duplicado.
    /// </summary>
    private async Task<bool> EnviarAlComercioAsync(string threadId, string comercio, Contacto? contacto, string tipo, string clave, Correo correo)
    {
        if (await _db.EnvioRegistradoAsync(threadId, tipo, clave)) return false;

        if (contacto is null)
        {
            if (await _gmail.HayBorradorAsync($"subject:\"{correo.Asunto}\""))
                return false;
            if (_cfg.DryRun)
            {
                _r.Borradores.Add($"[SIMULACIÓN] {comercio}: se crearía un borrador sin destinatario ({TipoLegible(tipo)}) porque no se encontró el correo del contacto.");
                return false;
            }
            var draftId = await _gmail.CrearBorradorAsync([], correo.Asunto, correo.Cuerpo);
            await _db.RegistrarEnvioAsync(threadId, comercio, tipo, clave, "", correo.Asunto, draftId, esBorrador: true);
            _r.Borradores.Add($"{comercio}: {TipoLegible(tipo)} quedó en borrador sin destinatario porque no se encontró con seguridad el correo del contacto.");
            return false;
        }

        // Si es un seguimiento, también se revisa en Enviados que no haya salido hoy (por ejemplo, desde la tarea anterior).
        if (tipo == TSegComercio && await EnviadoConAsuntoAsync(correo.Asunto, contacto.Email, InicioDeHoyCr()))
            return false;
        if (tipo == TBienvenida && await EnviadoConAsuntoAsync(correo.Asunto, contacto.Email, null))
            return false;

        VerificarConfidencialidad(contacto.Email, correo);

        if (_cfg.DryRun)
        {
            _r.Enviados.Add($"[SIMULACIÓN] {comercio}: {TipoLegible(tipo)} a {contacto.Email}.");
            return true;
        }
        var id = await _gmail.EnviarAsync([contacto.Email], [], correo.Asunto, correo.Cuerpo);
        await _db.RegistrarEnvioAsync(threadId, comercio, tipo, clave, contacto.Email, correo.Asunto, id, esBorrador: false);
        _r.Enviados.Add($"{comercio}: {TipoLegible(tipo)} a {contacto.Email}.");
        return true;
    }

    /// <summary>Recordatorio o seguimiento al banco, como respuesta dentro del hilo con el banco.</summary>
    private async Task ResponderAlBancoAsync(string threadId, string comercio, Mail dia0Msg, List<Mail> mensajes, string tipo, int n, string cuerpo)
    {
        var clave = n.ToString();
        if (await _db.EnvioRegistradoAsync(threadId, tipo, clave)) return;

        // ¿Ya salió hoy un correo nuestro en este hilo (por ejemplo, desde la tarea anterior)? Entonces no se duplica.
        if (mensajes.Any(m => m.Id != dia0Msg.Id && m.FromEmail.Equals(Buzon, StringComparison.OrdinalIgnoreCase)
                              && DateOnly.FromDateTime(m.FechaCr) == _hoy))
            return;

        // Destinatarios: los del último mensaje del banco (o del envío original si el banco no ha respondido).
        var ultimoBanco = mensajes.Where(m => DireccionUtil.EsDeDominio(m.FromEmail, Banco)).MaxBy(m => m.FechaCr);
        IEnumerable<string> para, cc;
        Mail responderA;
        if (ultimoBanco != null)
        {
            para = new[] { ultimoBanco.FromEmail }.Concat(ultimoBanco.ToEmails);
            cc = ultimoBanco.CcEmails;
            responderA = mensajes.MaxBy(m => m.FechaCr)!;
        }
        else
        {
            para = dia0Msg.ToEmails;
            cc = dia0Msg.CcEmails;
            responderA = mensajes.MaxBy(m => m.FechaCr)!;
        }
        var paraL = para.Where(e => !e.Equals(Buzon, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();
        var ccL = cc.Where(e => !e.Equals(Buzon, StringComparison.OrdinalIgnoreCase) && !paraL.Contains(e)).Distinct().ToList();
        // Nunca se incluye al comercio ni a nadie fuera del banco y SwiftPay en los correos al banco.
        paraL = paraL.Where(e => DireccionUtil.EsDeDominio(e, Banco) || DireccionUtil.EsDeDominio(e, Interno)).ToList();
        ccL = ccL.Where(e => DireccionUtil.EsDeDominio(e, Banco) || DireccionUtil.EsDeDominio(e, Interno)).ToList();
        if (paraL.Count == 0)
        {
            _r.Dudas.Add($"{comercio}: no se pudo determinar a quién responder en el hilo con el banco; no se envió el {TipoLegible(tipo)}.");
            return;
        }

        var asunto = dia0Msg.Subject.StartsWith("RE:", StringComparison.OrdinalIgnoreCase) ? dia0Msg.Subject : "RE: " + dia0Msg.Subject;
        if (_cfg.DryRun)
        {
            _r.Enviados.Add($"[SIMULACIÓN] {comercio}: {TipoLegible(tipo)} (N = {n}) a {string.Join(", ", paraL)} en el hilo \"{dia0Msg.Subject}\".");
            return;
        }
        var id = await _gmail.EnviarAsync(paraL, ccL, asunto, cuerpo, responderA);
        await _db.RegistrarEnvioAsync(threadId, comercio, tipo, clave, string.Join(", ", paraL.Concat(ccL)), asunto, id, esBorrador: false);
        _r.Enviados.Add($"{comercio}: {TipoLegible(tipo)} (N = {n}) a {string.Join(", ", paraL)} en el hilo \"{dia0Msg.Subject}\".");
    }

    /// <summary>Alerta interna solo a afiliacion@swiftpay.one, en un hilo nuevo. Una por comercio por día.</summary>
    private async Task EnviarAlertaAsync(string threadId, string comercio, string tipo, string asunto, string cuerpo)
    {
        var clave = _hoy.ToString("yyyy-MM-dd");
        if (await _db.EnvioRegistradoAsync(threadId, tipo, clave)) return;
        if (await EnviadoConAsuntoAsync(asunto, Buzon, InicioDeHoyCr())) return;

        if (_cfg.DryRun)
        {
            _r.Enviados.Add($"[SIMULACIÓN] Alerta interna a {Buzon}: \"{asunto}\".");
            return;
        }
        var id = await _gmail.EnviarAsync([Buzon], [], asunto, cuerpo);
        await _db.RegistrarEnvioAsync(threadId, comercio, tipo, clave, Buzon, asunto, id, esBorrador: false);
        _r.Enviados.Add($"Alerta interna a {Buzon}: \"{asunto}\".");
    }

    private string CuerpoAlerta(string comercio, string? cedula, DatosCreacion datos, List<(Terminal t, ResultadoApi res)> resultados, string motivo)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Comercio: {comercio}");
        sb.AppendLine($"Fecha y hora (Costa Rica): {CostaRicaTime.Ahora():dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Cédula usada: {cedula ?? "no encontrada"}");
        sb.AppendLine($"Merchant: {datos.Merchant}");
        foreach (var t in datos.Terminales)
        {
            var r = resultados.FirstOrDefault(x => x.t.Numero == t.Numero).res;
            sb.AppendLine(r is null
                ? $"Terminal ({t.Tipo}): {t.Numero} → resultado: no se llamó al API"
                : $"Terminal ({t.Tipo}): {t.Numero} → resultado: {r.Texto} — respuesta: {TextUtil.Recortar(r.Detalle, 300)}");
        }
        sb.AppendLine($"Motivo / acción: {motivo}");
        sb.AppendLine("La tarea reintentará en la próxima ejecución.");
        return sb.ToString();
    }

    /// <summary>Regla de confidencialidad: un correo al comercio no puede ir al banco ni mencionarlo.</summary>
    private void VerificarConfidencialidad(string destinatario, Correo correo)
    {
        if (DireccionUtil.EsDeDominio(destinatario, Banco))
            throw new InvalidOperationException($"Bloqueado: el destinatario {destinatario} es del banco.");
        var texto = TextUtil.Normalizar(correo.Asunto + " " + correo.Cuerpo);
        if (texto.Contains("lafise") || Regex.IsMatch(texto, "\\bbanco\\b"))
            throw new InvalidOperationException("Bloqueado: el correo al comercio menciona al banco.");
    }

    // =====================================================================
    // Duplicados
    // =====================================================================

    private async Task<bool> ExisteInicioAsync(string threadId, string comercio, Contacto? contacto)
    {
        if (await _db.EnvioRegistradoAsync(threadId, TInicio, "-")) return true;
        if (await _gmail.HayBorradorAsync($"subject:\"{Templates.AsuntoInicio(comercio)}\"")) return true;
        if (contacto is null) return false;
        var enviados = await _gmail.BuscarMensajesAsync($"in:sent to:{contacto.Email} newer_than:{_cfg.Gmail.DiasBusqueda + 30}d", 50);
        return enviados.Any(m =>
            TextUtil.Normalizar(m.Subject).Contains("estado de afiliacion swiftpay") ||
            TextUtil.Normalizar(m.Subject) == TextUtil.Normalizar(Templates.AsuntoInicio(comercio)));
    }

    private async Task<bool> ExisteBienvenidaAsync(string comercio, Contacto? contacto, DateTime despuesDe)
    {
        var desde = new DateTimeOffset(despuesDe, TimeSpan.FromHours(-6)).ToUnixTimeSeconds();
        var enviados = await _gmail.BuscarMensajesAsync($"in:sent subject:\"Bienvenido a SwiftPay\" after:{desde}", 50);
        return enviados.Any(m =>
            (contacto != null && m.ToEmails.Contains(contacto.Email)) ||
            TextUtil.ClaveComercio(m.Cuerpo).Contains(TextUtil.ClaveComercio(comercio)));
    }

    private async Task<bool> ExisteAlertaOkAsync(string threadId, string comercio, DateTime despuesDe)
    {
        var desde = new DateTimeOffset(despuesDe, TimeSpan.FromHours(-6)).ToUnixTimeSeconds();
        return await EnviadoConAsuntoAsync(Templates.AsuntoAlertaOkSinBienvenida(comercio), Buzon, desde);
    }

    private async Task<bool> EnviadoConAsuntoAsync(string asunto, string para, long? desdeUnix)
    {
        var q = $"in:sent to:{para}" + (desdeUnix is { } d ? $" after:{d}" : $" newer_than:{_cfg.Gmail.DiasBusqueda + 30}d");
        var enviados = await _gmail.BuscarMensajesAsync(q, 50);
        var objetivo = TextUtil.Normalizar(asunto);
        return enviados.Any(m => TextUtil.Normalizar(m.Subject) == objetivo);
    }

    private long InicioDeHoyCr() =>
        new DateTimeOffset(_hoy.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-6)).ToUnixTimeSeconds();

    // =====================================================================
    // Contacto del comercio y cédula
    // =====================================================================

    public sealed record Contacto(string NombrePila, string Email);

    private readonly Dictionary<string, Contacto?> _cacheContacto = new();

    private async Task<Contacto?> BuscarContactoAsync(string comercio)
    {
        if (_cacheContacto.TryGetValue(comercio, out var c)) return c;
        c = await BuscarContactoSinCacheAsync(comercio);
        _cacheContacto[comercio] = c;
        return c;
    }

    private async Task<Contacto?> BuscarContactoSinCacheAsync(string comercio)
    {
        // 1) Formulario automático de afiliación.
        var formularios = await _gmail.BuscarMensajesAsync(
            $"from:{_cfg.Gmail.RemitenteFormulario} subject:SwiftPay newer_than:{_cfg.Gmail.DiasBusqueda + 60}d", 100);
        var form = formularios
            .Where(m => EsAsuntoFormulario(m.Subject, out var nombre) && TextUtil.MismoComercio(nombre, comercio))
            .MaxBy(m => m.FechaCr);
        if (form != null)
        {
            var (nombre, email) = ParsearFormulario(form.Cuerpo);
            if ((nombre is null || email is null) && _claude.Disponible)
            {
                var ext = await _claude.ExtraerContactoAsync(comercio, TextUtil.Recortar(form.Cuerpo, 8000));
                nombre ??= ext?.Nombre;
                email ??= ext?.Email;
            }
            if (nombre != null && email != null && EsEmailValido(email))
                return new Contacto(NombrePila(nombre), email.ToLowerInvariant());
            _r.Dudas.Add($"{comercio}: el formulario de afiliación no trae con claridad nombre y correo del contacto.");
            return null;
        }

        // 2) Correos con direcciones externas (ni del banco ni de SwiftPay) que mencionen el comercio.
        var clave = TextUtil.ClaveComercio(comercio);
        if (clave.Length < 3) return null;
        var mensajes = await _gmail.BuscarMensajesAsync($"\"{clave}\" newer_than:{_cfg.Gmail.DiasBusqueda + 60}d", 50);
        var externos = mensajes
            .SelectMany(m => new[] { m.From }.Concat(m.To).Concat(m.Cc))
            .Where(d => { var e = DireccionUtil.Email(d); return !DireccionUtil.EsDeDominio(e, Banco) && !DireccionUtil.EsDeDominio(e, Interno); })
            .GroupBy(DireccionUtil.Email)
            .ToList();
        if (externos.Count == 1)
        {
            var nombreVisible = externos[0].Select(DireccionUtil.NombreVisible).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
            if (nombreVisible != null)
                return new Contacto(NombrePila(nombreVisible), externos[0].Key);
        }
        if (externos.Count > 1)
            _r.Dudas.Add($"{comercio}: encontré varias direcciones externas posibles ({string.Join(", ", externos.Select(g => g.Key).Take(5))}); no elegí ninguna.");
        return null;
    }

    private async Task<(string? cedula, string? motivo)> LeerCedulaAsync(Mail dia0Msg)
    {
        var adjunto = dia0Msg.Adjuntos
            .Where(a => a.Filename.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Filename.StartsWith("Comercios", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        if (adjunto is null) return (null, "el correo de día 0 no tiene un Excel adjunto");
        var bytes = await _gmail.DescargarAdjuntoAsync(adjunto);
        using var ms = new MemoryStream(bytes);
        var r = CedulaReader.Leer(ms);
        return (r.Cedula, r.Motivo);
    }

    // =====================================================================
    // Utilidades
    // =====================================================================

    private async Task EtiquetarAsync(string threadId, string comercio, IEnumerable<string> agregar, IEnumerable<string> quitar)
    {
        if (_cfg.DryRun) return;
        try { await _gmail.ModificarEtiquetasHiloAsync(threadId, agregar, quitar.Except(agregar)); }
        catch (Exception ex) { _r.Dudas.Add($"{comercio}: no se pudieron actualizar las etiquetas ({ex.Message})."); }
    }

    private async Task GuardarAsync(string threadId, string comercio, DateOnly dia0, int n, string etapa, Contacto? contacto,
        string? merchant, string? cedula, bool completado)
    {
        if (_cfg.DryRun) return;
        await _db.GuardarOnboardingAsync(threadId, comercio, dia0, n, etapa, contacto?.NombrePila, contacto?.Email, merchant, cedula, completado);
    }

    private string Conversacion(List<Mail> mensajes)
    {
        var sb = new StringBuilder();
        foreach (var m in mensajes)
        {
            var quien = DireccionUtil.EsDeDominio(m.FromEmail, Banco) ? "BANCO" : m.FromEmail.Equals(Buzon, StringComparison.OrdinalIgnoreCase) ? "SWIFTPAY" : m.FromEmail;
            sb.AppendLine($"--- {m.FechaCr:yyyy-MM-dd HH:mm} | {quien}");
            sb.AppendLine(TextUtil.Recortar(TextUtil.QuitarCitado(m.Cuerpo), 4000));
        }
        return sb.ToString();
    }

    public static string? ComercioDesdeAsunto(string asunto)
    {
        var m = AsuntoOnboarding().Match(asunto);
        return m.Success ? m.Groups["c"].Value.Trim() : null;
    }

    public static bool EsAsuntoFormulario(string asunto, out string comercio)
    {
        var m = AsuntoFormulario().Match(asunto);
        comercio = m.Success ? m.Groups["c"].Value.Trim() : "";
        return m.Success;
    }

    public static (string? nombre, string? email) ParsearFormulario(string cuerpo)
    {
        var texto = cuerpo.Replace("\r\n", "\n");
        var nombre = CampoNombre().Match(texto) is { Success: true } mn ? mn.Groups["v"].Value.Trim().Trim('*').Trim() : null;
        var email = CampoEmail().Match(texto) is { Success: true } me ? me.Groups["v"].Value.Trim() : null;
        return (string.IsNullOrWhiteSpace(nombre) ? null : nombre, string.IsNullOrWhiteSpace(email) ? null : email);
    }

    public static string NombrePila(string nombreCompleto) =>
        nombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombreCompleto.Trim();

    private static bool EsEmailValido(string e) => Regex.IsMatch(e, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static string TipoLegible(string tipo) => tipo switch
    {
        TInicio => "correo de INICIO",
        TSegComercio => "SEGUIMIENTO AL COMERCIO",
        TRecordatorioBanco => "RECORDATORIO DE VENCIMIENTO AL BANCO",
        TSegBanco => "SEGUIMIENTO AL BANCO",
        TBienvenida => "BIENVENIDA",
        TAlertaFallida => "ALERTA de activación fallida",
        TAlertaOk => "ALERTA activación OK sin bienvenida",
        _ => tipo,
    };

    [GeneratedRegex(@"^\s*(?:(?:re|rv|fw|fwd)\s*:\s*)*Solicitud de onboarding\s+(?<c>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AsuntoOnboarding();

    [GeneratedRegex(@"Af+iliaci[oó]n a SwiftPay\s*-\s*(?<c>.+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AsuntoFormulario();

    [GeneratedRegex(@"^[\s*]*Nombre(?!\s+(?:del?\s+(?:comercio|negocio|la\s+empresa)|comercial))[^:\n]{0,30}:[\s*]*(?<v>[^\n]+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex CampoNombre();

    [GeneratedRegex(@"^[\s*]*(?:E-?mail|Correo(?: electr[oó]nico)?)[^:\n]{0,30}:[\s*]*(?<v>[^\s@]+@[^\s@]+\.[^\s*]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex CampoEmail();
}
