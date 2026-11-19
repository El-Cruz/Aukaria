namespace Aukaria.Application.DTOs.Responses;

public class PreAnalisisFmiResponseDto
{
    public string MatriculaFMI { get; set; } = string.Empty;
    public bool ExistePrevio { get; set; }
    public DateTime? FechaUltimoAnalisis { get; set; }
    public Guid? AnalisisIdPrevio { get; set; }
    public ClasificacionDocumentoDto ClasificacionDocumento { get; set; } = new();
    public string CedulaCatastral { get; set; } = string.Empty;
    public string Nupre { get; set; } = string.Empty;
    public string Orip { get; set; } = string.Empty;
    public string NombrePredio { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public DateOnly? FechaExpedicionCertificado { get; set; }
    public DateOnly? FechaExpedicionPrevio { get; set; }
    public string AccionSugerida { get; set; } = "Nuevo";
    public int CantidadVigentes { get; set; }
}