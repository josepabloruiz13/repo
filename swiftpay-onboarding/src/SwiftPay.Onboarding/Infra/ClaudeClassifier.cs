using System.Text.Json;
using Anthropic;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using SwiftPay.Onboarding.Config;

namespace SwiftPay.Onboarding.Infra;

public sealed record AnalisisBanco(bool PideInformacion, string? QuePide, bool ConfirmoAceptacion, string? NumeroSolicitud);

public sealed record ContactoExtraido(string? Nombre, string? Email);

/// <summary>
/// Usa la API de Claude solo para las partes que no se pueden resolver con reglas fijas.
/// Si no hay llave configurada o la llamada falla, devuelve null y el programa lo reporta en el resumen.
/// </summary>
public sealed class ClaudeClassifier
{
    private readonly AnthropicClient? _client;
    private readonly string _model;

    public ClaudeClassifier(ClaudeSettings cfg)
    {
        _model = cfg.Model;
        if (!string.IsNullOrWhiteSpace(cfg.ApiKey))
            _client = new AnthropicClient { ApiKey = cfg.ApiKey };
    }

    public bool Disponible => _client != null;

    /// <summary>
    /// Lee los mensajes del banco (posteriores al envío inicial) y responde:
    /// si piden información pendiente, si confirmaron la aceptación del comercio y el número de solicitud de creación.
    /// </summary>
    public async Task<AnalisisBanco?> AnalizarHiloBancoAsync(string comercio, string conversacion)
    {
        const string system = """
            Analizas correos entre SwiftPay (afiliacion@swiftpay.one) y su banco adquirente sobre la solicitud de onboarding de un comercio.
            Responde solo con lo que el texto dice de forma explícita; si algo no está claro, responde false o null. No inventes números.
            - pide_informacion: true si el ÚLTIMO mensaje del banco pide datos o documentos faltantes que SwiftPay aún no respondió.
            - que_pide: resumen breve (máximo 200 caracteres) de lo que piden, o null.
            - confirmo_aceptacion: true si el banco confirmó en algún mensaje que el comercio fue aceptado (por ejemplo por Análisis de Riesgo).
            - numero_solicitud: el número de la solicitud de creación tal como lo escribió el banco (solo dígitos o el código exacto), o null si no lo dio.
            """;

        var schema = Esquema(new Dictionary<string, object>
        {
            ["pide_informacion"] = new { type = "boolean" },
            ["que_pide"] = new { type = new[] { "string", "null" } },
            ["confirmo_aceptacion"] = new { type = "boolean" },
            ["numero_solicitud"] = new { type = new[] { "string", "null" } },
        });

        var json = await LlamarAsync(system, $"Comercio: {comercio}\n\nCorreos (del más antiguo al más reciente):\n\n{conversacion}", schema);
        if (json is null) return null;
        var r = json.Value;
        return new AnalisisBanco(
            r.GetProperty("pide_informacion").GetBoolean(),
            Str(r, "que_pide"),
            r.GetProperty("confirmo_aceptacion").GetBoolean(),
            Str(r, "numero_solicitud"));
    }

    /// <summary>Extrae nombre y correo del contacto desde el formulario de afiliación (solo si las reglas no lo encontraron).</summary>
    public async Task<ContactoExtraido?> ExtraerContactoAsync(string comercio, string formulario)
    {
        const string system = """
            Extraes el nombre y el correo electrónico de la persona de contacto de un formulario de afiliación de comercio.
            Copia los datos exactamente como aparecen. Si un dato no aparece, responde null. No inventes.
            """;
        var schema = Esquema(new Dictionary<string, object>
        {
            ["nombre"] = new { type = new[] { "string", "null" } },
            ["email"] = new { type = new[] { "string", "null" } },
        });
        var json = await LlamarAsync(system, $"Comercio: {comercio}\n\nFormulario:\n{formulario}", schema);
        if (json is null) return null;
        return new ContactoExtraido(Str(json.Value, "nombre"), Str(json.Value, "email"));
    }

    private async Task<JsonElement?> LlamarAsync(string system, string usuario, Dictionary<string, JsonElement> schema)
    {
        if (_client is null) return null;
        try
        {
            var resp = await _client.Beta.Messages.Create(new MessageCreateParams
            {
                Model = _model,
                MaxTokens = 4096,
                System = system,
                // Si un clasificador de seguridad rechaza la solicitud, el servidor la reintenta con otro modelo.
                Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
                Fallbacks = new Default(),
                OutputConfig = new BetaOutputConfig
                {
                    Effort = Effort.Low,
                    Format = new BetaJsonOutputFormat { Schema = schema },
                },
                Messages = [new BetaUserMessageParam { Content = usuario }],
            });

            if (resp.StopReason == "refusal") return null;
            var texto = string.Concat(resp.Content.Select(b => b.TryPickText(out var t) ? t.Text : ""));
            return JsonDocument.Parse(texto).RootElement.Clone();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Claude] Error: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static Dictionary<string, JsonElement> Esquema(Dictionary<string, object> propiedades) => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(propiedades),
        ["required"] = JsonSerializer.SerializeToElement(propiedades.Keys.ToArray()),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    private static string? Str(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()!.Trim()
            : null;
}
