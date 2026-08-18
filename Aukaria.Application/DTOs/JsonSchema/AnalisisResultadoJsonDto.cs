namespace Aukaria.Application.DTOs.JsonSchema;

public class AnalisisResultadoJsonDto
{
    public string MatriculaFMI { get; set; } = string.Empty;
    public string ORIP { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Vereda { get; set; } = string.Empty;
    public string NombrePredio { get; set; } = string.Empty;
    public string AreaRegistrada { get; set; } = string.Empty;
    public string PropietarioActual { get; set; } = string.Empty;
    public string EstadoFolio { get; set; } = string.Empty;
    public string Viabilidad { get; set; } = string.Empty;
    public string ResumenEjecutivo { get; set; } = string.Empty;
    public List<AnotacionDto> Anotaciones { get; set; } = new();
    public List<AlertaJuridicaDto> AlertasJuridicas { get; set; } = new();
    public List<string> Observaciones { get; set; } = new();
    public string NumeroEscritura { get; set; } = string.Empty;
    public string Notaria { get; set; } = string.Empty;
    public string CiudadNotaria { get; set; } = string.Empty;
    public string FechaEscritura { get; set; } = string.Empty;
    public List<string> Otorgantes { get; set; } = new();
    public string Cuantia { get; set; } = string.Empty;
    public string Linderos { get; set; } = string.Empty;

    public string CedulaCatastral { get; set; } = string.Empty;
    public string Nupre { get; set; } = string.Empty;
    public string FolioMatriz { get; set; } = string.Empty;
    public string FoliosDerivados { get; set; } = string.Empty;
    public string FechaExpedicionCertificado { get; set; } = string.Empty;

    public string OrigenCabidaActual { get; set; } = string.Empty;
    public string ConclusionPredial { get; set; } = string.Empty;
    public string AreaSegunFmi { get; set; } = string.Empty;
    public string AreaAdjudicacionInicial { get; set; } = string.Empty;
    public string DesenglobesVentasParciales { get; set; } = string.Empty;
    public string AreaRemanenteReal { get; set; } = string.Empty;

    public string LinderosDescripcion { get; set; } = string.Empty;
    public string SoporteDocumentalLinderos { get; set; } = string.Empty;

    public List<TitularDto> Titulares { get; set; } = new();
    public string RegimenPropiedadAnalisis { get; set; } = string.Empty;

    public List<TradicionActoDto> TradicionActos { get; set; } = new();
    public string CertificacionTracto { get; set; } = string.Empty;

    public string DiagnosticoEjecutivo { get; set; } = string.Empty;

    public List<string> DocumentosAnalizados { get; set; } = new();
}