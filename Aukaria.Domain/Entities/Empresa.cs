namespace Aukaria.Domain.Entities;

public class Empresa
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public int BolsaCreditos { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; }
}