using Aukaria.Domain.Enums;

namespace Aukaria.Application.DTOs.Requests;

public class SolicitudAnalisisRequestDto
{
    public string MatriculaFMI { get; set; } = string.Empty;
    public PropositoAnalisis Proposito { get; set; }
    public TipoDocumentoJuridico TipoDocumento { get; set; } = TipoDocumentoJuridico.CTL;
    public Guid EmpresaId { get; set; }
    public Guid UsuarioId { get; set; }
    public string ContenidoTextoCtl { get; set; } = string.Empty;
}