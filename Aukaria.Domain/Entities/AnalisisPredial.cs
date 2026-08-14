using Aukaria.Domain.Enums;

namespace Aukaria.Domain.Entities;

public class AnalisisPredial
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid UsuarioId { get; set; }
    public string MatriculaFMI { get; set; } = string.Empty;
    public string ORIP { get; set; } = string.Empty;
    public string NombrePredio { get; set; } = string.Empty;
    public PropositoAnalisis Proposito { get; set; }
    public TipoDocumentoJuridico TipoDocumento { get; set; } = TipoDocumentoJuridico.CTL;
    public EstadoViabilidad Viabilidad { get; set; }
    public string ResumenEjecutivo { get; set; } = string.Empty;
    public string ResultadoJson { get; set; } = string.Empty;
    public DateTime FechaAnalisis { get; set; }
    public int ConsumoTokens { get; set; }
}