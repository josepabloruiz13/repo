using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;
using SwiftPay.Onboarding.Config;
using SwiftPay.Onboarding.Domain;
using GThread = Google.Apis.Gmail.v1.Data.Thread;

namespace SwiftPay.Onboarding.Infra;

/// <summary>Mensaje de Gmail ya decodificado.</summary>
public sealed class Mail
{
    public required string Id { get; init; }
    public required string ThreadId { get; init; }
    public required DateTime FechaCr { get; init; }
    public required string From { get; init; }
    public required IReadOnlyList<string> To { get; init; }
    public required IReadOnlyList<string> Cc { get; init; }
    public required string Subject { get; init; }
    public required string MessageIdHeader { get; init; }
    public required string References { get; init; }
    public required string Cuerpo { get; init; }
    public required IReadOnlyList<string> LabelIds { get; init; }
    public required IReadOnlyList<Adjunto> Adjuntos { get; init; }

    public string FromEmail => DireccionUtil.Email(From);
    public IEnumerable<string> ToEmails => To.Select(DireccionUtil.Email);
    public IEnumerable<string> CcEmails => Cc.Select(DireccionUtil.Email);
}

public sealed record Adjunto(string Filename, string AttachmentId, string MessageId);

public static class DireccionUtil
{
    public static string Email(string direccion) =>
        MailboxAddress.TryParse(direccion, out var m) ? m.Address.ToLowerInvariant() : direccion.Trim().Trim('<', '>').ToLowerInvariant();

    public static string NombreVisible(string direccion) =>
        MailboxAddress.TryParse(direccion, out var m) ? (m.Name ?? "") : "";

    public static bool EsDeDominio(string email, string dominio) =>
        email.EndsWith("@" + dominio, StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Lista(string? header) =>
        string.IsNullOrWhiteSpace(header) ? [] :
        InternetAddressList.TryParse(header, out var lista)
            ? lista.Mailboxes.Select(m => m.ToString()).ToList()
            : header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}

/// <summary>Acceso al buzón con la API de Gmail usando una cuenta de servicio con delegación de dominio.</summary>
public sealed class GmailClient
{
    private const string Me = "me";
    private readonly GmailService _svc;
    private readonly string _buzon;
    private Dictionary<string, string>? _labelIdsPorNombre;

    public GmailClient(GmailSettings cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.ServiceAccountJsonPath))
            throw new InvalidOperationException("Falta Gmail:ServiceAccountJsonPath.");

        // gmail.modify permite leer, etiquetar, crear borradores y enviar. No permite borrar definitivamente.
        var credencial = CredentialFactory.FromFile<ServiceAccountCredential>(cfg.ServiceAccountJsonPath)
            .ToGoogleCredential()
            .CreateScoped(GmailService.Scope.GmailModify)
            .CreateWithUser(cfg.Buzon);

        _svc = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credencial,
            ApplicationName = "SwiftPay.Onboarding",
        });
        _buzon = cfg.Buzon;
    }

    public string Buzon => _buzon;

    // ---------- Lectura ----------

    public async Task<List<string>> BuscarHilosAsync(string query, int max = 200)
    {
        var ids = new List<string>();
        string? page = null;
        do
        {
            var req = _svc.Users.Threads.List(Me);
            req.Q = query;
            req.MaxResults = 100;
            req.PageToken = page;
            var resp = await req.ExecuteAsync();
            if (resp.Threads != null) ids.AddRange(resp.Threads.Select(t => t.Id));
            page = resp.NextPageToken;
        } while (page != null && ids.Count < max);
        return ids;
    }

    public async Task<List<Mail>> BuscarMensajesAsync(string query, int max = 50)
    {
        var req = _svc.Users.Messages.List(Me);
        req.Q = query;
        req.MaxResults = max;
        var resp = await req.ExecuteAsync();
        var lista = new List<Mail>();
        foreach (var m in resp.Messages ?? [])
            lista.Add(Convertir(await _svc.Users.Messages.Get(Me, m.Id).ExecuteAsync()));
        return lista;
    }

    public async Task<bool> ExisteAsync(string query)
    {
        var req = _svc.Users.Messages.List(Me);
        req.Q = query;
        req.MaxResults = 1;
        var resp = await req.ExecuteAsync();
        return resp.Messages is { Count: > 0 };
    }

    public async Task<List<Mail>> ObtenerHiloAsync(string threadId)
    {
        GThread t = await _svc.Users.Threads.Get(Me, threadId).ExecuteAsync();
        return (t.Messages ?? []).Select(Convertir).OrderBy(m => m.FechaCr).ToList();
    }

    public async Task<bool> HayBorradorAsync(string query)
    {
        var req = _svc.Users.Drafts.List(Me);
        req.Q = query;
        req.MaxResults = 1;
        var resp = await req.ExecuteAsync();
        return resp.Drafts is { Count: > 0 };
    }

    public async Task<byte[]> DescargarAdjuntoAsync(Adjunto a)
    {
        var body = await _svc.Users.Messages.Attachments.Get(Me, a.MessageId, a.AttachmentId).ExecuteAsync();
        return DecodificarBase64Url(body.Data);
    }

    // ---------- Etiquetas ----------

    public async Task<string> LabelIdAsync(string nombre)
    {
        if (_labelIdsPorNombre == null)
        {
            var resp = await _svc.Users.Labels.List(Me).ExecuteAsync();
            _labelIdsPorNombre = (resp.Labels ?? []).ToDictionary(l => l.Name, l => l.Id, StringComparer.OrdinalIgnoreCase);
        }
        return _labelIdsPorNombre.TryGetValue(nombre, out var id)
            ? id
            : throw new InvalidOperationException($"La etiqueta \"{nombre}\" no existe en el buzón {_buzon}.");
    }

    public async Task ModificarEtiquetasHiloAsync(string threadId, IEnumerable<string> agregar, IEnumerable<string> quitar)
    {
        var add = new List<string>();
        foreach (var n in agregar) add.Add(await LabelIdAsync(n));
        var rem = new List<string>();
        foreach (var n in quitar) rem.Add(await LabelIdAsync(n));
        if (add.Count == 0 && rem.Count == 0) return;
        await _svc.Users.Threads.Modify(new ModifyThreadRequest { AddLabelIds = add, RemoveLabelIds = rem }, Me, threadId).ExecuteAsync();
    }

    // ---------- Envío ----------

    /// <summary>Envía un correo nuevo, o una respuesta dentro de un hilo si se pasa <paramref name="responderA"/>.</summary>
    public async Task<string> EnviarAsync(IEnumerable<string> para, IEnumerable<string> cc, string asunto, string cuerpo, Mail? responderA = null)
    {
        var mime = Construir(para, cc, asunto, cuerpo, responderA);
        var msg = new Message { Raw = CodificarBase64Url(mime), ThreadId = responderA?.ThreadId };
        var enviado = await _svc.Users.Messages.Send(msg, Me).ExecuteAsync();
        return enviado.Id;
    }

    /// <summary>Crea un borrador (se usa sin destinatario cuando no se tiene el correo del comercio).</summary>
    public async Task<string> CrearBorradorAsync(IEnumerable<string> para, string asunto, string cuerpo)
    {
        var mime = Construir(para, [], asunto, cuerpo, null);
        var draft = await _svc.Users.Drafts.Create(new Draft { Message = new Message { Raw = CodificarBase64Url(mime) } }, Me).ExecuteAsync();
        return draft.Id;
    }

    private MimeMessage Construir(IEnumerable<string> para, IEnumerable<string> cc, string asunto, string cuerpo, Mail? responderA)
    {
        var m = new MimeMessage();
        m.From.Add(new MailboxAddress("Afiliación SwiftPay", _buzon));
        foreach (var p in para) m.To.Add(MailboxAddress.Parse(p));
        foreach (var c in cc) m.Cc.Add(MailboxAddress.Parse(c));
        m.Subject = asunto;
        if (responderA != null && !string.IsNullOrEmpty(responderA.MessageIdHeader))
        {
            m.InReplyTo = responderA.MessageIdHeader;
            foreach (var r in (responderA.References + " " + responderA.MessageIdHeader).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                m.References.Add(r);
        }
        m.Body = new TextPart("plain") { Text = cuerpo };
        return m;
    }

    // ---------- Conversión ----------

    private static Mail Convertir(Message msg)
    {
        var headers = msg.Payload?.Headers ?? [];
        string H(string name) => headers.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";

        var textoPlano = new StringBuilder();
        var textoHtml = new StringBuilder();
        var adjuntos = new List<Adjunto>();
        Recorrer(msg.Payload, msg.Id, textoPlano, textoHtml, adjuntos);
        var cuerpo = textoPlano.Length > 0 ? textoPlano.ToString() : TextUtil.HtmlATexto(textoHtml.ToString());

        return new Mail
        {
            Id = msg.Id,
            ThreadId = msg.ThreadId,
            FechaCr = CostaRicaTime.DesdeUnixMs(msg.InternalDate ?? 0),
            From = H("From"),
            To = DireccionUtil.Lista(H("To")),
            Cc = DireccionUtil.Lista(H("Cc")),
            Subject = H("Subject"),
            MessageIdHeader = H("Message-ID"),
            References = H("References"),
            Cuerpo = cuerpo,
            LabelIds = (IReadOnlyList<string>?)msg.LabelIds?.ToList() ?? [],
            Adjuntos = adjuntos,
        };
    }

    private static void Recorrer(Google.Apis.Gmail.v1.Data.MessagePart? parte, string messageId, StringBuilder plano, StringBuilder html, List<Adjunto> adjuntos)
    {
        if (parte == null) return;
        if (!string.IsNullOrEmpty(parte.Filename) && parte.Body?.AttachmentId != null)
        {
            adjuntos.Add(new Adjunto(parte.Filename, parte.Body.AttachmentId, messageId));
        }
        else if (parte.Body?.Data != null)
        {
            var texto = Encoding.UTF8.GetString(DecodificarBase64Url(parte.Body.Data));
            if (parte.MimeType == "text/plain") plano.AppendLine(texto);
            else if (parte.MimeType == "text/html") html.AppendLine(texto);
        }
        foreach (var p in parte.Parts ?? []) Recorrer(p, messageId, plano, html, adjuntos);
    }

    private static byte[] DecodificarBase64Url(string data)
    {
        var s = data.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }

    private static string CodificarBase64Url(MimeMessage m)
    {
        using var ms = new MemoryStream();
        m.WriteTo(ms);
        return Convert.ToBase64String(ms.ToArray()).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
