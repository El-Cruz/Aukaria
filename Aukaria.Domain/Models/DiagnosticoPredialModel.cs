using Aukaria.Domain.Enums;

namespace Aukaria.Domain.Models;

public class DiagnosticoPredialModel
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
    public EstadoViabilidad Viabilidad { get; set; }
    public string ResumenEjecutivo { get; set; } = string.Empty;
    public List<AnotacionModel> Anotaciones { get; set; } = new();
    public List<AlertaJuridicaModel> AlertasJuridicas { get; set; } = new();
    public List<string> Observaciones { get; set; } = new();
    public string ObservacionAmbiental { get; set; } = string.Empty;
    public string ExclusionResponsabilidad { get; set; } = string.Empty;
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
    public List<TitularModel> Titulares { get; set; } = new();
    public string RegimenPropiedadAnalisis { get; set; } = string.Empty;
    public List<TradicionActoModel> TradicionActos { get; set; } = new();
    public string CertificacionTracto { get; set; } = string.Empty;
    public string DiagnosticoEjecutivo { get; set; } = string.Empty;
    public List<string> DocumentosAnalizados { get; set; } = new();
}

public class AnotacionModel
{
    public string Numero { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class AlertaJuridicaModel
{
    public string NivelRiesgo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Recomendacion { get; set; } = string.Empty;
}

public class TitularModel
{
    public string Nombre { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Porcentaje { get; set; } = string.Empty;
}

public class TradicionActoModel
{
    public string NumeroEscritura { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Notaria { get; set; } = string.Empty;
    public string Otorgante { get; set; } = string.Empty;
    public string Adquirente { get; set; } = string.Empty;
    public string Cuantia { get; set; } = string.Empty;
}
