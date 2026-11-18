using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SwiftPay.Onboarding.Domain;

public static partial class TextUtil
{
    /// <summary>Minúsculas, sin tildes, espacios colapsados.</summary>
    public static string Normalizar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return Espacios().Replace(sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(), " ").Trim();
    }

    /// <summary>Nombre de comercio para comparar: sin tildes, sin "S.A." / "Sociedad Anónima", solo letras y números.</summary>
    public static string ClaveComercio(string? nombre)
    {
        var n = Normalizar(nombre);
        n = SociedadAnonima().Replace(n, " ");
        n = NoAlfanumerico().Replace(n, " ");
        return Espacios().Replace(n, " ").Trim();
    }

    /// <summary>True si los dos nombres de comercio se refieren al mismo (uno contiene al otro tras normalizar).</summary>
    public static bool MismoComercio(string a, string b)
    {
        var ka = ClaveComercio(a);
        var kb = ClaveComercio(b);
        if (ka.Length == 0 || kb.Length == 0) return false;
        return ka == kb || ka.Contains(kb) || kb.Contains(ka);
    }

    /// <summary>Quita el texto citado de respuestas anteriores (">", "El ... escribió:", "On ... wrote:", "De:/From:").</summary>
    public static string QuitarCitado(string cuerpo)
    {
        var lineas = cuerpo.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        foreach (var linea in lineas)
        {
            var t = linea.TrimStart();
            if (t.StartsWith('>')) break;
            if (InicioCita().IsMatch(t)) break;
            sb.AppendLine(linea);
        }
        return sb.ToString().Trim();
    }

    public static string HtmlATexto(string html)
    {
        var s = Regex.Replace(html, "(?is)<(script|style)[^>]*>.*?</\\1>", "");
        s = Regex.Replace(s, "(?i)<br\\s*/?>|</p>|</div>|</tr>|</li>", "\n");
        s = Regex.Replace(s, "<[^>]+>", "");
        return WebUtility.HtmlDecode(s);
    }

    public static string Recortar(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max]);

    [GeneratedRegex("\\s+")] private static partial Regex Espacios();
    [GeneratedRegex("\\bsociedad anonima\\b|\\bs\\s*\\.?\\s*a\\s*\\.?(?=\\s|$)")] private static partial Regex SociedadAnonima();
    [GeneratedRegex("[^a-z0-9]+")] private static partial Regex NoAlfanumerico();
    [GeneratedRegex("^(El .+ escribi[oó]:|On .+ wrote:|-{2,} ?Original Message|-{2,} ?Mensaje original|De: .+|From: .+)", RegexOptions.IgnoreCase)]
    private static partial Regex InicioCita();
}
