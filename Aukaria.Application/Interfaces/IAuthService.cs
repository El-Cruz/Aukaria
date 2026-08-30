using Aukaria.Application.DTOs.Requests;
using Aukaria.Application.DTOs.Responses;
using Aukaria.Domain.Entities;

namespace Aukaria.Application.Interfaces;

public interface IAuthService
{
    Task SolicitarOtpAsync(SolicitarOtpRequestDto request, CancellationToken cancellationToken = default);
    Task<UsuarioSesionDto> VerificarOtpAsync(VerificarOtpRequestDto request, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerUsuarioSesionAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerOCrearPorProviderAsync(string provider, string email, string nombre, string subjectId, Guid empresaId, CancellationToken cancellationToken = default);
}
