using System.Net.Http.Json;
using SwiftPay.Onboarding.Config;

namespace SwiftPay.Onboarding.Infra;

public sealed record ResultadoApi(bool Exito, int? CodigoHttp, string Detalle)
{
    public string Texto => CodigoHttp is { } c ? $"HTTP {c}" : $"error: {Detalle}";
}

/// <summary>Activación del comercio en SwiftPortals. Una llamada por terminal; sin reintentos en la misma ejecución.</summary>
public sealed class SwiftPortalsClient
{
    private readonly SwiftPortalsSettings _cfg;
    private readonly HttpClient _http;

    public SwiftPortalsClient(SwiftPortalsSettings cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            throw new InvalidOperationException("Falta la llave del API de SwiftPortals (variable de entorno SWIFTPAY_SwiftPortals__ApiKey).");
        _cfg = cfg;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(cfg.TimeoutSegundos) };
    }

    public async Task<ResultadoApi> ActivarAsync(string identificacion, string terminal)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, _cfg.ActivationUrl)
        {
            Content = JsonContent.Create(new { identification = identificacion, terminal }),
        };
        req.Headers.Add("X-Api-Key", _cfg.ApiKey);

        try
        {
            using var resp = await _http.SendAsync(req);
            var cuerpo = await resp.Content.ReadAsStringAsync();
            var codigo = (int)resp.StatusCode;
            return new ResultadoApi(codigo is >= 200 and < 300, codigo, Recortar(cuerpo));
        }
        catch (TaskCanceledException)
        {
            return new ResultadoApi(false, null, $"timeout de {_cfg.TimeoutSegundos} s");
        }
        catch (HttpRequestException ex)
        {
            return new ResultadoApi(false, null, Recortar(ex.Message));
        }
    }

    private static string Recortar(string s) => s.Length <= 300 ? s : s[..300];
}
