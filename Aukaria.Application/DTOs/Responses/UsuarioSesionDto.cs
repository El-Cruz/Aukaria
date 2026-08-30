using Aukaria.Domain.Enums;

namespace Aukaria.Application.DTOs.Responses;

public class UsuarioSesionDto
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public bool EmailConfirmado { get; set; }
    public string? EmpresaNombre { get; set; }
}
