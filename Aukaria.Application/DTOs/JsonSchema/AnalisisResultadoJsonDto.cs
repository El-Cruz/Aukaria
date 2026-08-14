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
}