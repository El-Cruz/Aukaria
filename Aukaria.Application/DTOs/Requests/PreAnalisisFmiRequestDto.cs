namespace Aukaria.Application.DTOs.Requests;

public class PreAnalisisFmiRequestDto
{
    public string MatriculaFMI { get; set; } = string.Empty;
    public Guid EmpresaId { get; set; }
}