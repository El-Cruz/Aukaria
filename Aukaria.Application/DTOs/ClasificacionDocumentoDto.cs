using Aukaria.Domain.Enums;

namespace Aukaria.Application.DTOs;

public class ClasificacionDocumentoDto
{
    public TipoDocumentoJuridico TipoDocumento { get; set; }
    public string NombreTipoDocumento { get; set; } = string.Empty;
    public string IdentificadorExtraido { get; set; } = string.Empty;
    public string ResumenDeteccion { get; set; } = string.Empty;
}