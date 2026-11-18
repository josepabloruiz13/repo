using System.Text;

namespace SwiftPay.Onboarding.Domain;

/// <summary>Resumen de cada ejecución para José Pablo. Nunca incluye la llave del API.</summary>
public sealed class RunSummary
{
    public bool DryRun { get; init; }
    public List<string> Enviados { get; } = [];
    public List<string> Borradores { get; } = [];
    public List<string> Activos { get; } = [];
    public List<string> SlaHoy { get; } = [];
    public List<string> SlaVencidos { get; } = [];
    public List<string> Terminales { get; } = [];
    public List<string> PeticionesBanco { get; } = [];
    public List<string> Dudas { get; } = [];
    public List<string> Avisos { get; } = [];
    public string? LineaUnica { get; set; }

    public bool HuboCambios =>
        Enviados.Count + Borradores.Count + Terminales.Count + PeticionesBanco.Count + Dudas.Count + Avisos.Count > 0;

    public bool SinActividad => LineaUnica == null && Activos.Count == 0 && !HuboCambios;

    public string Texto()
    {
        var prefijo = DryRun ? "[SIMULACIÓN – no se envió ni cambió nada]\n\n" : "";
        if (LineaUnica != null) return prefijo + LineaUnica;
        if (SinActividad) return prefijo + "Sin onboardings activos ni cambios hoy.";

        var sb = new StringBuilder(prefijo);
        Seccion(sb, "Correos enviados hoy", Enviados);
        Seccion(sb, "Borradores sin destinatario", Borradores);
        Seccion(sb, "Onboardings activos", Activos);
        Seccion(sb, "SLA que vence hoy (N = 3)", SlaHoy);
        Seccion(sb, "SLA vencidos con el banco", SlaVencidos);
        Seccion(sb, "Terminales recibidos y activación en SwiftPortals", Terminales);
        Seccion(sb, "Peticiones de información del banco pendientes (acción para José Pablo)", PeticionesBanco);
        Seccion(sb, "Dudas, casos sin clasificar y errores", Dudas);
        Seccion(sb, "Avisos", Avisos);
        return sb.ToString().TrimEnd();
    }

    private static void Seccion(StringBuilder sb, string titulo, List<string> items)
    {
        if (items.Count == 0) return;
        sb.AppendLine(titulo + ":");
        foreach (var i in items) sb.AppendLine("- " + i);
        sb.AppendLine();
    }
}
