namespace Aukaria.Application.DTOs.JsonSchema;

public class AnotacionDto
{
    public string NumeroAnotacion { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Especificacion { get; set; } = string.Empty;
    public string NaturalezaJuridica { get; set; } = string.Empty;
    public bool EsGravamen { get; set; }
    public bool EsMedidaCautelar { get; set; }
}