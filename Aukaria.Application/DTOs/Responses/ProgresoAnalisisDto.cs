namespace Aukaria.Application.DTOs.Responses;

public class ProgresoAnalisisDto
{
    public string Etapa { get; set; } = string.Empty;

    public int Porcentaje { get; set; }

    public string Mensaje { get; set; } = string.Empty;
}
