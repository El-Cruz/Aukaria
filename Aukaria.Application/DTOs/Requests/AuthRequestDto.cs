namespace Aukaria.Application.DTOs.Requests;

public class SolicitarOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class VerificarOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string CodigoOtp { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public Guid EmpresaId { get; set; }
    public bool EsRegistro { get; set; }
}
