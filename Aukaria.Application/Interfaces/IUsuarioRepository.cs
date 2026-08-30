using Aukaria.Domain.Entities;

namespace Aukaria.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorProviderAsync(string provider, string subjectId, CancellationToken cancellationToken = default);
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Usuario> CrearAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<List<Empresa>> ObtenerEmpresasAsync(CancellationToken cancellationToken = default);
}
