namespace Aukaria.Application.DTOs.Responses;

public class PreAnalisisFmiResponseDto
{
    public string MatriculaFMI { get; set; } = string.Empty;
    public bool ExistePrevio { get; set; }
    public DateTime? FechaUltimoAnalisis { get; set; }
    public Guid? AnalisisIdPrevio { get; set; }
    public ClasificacionDocumentoDto ClasificacionDocumento { get; set; } = new();
}