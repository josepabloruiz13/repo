using ClosedXML.Excel;
using SwiftPay.Onboarding.Domain;

namespace SwiftPay.Onboarding.Tests;

public class BusinessCalendarTests
{
    private static readonly BusinessCalendar Cal = new([new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 25)]);

    [Fact]
    public void Dia0Domingo_MiercolesEsN3()
    {
        // SÉLA WEAR: día 0 domingo 15 nov 2026; el miércoles 18 nov es N = 3 (igual que la ejecución real).
        Assert.Equal(3, Cal.DiasHabilesDesde(new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 18)));
    }

    [Fact]
    public void Dia0NoCuenta_YFinesDeSemanaNoCuentan()
    {
        Assert.Equal(0, Cal.DiasHabilesDesde(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 20)));
        Assert.Equal(0, Cal.DiasHabilesDesde(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 22)));
        Assert.Equal(1, Cal.DiasHabilesDesde(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 23)));
    }

    [Fact]
    public void FeriadoNoCuenta()
    {
        // Lunes 30 nov -> martes 1 dic (feriado) -> miércoles 2 dic.
        Assert.Equal(1, Cal.DiasHabilesDesde(new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 2)));
        Assert.True(Cal.EsFeriado(new DateOnly(2026, 12, 25)));
        Assert.False(Cal.EsHabil(new DateOnly(2026, 12, 1)));
    }
}

public class CreationDataParserTests
{
    [Fact]
    public void LeeMerchantYTerminal()
    {
        var d = CreationDataParser.Parse("""
            Buenas tardes,

            Les compartimos los datos de creación:
            SP*SELA WEAR
            Merchant: 51717780889
            Terminal (WEB): 51698661

            Saludos
            """);
        Assert.NotNull(d);
        Assert.Equal("51717780889", d!.Merchant);
        Assert.Equal(new Terminal("WEB", "51698661"), Assert.Single(d.Terminales));
        Assert.Equal("SELA WEAR", d.NombreSp);
    }

    [Fact]
    public void VariosTerminales_YBloqueMasRecienteDelMensaje()
    {
        var d = CreationDataParser.Parse("""
            SP*COMERCIO
            Merchant: 111
            Terminal (WEB): 1

            Corrección:
            SP*COMERCIO
            Merchant: 222
            Terminal (WEB): 20
            Terminal (POS): 21
            """);
        Assert.Equal("222", d!.Merchant);
        Assert.Equal(["20", "21"], d.Terminales.Select(t => t.Numero));
        Assert.Equal("POS", d.Terminales[1].Tipo);
    }

    [Fact]
    public void IgnoraTextoCitado()
    {
        var d = CreationDataParser.Parse("""
            Gracias, lo revisamos.

            El lun, 16 nov 2026 a las 10:00, Banco <x@lafise.com> escribió:
            > Merchant: 999
            > Terminal (WEB): 888
            """);
        Assert.Null(d);
    }

    [Fact]
    public void SinTerminalNoHayDatos() => Assert.Null(CreationDataParser.Parse("Merchant: 123"));
}

public class CedulaReaderTests
{
    [Fact]
    public void LeeCeldaDebajo_TalCual()
    {
        using var ms = CrearExcel("Costa Rica ", "Cédula Física/Jurídica", " 1-1101-0816 ");
        var r = CedulaReader.Leer(ms);
        Assert.Equal("1-1101-0816", r.Cedula);
    }

    [Fact]
    public void NombreDeHojaConEspacioFinalSeReconoce() =>
        Assert.Equal("costa rica", TextUtil.Normalizar("Costa Rica "));

    [Fact]
    public void SinHoja_Motivo()
    {
        using var ms = CrearExcel("Panamá", "Cédula Física/Jurídica", "1");
        var r = CedulaReader.Leer(ms);
        Assert.Null(r.Cedula);
        Assert.Contains("Costa Rica", r.Motivo);
    }

    [Fact]
    public void CeldaVacia_Motivo()
    {
        using var ms = CrearExcel("Costa Rica", "Cedula fisica/juridica", "");
        Assert.Null(CedulaReader.Leer(ms).Cedula);
    }

    private static MemoryStream CrearExcel(string hoja, string titulo, string valor)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(hoja.Trim());
        ws.Cell("B2").Value = titulo;
        ws.Cell("B3").Value = valor;
        ws.Cell("B10").Value = "Cédula";
        ws.Cell("B11").Value = "9-9999-9999";
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}

public class AsuntosYFormularioTests
{
    [Theory]
    [InlineData("Solicitud de onboarding SÉLA WEAR.LIVE.EXPERIENCE", "SÉLA WEAR.LIVE.EXPERIENCE")]
    [InlineData("RE: Solicitud de onboarding Comercio XYZ S.A.", "Comercio XYZ S.A.")]
    [InlineData("Otra cosa", null)]
    public void ComercioDesdeAsunto(string asunto, string? esperado) =>
        Assert.Equal(esperado, OnboardingProcessor.ComercioDesdeAsunto(asunto));

    [Fact]
    public void AsuntoFormulario_AmbasOrtografias()
    {
        Assert.True(OnboardingProcessor.EsAsuntoFormulario("Affiliación a SwiftPay - Comercio XYZ", out var a));
        Assert.Equal("Comercio XYZ", a);
        Assert.True(OnboardingProcessor.EsAsuntoFormulario("Afiliación a SwiftPay - Otro", out _));
    }

    [Fact]
    public void MismoComercio_SinSAYSinTildes()
    {
        Assert.True(TextUtil.MismoComercio("Comercio XYZ, Sociedad Anónima", "COMERCIO XYZ S.A."));
        Assert.True(TextUtil.MismoComercio("SÉLA WEAR.LIVE.EXPERIENCE", "Sela Wear Live Experience"));
        Assert.False(TextUtil.MismoComercio("Panadería Luna", "Ferretería Sol"));
    }

    [Fact]
    public void ParseaFormulario()
    {
        var (nombre, email) = OnboardingProcessor.ParsearFormulario("""
            Nombre del comercio: SÉLA WEAR
            Nombre: Warren Solano Mora
            Email: warren@example.com
            """);
        Assert.Equal("Warren Solano Mora", nombre);
        Assert.Equal("warren@example.com", email);
        Assert.Equal("Warren", OnboardingProcessor.NombrePila(nombre!));
    }
}

public class PlantillasTests
{
    [Fact]
    public void CorreosAlComercioNoMencionanAlBanco()
    {
        foreach (var c in new[] { Templates.Inicio("Ana", "X"), Templates.Seguimiento("Ana", "X"),
                                  Templates.SeguimientoDisculpa("Ana", "X"), Templates.Bienvenida("Ana", "X") })
        {
            var t = TextUtil.Normalizar(c.Asunto + " " + c.Cuerpo);
            Assert.DoesNotContain("lafise", t);
            Assert.DoesNotMatch("\\bbanco\\b", t);
            Assert.Contains("afiliacion@swiftpay.one", t);
        }
    }

    [Fact]
    public void RecordatorioBanco_Variantes()
    {
        var d0 = new DateOnly(2026, 11, 15);
        Assert.Contains("solicitud de onboarding de X, que enviamos el 15 de noviembre", Templates.RecordatorioVencimientoBanco("X", d0, false, "123"));
        Assert.Contains("solicitud de creación #4567", Templates.RecordatorioVencimientoBanco("X", d0, true, "4567"));
        Assert.Contains("cómo va la creación del comercio", Templates.RecordatorioVencimientoBanco("X", d0, true, null));
        Assert.Contains("han transcurrido 6 días hábiles", Templates.SeguimientoBanco("X", d0, 6));
    }
}
