namespace Aukaria.Domain.Enums;

public enum TipoDocumentoJuridico
{
    CTL = 1,
    VUR = 2,
    EscrituraPublica = 3,
    DocumentoInmobiliarioGeneral = 4
}

public static class TipoDocumentoJuridicoExtensions
{
    public static string ObtenerNombre(this TipoDocumentoJuridico tipo) => tipo switch
    {
        TipoDocumentoJuridico.CTL => "Certificado de Tradición y Libertad",
        TipoDocumentoJuridico.VUR => "Informe VUR (Ventanilla Única Registral)",
        TipoDocumentoJuridico.EscrituraPublica => "Escritura Pública Notarial",
        _ => "Documento Inmobiliario General"
    };
}