using Aukaria.Domain.Enums;
using Aukaria.Application.DTOs.JsonSchema;

namespace Aukaria.Application.DTOs.Responses;

public class AnalisisPredialResponseDto
{
    public Guid Id { get; set; }
    public string MatriculaFMI { get; set; } = string.Empty;
    public string NombrePredio { get; set; } = string.Empty;
    public string ORIP { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string CedulaCatastral { get; set; } = string.Empty;
    public PropositoAnalisis Proposito { get; set; }
    public TipoDocumentoJuridico TipoDocumento { get; set; }
    public string NombreTipoDocumento { get; set; } = string.Empty;
    public EstadoViabilidad Viabilidad { get; set; }
    public string ResumenEjecutivo { get; set; } = string.Empty;
    public AnalisisResultadoJsonDto Resultado { get; set; } = new();
    public DateTime FechaAnalisis { get; set; }
}