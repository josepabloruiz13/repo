using ClosedXML.Excel;

namespace SwiftPay.Onboarding.Domain;

public sealed record ResultadoCedula(string? Cedula, string? Motivo);

/// <summary>Lee la cédula física/jurídica del Excel "Comercios &lt;COMERCIO&gt;.xlsx" enviado al banco.</summary>
public static class CedulaReader
{
    public static ResultadoCedula Leer(Stream xlsx)
    {
        XLWorkbook libro;
        try { libro = new XLWorkbook(xlsx); }
        catch (Exception ex) { return new(null, $"No se pudo abrir el Excel: {ex.Message}"); }

        using (libro)
        {
            // La hoja puede venir como "Costa Rica " (con espacio al final).
            var hoja = libro.Worksheets.FirstOrDefault(h => TextUtil.Normalizar(h.Name) == "costa rica");
            if (hoja is null) return new(null, "El Excel no tiene la hoja \"Costa Rica\".");

            foreach (var celda in hoja.CellsUsed())
            {
                if (TextUtil.Normalizar(celda.GetString()) != "cedula fisica/juridica") continue;

                // El valor está en la celda inmediatamente debajo. Se usa como texto, tal cual (con guiones).
                var valor = celda.CellBelow().GetFormattedString().Trim();
                return valor.Length == 0
                    ? new(null, $"La celda debajo de \"Cédula física/jurídica\" ({celda.CellBelow().Address}) está vacía.")
                    : new(valor, null);
            }
            return new(null, "No se encontró la celda \"Cédula física/jurídica\" en la hoja \"Costa Rica\".");
        }
    }
}
