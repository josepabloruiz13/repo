using System.Text.RegularExpressions;

namespace SwiftPay.Onboarding.Domain;

public sealed record Terminal(string Tipo, string Numero);

public sealed record DatosCreacion(string? NombreSp, string Merchant, IReadOnlyList<Terminal> Terminales);

/// <summary>
/// Lee el bloque de datos de creación que manda el banco:
///   SP*NOMBRE
///   Merchant: 51717780889
///   Terminal (WEB): 51698661
/// </summary>
public static partial class CreationDataParser
{
    public static DatosCreacion? Parse(string cuerpo)
    {
        // Solo el texto nuevo del mensaje; lo citado de correos anteriores no cuenta.
        var texto = TextUtil.QuitarCitado(cuerpo);

        var merchants = MerchantRx().Matches(texto);
        if (merchants.Count == 0) return null;

        // Si en el mismo mensaje hay varios bloques, el válido es el último.
        var ultimoMerchant = merchants[^1];
        var desde = texto.LastIndexOf("SP*", ultimoMerchant.Index, StringComparison.OrdinalIgnoreCase);
        var inicioBloque = desde >= 0 ? desde : ultimoMerchant.Index;
        var bloque = texto[inicioBloque..];

        var terminales = TerminalRx().Matches(bloque)
            .Select(m => new Terminal(m.Groups["tipo"].Value.Trim().ToUpperInvariant(), m.Groups["num"].Value))
            .DistinctBy(t => t.Numero)
            .ToList();
        if (terminales.Count == 0) return null;

        string? nombre = null;
        var sp = SpRx().Match(bloque);
        if (sp.Success) nombre = sp.Groups["nombre"].Value.Trim();

        return new DatosCreacion(nombre, ultimoMerchant.Groups["num"].Value, terminales);
    }

    [GeneratedRegex(@"Merchant\s*:\s*(?<num>\d+)", RegexOptions.IgnoreCase)] private static partial Regex MerchantRx();
    [GeneratedRegex(@"Terminal\s*\(\s*(?<tipo>[A-Za-z]+)\s*\)\s*:\s*(?<num>\d+)", RegexOptions.IgnoreCase)] private static partial Regex TerminalRx();
    [GeneratedRegex(@"SP\*\s*(?<nombre>[^\r\n]+)", RegexOptions.IgnoreCase)] private static partial Regex SpRx();
}
