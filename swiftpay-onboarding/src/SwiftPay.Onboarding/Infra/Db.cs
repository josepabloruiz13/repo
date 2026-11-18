using Dapper;
using Microsoft.Data.SqlClient;

namespace SwiftPay.Onboarding.Infra;

/// <summary>Estado y bitácora en SQL Server (esquema onb, ver sql/01_esquema.sql).</summary>
public sealed class Db
{
    private readonly string _cs;

    public Db(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Falta Sql:ConnectionString.");
        _cs = connectionString;
    }

    private SqlConnection Abrir() => new(_cs);

    public async Task<List<DateOnly>> FeriadosAsync()
    {
        await using var c = Abrir();
        var fechas = await c.QueryAsync<DateTime>("SELECT Fecha FROM onb.Feriado");
        return fechas.Select(DateOnly.FromDateTime).ToList();
    }

    public async Task<int> FeriadosVerificadosDelAnioAsync(int anio)
    {
        await using var c = Abrir();
        return await c.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM onb.Feriado WHERE YEAR(Fecha) = @anio AND Verificado = 1", new { anio });
    }

    public async Task<bool> EnvioRegistradoAsync(string threadId, string tipo, string clave)
    {
        await using var c = Abrir();
        return await c.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM onb.Envio WHERE ThreadId = @threadId AND Tipo = @tipo AND Clave = @clave",
            new { threadId, tipo, clave }) > 0;
    }

    public async Task RegistrarEnvioAsync(string threadId, string comercio, string tipo, string clave,
        string destinatarios, string asunto, string? gmailId, bool esBorrador)
    {
        await using var c = Abrir();
        await c.ExecuteAsync("""
            IF NOT EXISTS (SELECT 1 FROM onb.Envio WHERE ThreadId = @threadId AND Tipo = @tipo AND Clave = @clave)
            INSERT INTO onb.Envio (ThreadId, Comercio, Tipo, Clave, Destinatarios, Asunto, GmailId, EsBorrador)
            VALUES (@threadId, @comercio, @tipo, @clave, @destinatarios, @asunto, @gmailId, @esBorrador)
            """, new { threadId, comercio, tipo, clave, destinatarios, asunto, gmailId, esBorrador });
    }

    public async Task<bool> ActivacionExitosaRegistradaAsync(string threadId, string terminal)
    {
        await using var c = Abrir();
        return await c.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM onb.LlamadaApi WHERE ThreadId = @threadId AND Terminal = @terminal AND Exito = 1",
            new { threadId, terminal }) > 0;
    }

    public async Task RegistrarLlamadaAsync(string threadId, string comercio, string cedula, string terminal,
        string tipoTerminal, int? codigoHttp, bool exito, string respuesta)
    {
        await using var c = Abrir();
        await c.ExecuteAsync("""
            INSERT INTO onb.LlamadaApi (ThreadId, Comercio, Cedula, Terminal, TipoTerminal, CodigoHttp, Exito, Respuesta)
            VALUES (@threadId, @comercio, @cedula, @terminal, @tipoTerminal, @codigoHttp, @exito, @respuesta)
            """, new { threadId, comercio, cedula, terminal, tipoTerminal, codigoHttp, exito, respuesta });
    }

    public async Task GuardarOnboardingAsync(string threadId, string comercio, DateOnly dia0, int n, string etapa,
        string? contactoNombre, string? contactoEmail, string? merchant, string? cedula, bool completado)
    {
        await using var c = Abrir();
        await c.ExecuteAsync("""
            MERGE onb.Onboarding AS t
            USING (SELECT @threadId AS ThreadId) AS s ON t.ThreadId = s.ThreadId
            WHEN MATCHED THEN UPDATE SET
                Comercio = @comercio, Dia0 = @dia0, N = @n, Etapa = @etapa,
                ContactoNombre = COALESCE(@contactoNombre, t.ContactoNombre),
                ContactoEmail = COALESCE(@contactoEmail, t.ContactoEmail),
                Merchant = COALESCE(@merchant, t.Merchant),
                Cedula = COALESCE(@cedula, t.Cedula),
                Completado = @completado, FechaActualizado = SYSDATETIME()
            WHEN NOT MATCHED THEN INSERT
                (ThreadId, Comercio, Dia0, N, Etapa, ContactoNombre, ContactoEmail, Merchant, Cedula, Completado)
                VALUES (@threadId, @comercio, @dia0, @n, @etapa, @contactoNombre, @contactoEmail, @merchant, @cedula, @completado);
            """, new { threadId, comercio, dia0 = dia0.ToDateTime(TimeOnly.MinValue), n, etapa, contactoNombre, contactoEmail, merchant, cedula, completado });
    }

    public async Task<long> IniciarEjecucionAsync(bool dryRun)
    {
        await using var c = Abrir();
        return await c.ExecuteScalarAsync<long>(
            "INSERT INTO onb.Ejecucion (DryRun) OUTPUT INSERTED.Id VALUES (@dryRun)", new { dryRun });
    }

    public async Task TerminarEjecucionAsync(long id, string resumen, string? error)
    {
        await using var c = Abrir();
        await c.ExecuteAsync(
            "UPDATE onb.Ejecucion SET Fin = SYSDATETIME(), Resumen = @resumen, Error = @error WHERE Id = @id",
            new { id, resumen, error });
    }
}
