namespace Aukaria.Application.DTOs.JsonSchema;

public class AlertaJuridicaDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string NivelRiesgo { get; set; } = string.Empty;
    public string Recomendacion { get; set; } = string.Empty;
}